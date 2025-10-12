using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class FireOnTouchDown3D : MonoBehaviour, IPointerDownHandler
{
    public HeliCanonFloat owner;

    public void OnPointerDown(PointerEventData e)
    {
        owner?.Fire();             // démarre le laser continu
        gameObject.SetActive(false); // cache le texte tout de suite
        // pas d'attente de "click" (down+up) => pas de conflit avec le hold
    }
}