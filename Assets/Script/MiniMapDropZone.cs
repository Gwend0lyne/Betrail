using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class MiniMapDropZone : MonoBehaviour, IDropHandler
{
    [Header("Icone à créer sur la minimap")]
    public RectTransform iconPrefab;

    private RectTransform zone;

    void Awake() { zone = GetComponent<RectTransform>(); }

    public void OnDrop(PointerEventData eventData)
    {
        var dragObj = eventData.pointerDrag;
        if (dragObj == null) return;

        var stalactik = dragObj.GetComponent<StalactikInteraction>();
        if (stalactik == null || !stalactik.unlocked)
        {
            Debug.Log("Drop refusé: Stalactik non débloqué.");
            return;
        }

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                zone, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            // Instancie l’icône sur la mini-map
            RectTransform icon = Instantiate(iconPrefab, zone);
            icon.anchoredPosition = localPoint;

            // (Option) clamp dans la zone
            var half = zone.rect.size / 2f;
            var p = icon.anchoredPosition;
            p.x = Mathf.Clamp(p.x, -half.x, half.x);
            p.y = Mathf.Clamp(p.y, -half.y, half.y);
            icon.anchoredPosition = p;

            // ➜ Usage unique : on "consume" le stalactik d’origine
            stalactik.Consume();

            Debug.Log("Stalactik placé sur la mini map ✅ (usage unique).");
        }
    }
}