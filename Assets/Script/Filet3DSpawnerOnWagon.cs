using UnityEngine;

[RequireComponent(typeof(PlayingAreaQuadBuilder))]
public class Filet3DSpawnerOnWagon : MonoBehaviour
{
    [Header("Références")]
    public Transform wagon;              // le wagon dans la grotte
    public Transform worldParent;        // la grotte (parent de la copie)
    public GameObject filet3DPrefab;     // ton prefab Filet3D avec collider + tag "Filet"

    [Header("Placement")]
    public Vector3 localOffset = new Vector3(0f, 5f, 0f); // position relative au wagon
    public Vector3 filetSize = new Vector3(2f, 12f, 0.6f);

    private PlayingAreaQuadBuilder builder;
    private GameObject filetInstance;

    void Awake()
    {
        builder = GetComponent<PlayingAreaQuadBuilder>();
    }

    void Update()
    {
        if (builder == null || !wagon || !filet3DPrefab || !worldParent)
            return;

        // Si le filet 2D est visible (les 4 doigts sont posés)
        if (builder.HasValidQuad())
        {
            // Créer la version 3D si elle n'existe pas déjà
            if (filetInstance == null)
            {
                filetInstance = Instantiate(filet3DPrefab, worldParent);
                filetInstance.name = "Filet3D(Clone)";
                filetInstance.tag = "Filet";

                var col = filetInstance.GetComponent<BoxCollider>() ?? filetInstance.AddComponent<BoxCollider>();
                col.size = filetSize;
                col.center = Vector3.zero;
                col.isTrigger = false;
            }

            // Suivre la position du wagon
            filetInstance.transform.position = wagon.position + localOffset;
        }
        else
        {
            // Si le joueur relâche les doigts, on détruit le clone
            if (filetInstance != null)
            {
                Destroy(filetInstance);
                filetInstance = null;
            }
        }
    }
}
