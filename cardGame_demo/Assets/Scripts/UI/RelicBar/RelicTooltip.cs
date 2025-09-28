using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RelicTooltip : MonoBehaviour
{
    public static RelicTooltip Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private RectTransform panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private Image iconImage;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>();
        Hide();
    }

    public static void ShowFor(RelicRuntime runtime, RectTransform target)
    {
        if (Instance == null || runtime == null) return;

        var d = runtime.def;
        Instance.titleText.text = d.displayName;
        Instance.iconImage.sprite = d.icon;

        string desc = d.descriptionTemplate;
        desc = desc.Replace("{stacks}", runtime.stacks.ToString());
        Instance.descText.text = desc;

        Instance.panel.gameObject.SetActive(true);

        // konum: hedefin sağ üstü
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        var worldPos = corners[2]; // top-right
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)Instance.rootCanvas.transform,
            RectTransformUtility.WorldToScreenPoint(null, worldPos),
            Instance.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Instance.rootCanvas.worldCamera,
            out var localPoint
        );
        Instance.panel.anchoredPosition = localPoint + new Vector2(12f, -12f);
    }

    public static void Hide()
    {
        if (Instance == null) return;
        Instance.panel.gameObject.SetActive(false);
    }
}
