using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

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
        public UnityEvent<float> onApplied; // inspector fallback
    }

    public enum BaseSource { Auto, Manual }
    public enum ApplyTarget { Auto, Manual }

    [Header("When")]
    [SerializeField] bool applyOnStart = true;
    [SerializeField, Min(0)] int delayFrames = 1;
    [SerializeField] bool reapplyOnRelicsChanged = true;
    [SerializeField] bool reapplyOnTurnStart = false;

    [Header("Bindings")]
    [SerializeField] Binding[] bindings = new Binding[]
    {
        new Binding{ name="Player ATK TH", stat=StatId.AttackThreshold, actor=Actor.Player, phase=PhaseKind.Attack },
        new Binding{ name="Player MAX HP", stat=StatId.MaxHealth,       actor=Actor.Player, phase=PhaseKind.Attack },
    };

    CombatDirector combatDirector;
    RelicManager relics;

    void Awake()
    {
        ResolveRefs();
        // her sahne yüklendiğinde combat varsa tekrar uygula
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (reapplyOnRelicsChanged && relics != null)
            relics.OnRelicsChanged -= HandleRelicsChanged;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        if (applyOnStart) StartCoroutine(ApplyRoutine());
        if (reapplyOnRelicsChanged && relics != null)
            relics.OnRelicsChanged += HandleRelicsChanged;

        if (reapplyOnTurnStart)
            CombatDirector.ContextReady += OnCombatContextReady;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // CombatScene açıldığında tekrar relic değerlerini uygula
        StartCoroutine(ApplyRoutine());
    }

    void OnCombatContextReady()
    {
        // CombatDirector hazır olduğunda re-apply
        StartCoroutine(ApplyRoutine());
    }

    void HandleRelicsChanged() => StartCoroutine(ApplyRoutine());

    IEnumerator ApplyRoutine()
    {
        for (int i = 0; i < delayFrames; i++) yield return null;
        ResolveRefs();

        if (combatDirector?.Ctx == null || relics == null) yield break;

        foreach (var b in bindings)
        {
            float baseVal = GetBase(combatDirector, b);
            float finalVal = relics.ApplyStatModifiers(b.stat, baseVal, b.actor, b.phase);
            ApplyFinal(combatDirector, b, finalVal);
        }
    }

    void ResolveRefs()
    {
        if (!combatDirector) combatDirector = CombatDirector.Instance ?? FindFirstObjectByType<CombatDirector>(FindObjectsInactive.Include);
        if (!relics) relics = RelicManager.Instance ?? FindFirstObjectByType<RelicManager>(FindObjectsInactive.Include);
    }

    public void ApplyNow()
    {
        StartCoroutine(ApplyRoutine());
    }

    float GetBase(CombatDirector gd, Binding b)
    {
        if (b.source == BaseSource.Manual) return b.baseOverride;

        var sc   = gd.Player;
        var stat = sc ? sc.GetComponent<PlayerStats>()    : null;
        var hm   = sc ? sc.GetComponent<HealthManager>()  : null;

        switch (b.stat)
        {
            case StatId.AttackThreshold:
                if (stat) return Mathf.Max(1, stat.MaxAttackRange);
                return Mathf.Max(1, gd.Ctx.GetThreshold(b.actor, PhaseKind.Attack));

            case StatId.DefenseThreshold:
                if (stat) return Mathf.Max(1, stat.MaxDefenseRange);
                return Mathf.Max(1, gd.Ctx.GetThreshold(b.actor, PhaseKind.Defense));

            case StatId.MaxHealth:
                if (stat) return Mathf.Max(1, stat.MaxHealth);
                if (hm)   return Mathf.Max(1, hm.MaxHP);
                return 80f;

            case StatId.DamageMultiplier:
                return 1f;

            default:
                return 0f;
        }
    }

    void ApplyFinal(CombatDirector gd, Binding b, float v)
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
                if (b.actor == Actor.Player)
                    gd.Ctx.OnProgress?.Invoke(Actor.Player, PhaseKind.Attack,
                        gd.State?.PlayerAtkTotal ?? 0,
                        Mathf.RoundToInt(v));
                break;

            case StatId.DefenseThreshold:
                gd.Ctx.SetPhaseThreshold(b.actor, PhaseKind.Defense, Mathf.RoundToInt(v));
                if (b.actor == Actor.Player)
                    gd.Ctx.OnProgress?.Invoke(Actor.Player, PhaseKind.Defense,
                        gd.State?.PlayerDefTotal ?? 0,
                        Mathf.RoundToInt(v));
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
