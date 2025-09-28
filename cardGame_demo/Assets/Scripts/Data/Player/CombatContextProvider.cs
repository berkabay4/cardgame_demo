using UnityEngine;

public class CombatContextProvider : MonoBehaviour
{
    public CombatContext Context { get; private set; }

    [SerializeField] private int defaultThreshold = 21;

    private void Awake()
    {
        Context = new CombatContext(defaultThreshold);
        DontDestroyOnLoad(gameObject);
    }
}
