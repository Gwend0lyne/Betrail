using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PanelDuo : MonoBehaviour
{
    [Header("Gauche")]
    public Button boutonGauche;
    public RectTransform roueGauche;

    [Header("Droite")]
    public Button boutonDroite;
    public RectTransform roueDroite;

    [Header("Réglages")]
    [Tooltip("Accélérer la rotation appliquée pendant le drag.")]
    public float rotationMultiplier = 1f;

    // états “bouton maintenu”
    bool gaucheBoutonEnfonce = false;
    bool droiteBoutonEnfonce = false;
    public bool cheat = false;

    // mémos d'angle pendant le drag
    bool gaucheDragging = false;
    bool droiteDragging = false;
    float gaucheDernierAngle = 0f;
    float droiteDernierAngle = 0f;

    void Awake()
    {
        // On garde le log quand on clique (relâchement) mais ce n'est pas ce qui autorise la rotation.
        if (boutonGauche) boutonGauche.onClick.AddListener(() =>
            Debug.Log("[GAUCHE] Bouton cliqué."));
        if (boutonDroite) boutonDroite.onClick.AddListener(() =>
            Debug.Log("[DROITE] Bouton cliqué."));
    }

    // ---------- Boutons : évènements de maintien (via EventTrigger sur le bouton) ----------
    public void Gauche_BoutonPointerDown(BaseEventData _)
    {
        gaucheBoutonEnfonce = true;
        Debug.Log("[GAUCHE] Bouton maintenu.");
    }
    public void Gauche_BoutonPointerUp(BaseEventData _)
    {
        gaucheBoutonEnfonce = false;
        Debug.Log("[GAUCHE] Bouton relâché.");
    }

    public void Droite_BoutonPointerDown(BaseEventData _)
    {
        droiteBoutonEnfonce = true;
        Debug.Log("[DROITE] Bouton maintenu.");
    }
    public void Droite_BoutonPointerUp(BaseEventData _)
    {
        droiteBoutonEnfonce = false;
        Debug.Log("[DROITE] Bouton relâché.");
    }

    // ---------- ROUE GAUCHE ----------
    public void Gauche_OnPointerDown(BaseEventData data)
    {
        var ped = (PointerEventData)data;
        gaucheDernierAngle = AngleAutourDuCentre(roueGauche, ped.position);
        gaucheDragging = true;

        if (!gaucheBoutonEnfonce)
            Debug.Log("[GAUCHE] Maintiens le bouton rouge pour tourner !");
    }

    public void Gauche_OnDrag(BaseEventData data)
    {
        if (!gaucheDragging) return;

        var ped = (PointerEventData)data;
        float angleActuel = AngleAutourDuCentre(roueGauche, ped.position);
        float delta = Mathf.DeltaAngle(gaucheDernierAngle, angleActuel);

        // Tourne uniquement si le bouton est actuellement maintenu ET en sens horaire
        if (gaucheBoutonEnfonce && delta < 0f || cheat)
            roueGauche.Rotate(0f, 0f, delta * rotationMultiplier, Space.Self);

        gaucheDernierAngle = angleActuel;
    }

    public void Gauche_OnPointerUp(BaseEventData _)
    {
        gaucheDragging = false;
    }

    // ---------- ROUE DROITE ----------
    public void Droite_OnPointerDown(BaseEventData data)
    {
        var ped = (PointerEventData)data;
        droiteDernierAngle = AngleAutourDuCentre(roueDroite, ped.position);
        droiteDragging = true;

        if (!droiteBoutonEnfonce)
            Debug.Log("[DROITE] Maintiens le bouton rouge pour tourner !");
    }

    public void Droite_OnDrag(BaseEventData data)
    {
        if (!droiteDragging) return;

        var ped = (PointerEventData)data;
        float angleActuel = AngleAutourDuCentre(roueDroite, ped.position);
        float delta = Mathf.DeltaAngle(droiteDernierAngle, angleActuel);

        if (droiteBoutonEnfonce && delta < 0f || cheat) // horaire seulement
            roueDroite.Rotate(0f, 0f, delta * rotationMultiplier, Space.Self);

        droiteDernierAngle = angleActuel;
    }

    public void Droite_OnPointerUp(BaseEventData _)
    {
        droiteDragging = false;
    }

    // ---------- Utilitaires ----------
    static float AngleAutourDuCentre(RectTransform cible, Vector2 posEcran)
    {
        Vector2 centre = RectTransformUtility.WorldToScreenPoint(null, cible.position);
        Vector2 v = posEcran - centre;
        return Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
    }
}
