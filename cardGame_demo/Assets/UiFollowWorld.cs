// Scripts/UI/UiFollowWorld.cs
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UiFollowWorld : MonoBehaviour
{
    public Transform worldTarget;
    public Camera cam;
    public Vector2 screenOffset = new(0f, 24f);

    RectTransform rect;

    void Awake()
    {
        rect = (RectTransform)transform;
        if (!cam) cam = Camera.main;
    }

    void LateUpdate()
    {
        if (!worldTarget || !cam) return;

        Vector3 sp = cam.WorldToScreenPoint(worldTarget.position);
        bool visible = sp.z > 0f
                    && sp.x >= 0 && sp.x <= Screen.width
                    && sp.y >= 0 && sp.y <= Screen.height;

        gameObject.SetActive(visible);
        if (visible) rect.position = (Vector2)sp + screenOffset;
    }
}
