using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class RelicIconView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Refs")]
    [SerializeField] private Image iconImage;                 // ZORUNLU: Prefab'da ataman en sağlıklısı
    [SerializeField] private GameObject stackBadge;           // opsiyonel
    [SerializeField] private TextMeshProUGUI stackText;       // opsiyonel
    [Header("Fallbacks")]
    [SerializeField] private Sprite placeholderIcon;          // opsiyonel: icon yoksa

    private RelicRuntime runtime;

    void Reset()
    {
        // Otomatik bulma (ama prefab'da açıkça atamanı öneririm)
        if (!iconImage) iconImage = GetComponentInChildren<Image>(true);
        if (!stackText && stackBadge) stackText = stackBadge.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void Bind(RelicRuntime r)
    {
        runtime = r;

        // Güçlü null guard
        if (runtime == null)
        {
            Debug.LogWarning("[RelicIconView] Bind null runtime.");
            SetIcon(placeholderIcon);
            SetStack(0);
            return;
        }

        var def = runtime.def;
        if (def == null)
        {
            Debug.LogWarning("[RelicIconView] RelicRuntime.def is null.");
            SetIcon(placeholderIcon);
            SetStack(runtime.stacks);
            return;
        }

        // Icon
        SetIcon(def.icon != null ? def.icon : placeholderIcon);

        // Stack
        SetStack(runtime.stacks);
    }

    private void SetIcon(Sprite s)
    {
        if (!iconImage)
        {
            Debug.LogWarning($"[RelicIconView] iconImage missing on {name}. Assign in prefab.");
            return;
        }
        iconImage.sprite = s;
        iconImage.enabled = (s != null);
    }

    private void SetStack(int stacks)
    {
        bool show = stacks > 1;
        if (stackBadge) stackBadge.SetActive(show);
        if (stackText)  stackText.text = show ? stacks.ToString() : string.Empty;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (runtime?.def == null) return;
        RelicTooltip.ShowFor(runtime, (RectTransform)transform);
    }

    public void OnPointerExit(PointerEventData e)
    {
        RelicTooltip.Hide();
    }
}
