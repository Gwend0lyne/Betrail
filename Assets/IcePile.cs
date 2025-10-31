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
        if (!isMelting) return;
        progress += Time.deltaTime;
        if (progress >= meltNeeded)
        {
            if (stoppedWagon) stoppedWagon.ReleaseFromIce();
            Destroy(gameObject);
        }
    }

    void OnMouseDown() { isMelting = true; }
    void OnMouseUp()   { isMelting = false; }

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