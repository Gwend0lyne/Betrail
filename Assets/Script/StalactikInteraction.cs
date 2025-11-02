using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class StalactikInteraction : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Unlocked appearance")]
    public Image mainImage;
    public Sprite unlockedSprite;
    
    [Header("Progress (optional UI)")]
    public Image progressFill;

    [Header("Drag settings")]
    public Canvas uiCanvas;
    public bool unlocked = false;

    private float progress = 0f;  // 0..1
    private RectTransform rt;
    private CanvasGroup cg;
    private Transform originalParent;
    private Vector2 startPos;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        if (uiCanvas == null) uiCanvas = GetComponentInParent<Canvas>();
        UpdateProgressUI();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (unlocked) return;
        progress = Mathf.Min(1f, progress + 0.20f);
        Debug.Log($"Stalactik progress: {(int)(progress * 100)}%");
        if (progress >= 1f)
        {
            unlocked = true;
            Debug.Log("Stalactik UNLOCKED ✅"); 
            if (mainImage && unlockedSprite)
            {
                mainImage.sprite = unlockedSprite; // changement instantané
            }
        }
        UpdateProgressUI();
    }

    private void UpdateProgressUI()
    {
        if (progressFill != null) progressFill.fillAmount = progress;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!unlocked) return;
        originalParent = rt.parent;
        startPos = rt.anchoredPosition;
        cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!unlocked) return;
        rt.anchoredPosition += eventData.delta / uiCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!unlocked) return;
        // Si pas déposé sur une DropZone, on revient à la place d'origine
        rt.anchoredPosition = startPos;
        rt.SetParent(originalParent, false);
        cg.blocksRaycasts = true;
    }

    // ➜ Appelée par la DropZone quand le drop est validé
    public void Consume()
    {
        // Usage unique : on désactive l’objet (ou Destroy(gameObject); si tu préfères)
        gameObject.SetActive(false);
    }
}
