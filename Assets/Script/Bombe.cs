using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))] // pour recevoir les clics
public class Bomb : MonoBehaviour, IPointerClickHandler
{
    // Clic via EventSystem + PhysicsRaycaster (multi-touch friendly)
    public void OnPointerClick(PointerEventData eventData)
    {
        Explode();
    }

    // Fallback souris (si jamais l’EventSystem n’est pas présent)
    void OnMouseDown()
    {
        Explode();
    }

    void Explode()
    {
        Debug.Log("bombe qui explose");
        Destroy(gameObject);               // ou gameObject.SetActive(false) si tu préfères
    }
}