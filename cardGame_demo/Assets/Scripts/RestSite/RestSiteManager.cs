using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[DisallowMultipleComponent]
public class RestSiteManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button healMissing50Btn;
    [SerializeField] private Button addJokerBtn;
    [SerializeField] private Button mapBtn;
    [SerializeField] private TMP_Text infoText;

    [Header("Config")]
    [SerializeField] private string mapSceneName = "MapScene";

    private Player _player;
    private HealthManager _health;
    private DeckOwner _deckOwner;

    private bool _choiceLocked;

    void Awake()
    {
        _player    = Player.Instance ?? FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (_player == null) { Debug.LogError("[RestScene] Player bulunamadı."); return; }

        _health    = _player.Health;
        _deckOwner = _player.DeckOwner;

        if (mapBtn) mapBtn.gameObject.SetActive(false);
    }

    void Start()
    {
        if (healMissing50Btn) healMissing50Btn.onClick.AddListener(OnHealMissing50);
        if (addJokerBtn)      addJokerBtn.onClick.AddListener(OnAddJoker);
        if (mapBtn)           mapBtn.onClick.AddListener(OnGoMap);

        SetInfo("Birini seç: Eksik canının %50’sini yenile veya destene 1 Joker ekle.");

        // Full can ise opsiyonel disable
        if (GetHP(out var cur, out var max) && cur >= max)
        {
            if (healMissing50Btn) healMissing50Btn.interactable = false;
            SetInfo("Canın zaten full. İstersen Joker ekle.");
        }
    }

    public void OnHealMissing50()
    {
        if (_choiceLocked || _health == null) return;

        if (!GetHP(out var cur, out var max))
        {
            SetInfo("Sağlık bilgisi okunamadı.");
            return;
        }

        var missing = Mathf.Max(0, max - cur);
        var healAmt = Mathf.FloorToInt(missing * 0.5f);
        if (healAmt > 0)
        {
            // Projendeki HealthManager API’sine göre uyarlayabilirsin.
            // Örn: _health.Heal(int)
            _health.Heal(healAmt);
            SetInfo($"Dinlendin. {healAmt} can yenilendi. ({cur}→{Mathf.Min(cur + healAmt, max)}/{max})");
        }
        else
        {
            SetInfo("Canın zaten full; iyileştirilecek bir şey yoktu.");
        }

        LockChoice();
    }

    public void OnAddJoker()
    {
        if (_choiceLocked || _deckOwner == null) return;

        bool added = _deckOwner.AddJoker(); // CardDatabase yok; direkt Joker üretip ekliyoruz.
        SetInfo(added ? "Destene 1 adet Joker eklendi." : "Joker eklenemedi (kapasite dolu olabilir).");

        LockChoice();
    }

    private void OnGoMap()
    {
        if (!string.IsNullOrEmpty(mapSceneName))
            SceneManager.LoadScene(mapSceneName, LoadSceneMode.Single);
    }

    private void LockChoice()
    {
        _choiceLocked = true;
        if (healMissing50Btn) healMissing50Btn.interactable = false;
        if (addJokerBtn)      addJokerBtn.interactable = false;
        if (mapBtn)           mapBtn.gameObject.SetActive(true);
    }

    private void SetInfo(string msg)
    {
        if (infoText) infoText.text = msg;
    }

    // HealthManager alan adların farklıysa burayı uyarlarsın.
    private bool GetHP(out int current, out int max)
    {
        current = 0; max = 0;
        if (_health != null)
        {
            // Örn: public int CurrentHP {get;}  public int MaxHP {get;}
            current = _health.CurrentHP;
            max     = _health.MaxHP;
            return true;
        }
        // Yedek: PlayerData’dan
        if (_player?.Data != null)
        {
            max = _player.Data.maxHealth;
            // current’ı PlayerData tutmuyorsa 0 kalabilir; burada true dönmeyelim.
        }
        return false;
    }
}
