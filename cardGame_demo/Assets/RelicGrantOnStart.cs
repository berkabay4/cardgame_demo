using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class RelicGrantOnStart : MonoBehaviour
{
    [System.Serializable]
    public struct Entry
    {
        public RelicDefinition relic;
        [Min(1)] public int stacks;
        public bool enabled;
    }

    [Header("Trigger (Inspector Toggle)")]
    [Tooltip("PLAY MODE'da bunu TRUE yapınca listedeki relic'ler verilir.")]
    [SerializeField] bool grantOnToggle = false;
    [SerializeField] bool autoResetToggleAfterGrant = true;

    [Header("What")]
    [Tooltip("Grant etmeden önce oyuncunun tüm relic'lerini temizle.")]
    [SerializeField] bool clearExistingBefore = false;

    [Tooltip("Bu liste tetiklendiğinde verilecektir.")]
    [SerializeField] List<Entry> toGrant = new()
    {
        new Entry{ relic = null, stacks = 1, enabled = true },
    };

    [Header("Post Actions")]
    [SerializeField] bool raiseRelicsChangedEvent = true; // RelicManager.OnRelicsChanged/RaiseRelicsChanged tetikle
    [SerializeField] bool forceApplyRelicStats = true;    // RelicStatsSync.ApplyNow() çağır

    [Header("Safety")]
    [Tooltip("Verildikten sonra bu component'i devre dışı bırak.")]
    [SerializeField] bool disableAfterGrant = false;

    // --- internal ---
    bool _lastToggle; // edge-detect

    void OnValidate()
    {
        // Toggle edge detection: false -> true
        if (grantOnToggle && !_lastToggle)
        {
            if (Application.isPlaying)
            {
                StartCoroutine(GrantRoutine());
            }
            else
            {
                Debug.LogWarning("[RelicGrantOnStart] Play Mode dışında çalışmaz. Oyunu başlatıp toggle'ı tekrar açın.");
            }
        }
        _lastToggle = grantOnToggle;
    }
    bool ValidateGrantList(List<Entry> list)
    {
        var set = new HashSet<string>();
        foreach (var e in list)
        {
            if (!e.enabled || e.relic == null) continue;
            var id = e.relic.relicId;
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning($"[RelicGrantOnStart] '{e.relic.name}' relicId boş!");
                continue;
            }
            if (!set.Add(id))
                Debug.LogWarning($"[RelicGrantOnStart] Listede tekrarlı relicId: '{id}'. " +
                                $"Aynı id birden fazla entry’de kullanılıyor.");
        }
        return true;
    }
    IEnumerator GrantRoutine()
    {
       
        // RelicManager'ı bul
        var rm = RelicManager.Instance ?? FindFirstObjectByType<RelicManager>(FindObjectsInactive.Include);
        if (rm == null)
        {
            Debug.LogWarning("[RelicGrantOnStart] RelicManager bulunamadı. Grant işlemi atlandı.");
            yield break;
        }

        if (clearExistingBefore)
            rm.ClearAll(callLoseHooks: false);
        ValidateGrantList(toGrant);
        int granted = 0;
        foreach (var e in toGrant)
        {
            if (!e.enabled || e.relic == null) continue;
            rm.Acquire(e.relic, Mathf.Max(1, e.stacks));
            granted++;
            yield return null; // UI/Logs için bir kare esneklik (isteğe bağlı)
        }

        Debug.Log($"[RelicGrantOnStart] Granted {granted} relic(s).");

        // Değişimi bildir + statları uygula
        if (raiseRelicsChangedEvent)
        {
            // Tercihen public RaiseRelicsChanged kullan
            rm.RaiseRelicsChanged();
        }

        if (forceApplyRelicStats)
        {
            var sync = FindFirstObjectByType<RelicStatsSync>(FindObjectsInactive.Include);
            if (sync != null) sync.ApplyNow();
        }

        if (autoResetToggleAfterGrant)
        {
            grantOnToggle = false;
            _lastToggle = false;
        }

        if (disableAfterGrant) enabled = false;
    }
}
