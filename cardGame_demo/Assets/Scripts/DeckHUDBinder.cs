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

    // GameDirector'a erişim her zaman singleton üzerinden
    GameDirector Director => GameDirector.Instance;

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
    }

    void OnDisable()
    {
        UnsubscribeDirectorEvents();
        if (_pollCo != null) { StopCoroutine(_pollCo); _pollCo = null; }
    }

    IEnumerator BindWhenDirectorReady()
    {
        // Director oluşana kadar bekle
        while (Director == null) yield return null;

        // Deck’i bağla
        TryBindDeck();

        // Event’lere abone ol (bir kez)
        SubscribeDirectorEvents();

        // Periyodik fallback (spawn/reshuffle vb. için güvence)
        if (_pollCo == null) _pollCo = StartCoroutine(PollRoutine());

        UpdateLabel(); // ilk çizim
    }

    void SubscribeDirectorEvents()
    {
        if (_subscribed || Director == null) return;
        Director.onCardDrawn.AddListener(OnAnyCardDrawn);
        _subscribed = true;
    }

    void UnsubscribeDirectorEvents()
    {
        if (!_subscribed || Director == null) { _subscribed = false; return; }
        Director.onCardDrawn.RemoveListener(OnAnyCardDrawn);
        _subscribed = false;
    }

    void TryBindDeck()
    {
        _deck = null;
        _baseSize = 0;
        _lastCount = 0;

        if (Director == null || Director.Ctx == null || owner == null) return;

        if (Director.Ctx.DecksByUnit.TryGetValue(owner, out var deck) && deck != null)
        {
            _deck = deck;
            _baseSize  = deck.Count;
            _lastCount = _baseSize;
        }
    }

    void OnAnyCardDrawn(Actor actor, PhaseKind phase, Card _)
    {
        if (Director == null || Director.Ctx == null || owner == null) return;

        // Bu binder’ın sahibine ait çekim mi?
        if ((actor == Actor.Player && Director.Ctx.Player == owner) ||
            (actor == Actor.Enemy  && Director.Ctx.Enemy  == owner))
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
