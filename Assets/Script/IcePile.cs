using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class IcePile : MonoBehaviour
{
    public enum ImpactCase { Rails, Wagon }

    [Header("Fonte")]
    public float meltNeeded = 3.0f;
    public float progress;

    ImpactCase currentCase;
    bool isMelting;
    WagonController stoppedWagon;

    void Reset()
    {
        var rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
    }

    public void Initialize(ImpactCase c) { currentCase = c; }

    void Update()
    {
        HandleMouseOrTouch();

        if (!isMelting) return;

        progress += Time.deltaTime;

        if (progress >= meltNeeded)
        {
            if (stoppedWagon) stoppedWagon.ReleaseFromIce();
            Destroy(gameObject);
        }
    }


    void HandleMouseOrTouch()
    {
        bool pressing = false;
        Vector3 screenPos = Vector3.zero;

        // Souris
        if (Input.GetMouseButton(0))
        {
            pressing = true;
            screenPos = Input.mousePosition;
        }

        // Touch (mobile)
        if (Input.touchCount > 0)
        {
            pressing = true;
            screenPos = Input.GetTouch(0).position;
        }

        if (pressing)
        {
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            Debug.DrawRay(ray.origin, ray.direction * 50f, Color.cyan, 0.2f);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    if (!isMelting)
                    {
                        isMelting = true;
                        Debug.Log($"Début fonte glace : {name}");
                    }
                    return; // on reste en fonte
                }
            }
        }

        // Si on n’est plus sur la glace, on arrête de fondre
        if (isMelting)
        {
            isMelting = false;
            Debug.Log($"Arrêt fonte glace : {name}");
        }
    }

    void OnCollisionEnter(Collision other)
    {
        var wagon = other.collider.GetComponentInParent<WagonController>();
        if (!wagon) return;

        if (wagon.CanBreakIce())
        {
            wagon.ApplyBreakSlowdown();
            Destroy(gameObject);
        }
        else
        {
            wagon.StopDueToIce();
            stoppedWagon = wagon;
        }
    }
}
