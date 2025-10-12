using UnityEngine;

public class MoveLeft : MonoBehaviour
{
    [Tooltip("Vitesse en unités par seconde (positif = vers la gauche).")]
    public float speed = 1f;

    void Update()
    {
        // Déplacement continu vers la gauche en coordonnées monde
        transform.position += Vector3.left * speed * Time.deltaTime;
    }
}