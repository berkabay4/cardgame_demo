using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Map
{
    public enum NodeStates
    {
        Locked,
        Visited,
        Attainable
    }
}

namespace Map
{
    public class MapNode : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public SpriteRenderer sr;
        public Image image;
        public SpriteRenderer visitedCircle;
        public Image circleImage;
        public Image visitedCircleImage;

        public Node Node { get; private set; }
        public NodeBlueprint Blueprint { get; private set; }
        private MapView owner;
        private float initialScale;
        private const float HoverScaleFactor = 1.2f;
        private float mouseDownTime;

        private const float MaxClickDuration = 0.5f;

 public void SetUp(Node node, NodeBlueprint blueprint, MapView ownerView)
{
    Node = node;
    Blueprint = blueprint;
    owner = ownerView;

    if (owner == null)
        Debug.LogWarning($"[MapNode] owner(MapView) is NULL for node type={node?.nodeType}");

    if (blueprint == null)
    {
        Debug.LogWarning($"[MapNode] Blueprint is NULL for type={node?.nodeType}. Applying visual fallback.");
        ApplyVisualFallback();      // ← EKLENDİ
        SetState(NodeStates.Locked);
        return;                     // blueprint yoksa erken çık (kırma)
    }

    if (sr != null) sr.sprite = blueprint.sprite;
    if (image != null) image.sprite = blueprint.sprite;

    if (node != null && node.nodeType == NodeType.Boss) transform.localScale *= 1.5f;

    if (sr != null) initialScale = sr.transform.localScale.x;
    if (image != null) initialScale = image.transform.localScale.x;

    if (visitedCircle != null)
    {
        if (owner != null) visitedCircle.color = owner.visitedColor;
        visitedCircle.gameObject.SetActive(false);
    }

    if (circleImage != null)
    {
        if (owner != null) circleImage.color = owner.visitedColor;
        circleImage.gameObject.SetActive(false);
    }

    SetState(NodeStates.Locked);
}
        public void SetState(NodeStates state)
        {
            if (visitedCircle != null) visitedCircle.gameObject.SetActive(false);
            if (circleImage  != null)  circleImage.gameObject.SetActive(false);

            // Güvenli renk alma helper’ı
            Color locked = owner ? owner.lockedColor : Color.gray;
            Color visited = owner ? owner.visitedColor : Color.white;

            switch (state)
            {
                case NodeStates.Locked:
                    if (sr != null)   { sr.DOKill();   sr.color   = locked; }
                    if (image != null){ image.DOKill(); image.color= locked; }
                    break;

                case NodeStates.Visited:
                    if (sr != null)   { sr.DOKill();   sr.color   = visited; }
                    if (image != null){ image.DOKill(); image.color= visited; }
                    if (visitedCircle != null) visitedCircle.gameObject.SetActive(true);
                    if (circleImage != null)    circleImage.gameObject.SetActive(true);
                    break;

                case NodeStates.Attainable:
                    if (sr != null) {
                        sr.DOKill();
                        sr.color = locked;
                        sr.DOColor(visited, 0.5f).SetLoops(-1, LoopType.Yoyo);
                    }
                    if (image != null) {
                        image.DOKill();
                        image.color = locked;
                        image.DOColor(visited, 0.5f).SetLoops(-1, LoopType.Yoyo);
                    }
                    break;
            }
        }

        public void OnPointerEnter(PointerEventData data)
        {
            if (sr != null)
            {
                sr.transform.DOKill();
                sr.transform.DOScale(initialScale * HoverScaleFactor, 0.3f);
            }

            if (image != null)
            {
                image.transform.DOKill();
                image.transform.DOScale(initialScale * HoverScaleFactor, 0.3f);
            }
        }

        public void OnPointerExit(PointerEventData data)
        {
            if (sr != null)
            {
                sr.transform.DOKill();
                sr.transform.DOScale(initialScale, 0.3f);
            }

            if (image != null)
            {
                image.transform.DOKill();
                image.transform.DOScale(initialScale, 0.3f);
            }
        }

        public void OnPointerDown(PointerEventData data)
        {
            mouseDownTime = Time.time;
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (Time.time - mouseDownTime < MaxClickDuration)
            {
                // user clicked on this node:
                MapPlayerTracker.Instance.SelectNode(this);
            }
        }

        public void ShowSwirlAnimation()
        {
            if (visitedCircleImage == null)
                return;

            const float fillDuration = 0.3f;
            visitedCircleImage.fillAmount = 0;

            DOTween.To(() => visitedCircleImage.fillAmount, x => visitedCircleImage.fillAmount = x, 1f, fillDuration);
        }
    private void ApplyVisualFallback()
    {
        Color locked = Color.magenta;
        if (owner != null) locked = (Color)owner.lockedColor; // Color32 -> Color cast

        if (sr != null)
        {
            sr.DOKill();
            sr.sprite = null;                // boş bırak
            sr.color = locked;
        }
        if (image != null)
        {
            image.DOKill();
            image.sprite = null;
            image.color = locked;
        }

        if (visitedCircle != null) visitedCircle.gameObject.SetActive(false);
        if (circleImage != null)  circleImage.gameObject.SetActive(false);
    }
        private void OnDestroy()
        {
            if (image != null)
            {
                image.transform.DOKill();
                image.DOKill();
            }

            if (sr != null)
            {
                sr.transform.DOKill();
                sr.DOKill();
            }
        }
    }
}
