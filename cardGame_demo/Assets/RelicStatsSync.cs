using UnityEngine;
using System.Collections;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class RelicStatsSync : MonoBehaviour
{
    [System.Serializable]
    public class Binding
    {
        public string name;
        public StatId stat;
        public Actor actor = Actor.Player;
        public PhaseKind phase = PhaseKind.Attack;

        [Tooltip("Baz değeri nereden alalım? (Threshold için CombatContext, MaxHealth için HealthManager vb.)")]
        public BaseSource source = BaseSource.Auto;
        public float baseOverride = 0f; // Manual ise kullan

        [Header("Apply Target")]
        public ApplyTarget applyTarget = ApplyTarget.Auto;
        public UnityEvent<float> onApplied; // fallback: inspector’dan bağlayabileceğin event
    }

    public enum BaseSource { Auto, Manual }
    public enum ApplyTarget { Auto, Manual }

    [Header("When")]
    [SerializeField] bool applyOnStart = true;
    [SerializeField, Min(0)] int delayFrames = 1;
    [SerializeField] bool reapplyOnRelicsChanged = true;

    [Header("Bindings")]
    [SerializeField] Binding[] bindings = new Binding[]
    {
        new Binding{ name="Player ATK TH", stat=StatId.AttackThreshold, actor=Actor.Player, phase=PhaseKind.Attack },
        new Binding{ name="Player MAX HP", stat=StatId.MaxHealth,       actor=Actor.Player, phase=PhaseKind.Attack },
    };

    GameDirector director;
    RelicManager relics;

    void ResolveRefs()
    {
        if (!director) director = GameDirector.Instance ?? FindFirstObjectByType<GameDirector>(FindObjectsInactive.Include);
        if (!relics)   relics   = RelicManager.Instance ?? FindFirstObjectByType<RelicManager>(FindObjectsInactive.Include);
    }

    void Start()
    {
        ResolveRefs();
        if (applyOnStart) StartCoroutine(ApplyRoutine());
        if (reapplyOnRelicsChanged && relics != null)
            relics.OnRelicsChanged += HandleRelicsChanged;
    }

    void OnDestroy()
    {
        if (reapplyOnRelicsChanged && relics != null)
            relics.OnRelicsChanged -= HandleRelicsChanged;
    }

    void HandleRelicsChanged() => StartCoroutine(ApplyRoutine());

    IEnumerator ApplyRoutine()
    {
        for (int i = 0; i < delayFrames; i++) yield return null;
        ResolveRefs();

        if (director?.Ctx == null || relics == null) yield break;

        foreach (var b in bindings)
        {
            float baseVal = GetBase(director, b);
            float finalVal = relics.ApplyStatModifiers(b.stat, baseVal, b.actor, b.phase);
            ApplyFinal(director, b, finalVal);
        }
    }
    [SerializeField] bool reapplyOnTurnStart = false;

    void OnEnable()
    {
        if (reapplyOnTurnStart)
            GameDirector.ContextReady += () =>
            {
                var gd = GameDirector.Instance;
                // GameDirector'a küçük bir event ekleyebilir ya da BeginPhase/StartNewTurn içinde çağrılan bir UnityEvent’e abone olabilirsin.
            };
    }
    public void ApplyNow()
    {
        StartCoroutine(ApplyRoutine());
    }
    float GetBase(GameDirector gd, Binding b)
    {
        if (b.source == BaseSource.Manual) return b.baseOverride;

        var sc   = gd.Player;
        var stat = sc ? sc.GetComponent<PlayerStats>()    : null;
        var hm   = sc ? sc.GetComponent<HealthManager>()  : null;

        switch (b.stat)
        {
            case StatId.AttackThreshold:
                if (stat) return Mathf.Max(1, stat.MaxAttackRange);
                return Mathf.Max(1, gd.Ctx.GetThreshold(b.actor, PhaseKind.Attack)); // fallback

            case StatId.DefenseThreshold:
                if (stat) return Mathf.Max(1, stat.MaxDefenseRange);
                return Mathf.Max(1, gd.Ctx.GetThreshold(b.actor, PhaseKind.Defense)); // fallback

            case StatId.MaxHealth:
                if (stat) return Mathf.Max(1, stat.MaxHealth);
                if (hm)   return Mathf.Max(1, hm.MaxHP);
                return 80f; // güvenli varsayılan

            case StatId.DamageMultiplier:
                return 1f;

            default:
                return 0f;
        }
    }


    void ApplyFinal(GameDirector gd, Binding b, float v)
    {
        if (b.applyTarget == ApplyTarget.Manual)
        {
            b.onApplied?.Invoke(v);
            return;
        }

        switch (b.stat)
        {
            case StatId.AttackThreshold:
                gd.Ctx.SetPhaseThreshold(b.actor, PhaseKind.Attack, Mathf.RoundToInt(v));
                // UI tazele (Context üzerinden)
                if (b.actor == Actor.Player)
                    gd.Ctx.OnProgress?.Invoke(
                        Actor.Player, PhaseKind.Attack,
                        gd.State?.PlayerAtkTotal ?? 0,
                        Mathf.RoundToInt(v)
                    );
                break;

            case StatId.DefenseThreshold:
                gd.Ctx.SetPhaseThreshold(b.actor, PhaseKind.Defense, Mathf.RoundToInt(v));
                if (b.actor == Actor.Player)
                    gd.Ctx.OnProgress?.Invoke(
                        Actor.Player, PhaseKind.Defense,
                        gd.State?.PlayerDefTotal ?? 0,
                        Mathf.RoundToInt(v)
                    );
                break;

            case StatId.MaxHealth:
            {
                var hm = gd.Player ? gd.Player.GetComponent<HealthManager>() : null;
                if (hm)
                {
                    int newMax = Mathf.RoundToInt(v);
                    float ratio = hm.CurrentHP / (float)Mathf.Max(1, hm.MaxHP);
                    hm.SetMaxHP(newMax);
                    hm.SetHP(Mathf.Clamp(Mathf.RoundToInt(newMax * ratio), 1, newMax));
                }
                else
                {
                    b.onApplied?.Invoke(v);
                }
                break;
            }

            case StatId.DamageMultiplier:
                b.onApplied?.Invoke(v);
                break;

            default:
                b.onApplied?.Invoke(v);
                break;
        }
    }

}
