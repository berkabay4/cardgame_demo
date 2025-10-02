using UnityEngine;
using TMPro;
using System.Collections;

[DisallowMultipleComponent]
public class DeckHUDBinder : MonoBehaviour
{
    [Header("Refs (auto)")]
    [SerializeField] private SimpleCombatant owner;
    [SerializeField] private TextMeshProUGUI targetText; // Canvas(4th child)'daki TMP

    [Header("Options")]
    [Tooltip("Canvas altında 4. child (index=3) otomatik bulunsun mu?")]
    [SerializeField] private bool autoFindFourthChildTMP = true;

    // Runtime
    private IDeckService _deck;
    private int _baseSize;      // destenin “tam dolu” kabul edilen kart sayısı
    private int _lastCount;     // son kalan
    private Coroutine _pollCo;
    private bool _subscribed;
    CombatDirector combatDirector => CombatDirector.Instance;

    void Reset()
    {
        owner = GetComponent<SimpleCombatant>();
    }

    void Awake()
    {
        if (!owner) owner = GetComponent<SimpleCombatant>();

        if (!targetText && autoFindFourthChildTMP)
        {
            Transform t = transform;
            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas) t = canvas.transform;

            if (t.childCount >= 4)
            {
                var fourth = t.GetChild(3);
                targetText = fourth.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }
    }

    void OnEnable()
    {
        StartCoroutine(BindWhenDirectorReady());
        EnemySpawner.EnemiesSpawned += OnEnemiesSpawned; // spawn/refresh sonrası rebind
    }

    void OnDisable()
    {
        EnemySpawner.EnemiesSpawned -= OnEnemiesSpawned;
        UnsubscribeDirectorEvents();
        if (_pollCo != null) { StopCoroutine(_pollCo); _pollCo = null; }
    }

    IEnumerator BindWhenDirectorReady()
    {
        // Director oluşana kadar bekle
        while (combatDirector == null) yield return null;
        // Context oluşana kadar bekle
        while (combatDirector.Ctx == null) yield return null;

        // Director event'ine (oyun start) abone ol – ek güvence
        SubscribeDirectorEvents();

        // Deck’i bağla
        TryBindDeck();

        // Periyodik fallback (spawn/reshuffle vb. için güvence)
        if (_pollCo == null) _pollCo = StartCoroutine(PollRoutine());

        UpdateLabel(); // ilk çizim
    }

    void SubscribeDirectorEvents()
    {
        if (_subscribed || combatDirector == null) return;
        combatDirector.onCardDrawn.AddListener(OnAnyCardDrawn);
        combatDirector.onGameStarted.AddListener(OnDirectorGameStarted);
        _subscribed = true;
    }

    void UnsubscribeDirectorEvents()
    {
        if (!_subscribed || combatDirector == null) { _subscribed = false; return; }
        combatDirector.onCardDrawn.RemoveListener(OnAnyCardDrawn);
        combatDirector.onGameStarted.RemoveListener(OnDirectorGameStarted);
        _subscribed = false;
    }

    void OnDirectorGameStarted()
    {
        // Oyun başlarken context kayıtları tamamlanmış olur – rebind et
        TryBindDeck();
        UpdateLabel();
    }

    void OnEnemiesSpawned()
    {
        // Yeni düşmanlar/refresh sonrası – rebind et
        TryBindDeck();
        UpdateLabel();
    }

    void TryBindDeck()
    {
        _deck = null;
        _baseSize = 0;
        _lastCount = 0;

        if (combatDirector?.Ctx == null || owner == null) return;

        // Context API
        var deck = combatDirector.Ctx.GetDeckFor(owner);
        if (deck != null)
        {
            _deck      = deck;
            _baseSize  = deck.Count;   // ilk “tam dolu” kabul
            _lastCount = _baseSize;
        }
    }

    void OnAnyCardDrawn(Actor actor, PhaseKind phase, Card _)
    {
        if (combatDirector == null || combatDirector.Ctx == null || owner == null) return;

        // Bu binder’ın sahibine ait çekim mi?
        if ((actor == Actor.Player && combatDirector.Ctx.Player == owner) ||
            (actor == Actor.Enemy  && combatDirector.Ctx.Enemy  == owner))
        {
            TouchAndUpdate();
        }
    }

    IEnumerator PollRoutine()
    {
        var wait = new WaitForSeconds(0.25f);
        while (true)
        {
            TouchAndUpdate();
            yield return wait;
        }
    }

    void TouchAndUpdate()
    {
        if (_deck == null)
        {
            TryBindDeck();
            if (_deck == null) { UpdateLabel(); return; }
        }

        int current = _deck.Count;

        // Rebuild sonrası count artarsa, yeni tam kapasite olarak kabul et
        if (current > _baseSize) _baseSize = current;

        _lastCount = current;
        UpdateLabel();
    }

    void UpdateLabel()
    {
        if (!targetText)
            return;

        if (_deck == null)
        {
            targetText.text = "--/--";
            return;
        }

        int remaining = _lastCount;
        int drawn     = Mathf.Max(0, _baseSize - remaining);
        targetText.text = $"{drawn}/{remaining}";
    }
}
