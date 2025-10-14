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

    [Header("Compteur (aiguille)")]
    public RectTransform aiguille;
    public float angleMin = 0f;      // 0%
    public float angleMax = -180f;   // 100%

    [Header("Réglages de rotation")]
    public float rotationMultiplier = 1f;
    [Tooltip("Vitesse (deg/s) considérée comme 'à fond' pour UNE manivelle.")]
    public float omegaFull = 180f;

    [Header("Lissage et synchro")]
    [Range(0f, 0.99f)] public float smoothing = 0.95f;  // pour l'aiguille & mesure
    [Range(0.05f, 0.5f)] public float syncTolerance = 0.2f;
    [Range(0f, 1f)] public float minActive = 0.1f;

    [Header("Courbe de montée/descente (ramp)")]
    [Tooltip("Cap si UNE seule manivelle tourne.")]
    public float singleCap = 40f;
    [Tooltip("Cap si les deux tournent mais PAS au même rythme.")]
    public float desyncCap = 50f;      // << demandé
    [Tooltip("Temps (s) pour atteindre singleCap à UNE manivelle à fond.")]
    public float singleRampSeconds = 3f; // << demandé
    [Tooltip("Temps (s) pour atteindre desyncCap quand les deux tournent mais pas synchro.")]
    public float desyncRampSeconds = 3f;
    [Tooltip("Temps (s) pour aller vers 100% quand synchro.")]
    public float syncedRampSeconds = 1.5f;
    [Tooltip("Temps (s) pour redescendre vers la cible quand on ralentit/lâche.")]
    public float fallSeconds = 0.8f;

    [Header("Debug / test")]
    public bool cheat = false;

    // états “bouton maintenu”
    bool gaucheBoutonEnfonce = false;
    bool droiteBoutonEnfonce = false;

    // drag
    bool gaucheDragging = false, droiteDragging = false;
    float gaucheDernierAngle = 0f, droiteDernierAngle = 0f;

    // vitesses filtrées (deg/s)
    float omegaL = 0f, omegaR = 0f;

    // vitesse logique & visuelle
    float speedPercent = 0f;     // sort du ramp (0..100)
    float uiPercent = 0f;        // aiguille lissée
    bool syncedLastFrame = false;
    public float SpeedPercent => uiPercent;

    void Awake()
    {
        if (boutonGauche) boutonGauche.onClick.AddListener(() => Debug.Log("[GAUCHE] Bouton cliqué."));
        if (boutonDroite) boutonDroite.onClick.AddListener(() => Debug.Log("[DROITE] Bouton cliqué."));
    }

    void Update()
    {
        // Amortissement naturel si on arrête de bouger
        float decay = Mathf.Pow(0.001f, Time.deltaTime);
        omegaL *= decay;
        omegaR *= decay;

        // --- Calcul des vitesses normalisées ---
        float vL = Mathf.Clamp01(Mathf.Abs(omegaL) / omegaFull);
        float vR = Mathf.Clamp01(Mathf.Abs(omegaR) / omegaFull);

        bool leftActive = vL > minActive;
        bool rightActive = vR > minActive;
        bool bothActive = leftActive && rightActive;

        bool sameDirection = Mathf.Sign(omegaL) == Mathf.Sign(omegaR);
        float relDiff = bothActive ? Mathf.Abs(vL - vR) / Mathf.Max(vL, vR) : 1f;
        bool synced = bothActive && sameDirection && relDiff <= syncTolerance;

        // --- Cible logique (avant ramp) + cap selon l'état ---
        float target;       // 0..100
        float cap;          // cap courant (40 / 50 / 100)
        float rampSeconds;  // temps de montée cible

        if (!leftActive && !rightActive)
        {
            target = 0f; cap = 0f; rampSeconds = fallSeconds;
        }
        else if (synced)
        {
            // moyenne → 0..100
            target = Mathf.Clamp01((vL + vR) * 0.5f) * 100f;
            cap = 100f;
            rampSeconds = syncedRampSeconds;
        }
        else if (leftActive ^ rightActive) // une seule
        {
            target = Mathf.Clamp01(Mathf.Max(vL, vR)) * singleCap;
            cap = singleCap;
            rampSeconds = singleRampSeconds;     // 0 → 40% en ~3s
        }
        else // les deux mais pas synchro
        {
            target = Mathf.Clamp01(Mathf.Max(vL, vR)) * desyncCap;
            cap = desyncCap;
            rampSeconds = desyncRampSeconds;     // 0 → 50% en ~3s (par défaut)
        }

        // --- RAMP (limiteur de pente) ---
        // Montée : vitesse max = cap / rampSeconds (%/s)
        // Descente : vitesse max = 100 / fallSeconds (%/s)
        float riseRate = cap > 0f && rampSeconds > 0f ? cap / rampSeconds : 9999f;
        float fallRate = fallSeconds > 0f ? 100f / fallSeconds : 9999f;
        float maxStep = (target > speedPercent ? riseRate : fallRate) * Time.deltaTime;
        speedPercent = Mathf.MoveTowards(speedPercent, target, maxStep);

        // Log au passage en synchro
        if (synced && !syncedLastFrame)
            Debug.Log("[VITESSE] Les deux tournent ensemble au même rythme !");
        syncedLastFrame = synced;

        // --- Aiguille (lissage visuel) ---
        uiPercent = Mathf.Lerp(uiPercent, speedPercent, 1f - smoothing);
        if (aiguille)
        {
            float t = uiPercent / 100f;
            float z = Mathf.Lerp(angleMin, angleMax, t);
            var e = aiguille.localEulerAngles; e.z = z; aiguille.localEulerAngles = e;
        }
    }

    // ---------- Pointer & Drag ----------
    public void Gauche_BoutonPointerDown(BaseEventData _) { gaucheBoutonEnfonce = true; }
    public void Gauche_BoutonPointerUp  (BaseEventData _) { gaucheBoutonEnfonce = false; }
    public void Droite_BoutonPointerDown(BaseEventData _) { droiteBoutonEnfonce = true; }
    public void Droite_BoutonPointerUp  (BaseEventData _) { droiteBoutonEnfonce = false; }

    public void Gauche_OnPointerDown(BaseEventData data)
    {
        var ped = (PointerEventData)data;
        gaucheDernierAngle = AngleAutourDuCentre(roueGauche, ped.position);
        gaucheDragging = true;
        if (!gaucheBoutonEnfonce) Debug.Log("[GAUCHE] Maintiens le bouton pour tourner !");
    }

    public void Gauche_OnDrag(BaseEventData data)
    {
        if (!gaucheDragging) return;
        var ped = (PointerEventData)data;
        float angleActuel = AngleAutourDuCentre(roueGauche, ped.position);
        float delta = Mathf.DeltaAngle(gaucheDernierAngle, angleActuel);

        if ((gaucheBoutonEnfonce && delta < 0f) || cheat)
        {
            roueGauche.Rotate(0f, 0f, delta * rotationMultiplier, Space.Self);
            float w = (delta * rotationMultiplier) / Mathf.Max(Time.deltaTime, 1e-5f);
            omegaL = Mathf.Lerp(omegaL, w, 1f - smoothing);
        }
        gaucheDernierAngle = angleActuel;
    }

    public void Gauche_OnPointerUp(BaseEventData _) { gaucheDragging = false; }

    public void Droite_OnPointerDown(BaseEventData data)
    {
        var ped = (PointerEventData)data;
        droiteDernierAngle = AngleAutourDuCentre(roueDroite, ped.position);
        droiteDragging = true;
        if (!droiteBoutonEnfonce) Debug.Log("[DROITE] Maintiens le bouton pour tourner !");
    }

    public void Droite_OnDrag(BaseEventData data)
    {
        if (!droiteDragging) return;
        var ped = (PointerEventData)data;
        float angleActuel = AngleAutourDuCentre(roueDroite, ped.position);
        float delta = Mathf.DeltaAngle(droiteDernierAngle, angleActuel);

        if ((droiteBoutonEnfonce && delta < 0f) || cheat)
        {
            roueDroite.Rotate(0f, 0f, delta * rotationMultiplier, Space.Self);
            float w = (delta * rotationMultiplier) / Mathf.Max(Time.deltaTime, 1e-5f);
            omegaR = Mathf.Lerp(omegaR, w, 1f - smoothing);
        }
        droiteDernierAngle = angleActuel;
    }

    public void Droite_OnPointerUp(BaseEventData _) { droiteDragging = false; }

    static float AngleAutourDuCentre(RectTransform cible, Vector2 posEcran)
    {
        Vector2 centre = RectTransformUtility.WorldToScreenPoint(null, cible.position);
        Vector2 v = posEcran - centre;
        return Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
    }
}
