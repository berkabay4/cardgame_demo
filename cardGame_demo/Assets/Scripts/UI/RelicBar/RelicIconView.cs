using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class RelicIconView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Refs")]
    [SerializeField] private Image iconImage;        // ikon görseli
    [SerializeField] private TextMeshProUGUI stackText; // köşedeki sayı (1 ise gizle)
    [SerializeField] private GameObject stackBadge;  // rozet objesi (opsiyonel)

    private RelicRuntime _runtime;

    private void Reset()
    {
        if (!iconImage) iconImage = GetComponentInChildren<Image>();
        if (!stackText && stackBadge) stackText = stackBadge.GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Bind(RelicRuntime runtime)
    {
        _runtime = runtime;

        var def = runtime.def;
        if (iconImage) iconImage.sprite = def.icon;

        if (stackText)
        {
            bool show = runtime.stacks > 1;
            stackText.text = show ? runtime.stacks.ToString() : string.Empty;
            if (stackBadge) stackBadge.SetActive(show);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_runtime == null) return;
        RelicTooltip.ShowFor(_runtime, (RectTransform)transform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        RelicTooltip.Hide();
    }
}
