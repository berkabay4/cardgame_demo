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

    [Header("When")]
    [SerializeField] bool grantOnAwake = false;
    [SerializeField] bool grantOnStart = true;
    [SerializeField, Min(0)] int delayFrames = 0; // GameDirector/RelicManager hazır olsun diye

    [Header("What")]
    [Tooltip("Grant etmeden önce oyuncunun tüm relic'lerini temizle.")]
    [SerializeField] bool clearExistingBefore = false;

    [Tooltip("Bu liste sahne açılınca verilecektir.")]
    [SerializeField] List<Entry> toGrant = new()
    {
        new Entry{ relic = null, stacks = 1, enabled = true },
    };

    [Header("Safety")]
    [Tooltip("Verildikten sonra bu component'i devre dışı bırak.")]
    [SerializeField] bool disableAfterGrant = true;

    void Awake()
    {
        if (grantOnAwake) StartCoroutine(GrantRoutine());
    }

    void Start()
    {
        if (grantOnStart) StartCoroutine(GrantRoutine());
    }

    IEnumerator GrantRoutine()
    {
        // İstenirse birkaç frame bekle
        for (int i = 0; i < delayFrames; i++) yield return null;

        // RelicManager'ı bul
        var rm = RelicManager.Instance ?? FindFirstObjectByType<RelicManager>(FindObjectsInactive.Include);
        if (rm == null)
        {
            Debug.LogWarning("[RelicGrantOnStart] RelicManager bulunamadı. Grant işlemi atlandı.");
            yield break;
        }

        if (clearExistingBefore)
            rm.ClearAll(callLoseHooks: false);

        int granted = 0;
        foreach (var e in toGrant)
        {
            if (!e.enabled || e.relic == null) continue;
            rm.Acquire(e.relic, Mathf.Max(1, e.stacks));
            granted++;
        }

        Debug.Log($"[RelicGrantOnStart] Granted {granted} relic(s).");

        if (disableAfterGrant) enabled = false;
    }

#if UNITY_EDITOR
    // Inspector'da sağ tık menüsüyle anında denemek için
    [ContextMenu("Grant Now (Play Mode)")]
    void ContextGrantNow()
    {
        if (Application.isPlaying) StartCoroutine(GrantRoutine());
        else Debug.LogWarning("Play Mode dışında çalıştırılamaz.");
    }
#endif
}
