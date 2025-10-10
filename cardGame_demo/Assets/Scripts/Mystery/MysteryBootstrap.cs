// MysteryBootstrap.cs
using UnityEngine;
using System;

[DisallowMultipleComponent]
public class MysteryBootstrap : MonoBehaviour
{
    [SerializeField] private bool autoStart = true;

    private void Start()
    {
        if (!autoStart) return;

        var gsd = GameSessionDirector.Instance;
        if (!gsd || gsd.CurrentMystery == null)
        {
            Debug.LogError("[MysteryBootstrap] CurrentMystery yok. Haritaya dönülüyor.");
            gsd?.ReturnToMap();
            return;
        }

        var data = gsd.CurrentMystery;
        var type = data.GetHandlerType();
        if (type == null)
        {
            Debug.LogError($"[MysteryBootstrap] Handler tipi çözülemedi: {data.HandlerTypeName}");
            gsd.ReturnToMap();
            return;
        }

        var go = new GameObject(type.Name);
        var comp = go.AddComponent(type) as IMystery;
        if (comp == null)
        {
            Debug.LogError($"[MysteryBootstrap] {type.Name} IMystery değil!");
            Destroy(go);
            gsd.ReturnToMap();
            return;
        }

        int seed = ResolveSeed(gsd);
        var ctx  = new MysteryContext(data, gsd.Run, gsd, new System.Random(seed));
        comp.Init(ctx);
    }

    private int ResolveSeed(GameSessionDirector gsd)
    {
        if (gsd?.Run != null && gsd.Run.pendingEncounter != null)
            return gsd.Run.pendingEncounter.seed;

        if (gsd?.CurrentMystery != null && !string.IsNullOrEmpty(gsd.CurrentMystery.id))
            return gsd.CurrentMystery.id.GetHashCode();

        return Environment.TickCount;
    }
}
