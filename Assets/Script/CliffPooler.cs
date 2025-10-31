using UnityEngine;
using System.Collections.Generic;

public class CliffPooler : MonoBehaviour
{
    [Header("==== Prefab principal ====")]
    public GameObject cliffPrefab;
    public int cliffPoolSize = 5;
    public float cliffSpacing = 20f;
    public Vector3 cliffStartPosition = new Vector3(0f, 23f, 0f);
    public Vector3 cliffStartRotation = new Vector3(-90f, 0f, 0f);

    [Header("==== Prefab du rail ====")]
    public GameObject railPrefab;
    public int railPoolSize = 3;
    public float railSpacing = 25f;
    public Vector3 railStartPosition = new Vector3(0f, 53f, 0f);
    public Vector3 railStartRotation = new Vector3(-90f, 0f, 0f);

    [Header("Caméra cible (suivi)")]
    public Camera targetCamera;

    private bool hasCamera;
    private List<GameObject> cliffPool = new List<GameObject>();
    private List<GameObject> railPool = new List<GameObject>();

    private int cliffIndex = 0;
    private int railIndex = 0;
    private const uint RailRenderingLayerMask = 4294967295;

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        hasCamera = !ReferenceEquals(targetCamera, null);
    }

    void Start()
    {
        // Pool des cliffs
        for (int i = 0; i < cliffPoolSize; i++)
        {
            Vector3 pos = cliffStartPosition + new Vector3(i * cliffSpacing, 0f, 0f);
            Quaternion rot = Quaternion.Euler(cliffStartRotation);
            GameObject obj = Instantiate(cliffPrefab, pos, rot, transform);
            cliffPool.Add(obj);
        }

        // Pool des rails
        if (railPrefab != null)
        {
            for (int i = 0; i < railPoolSize; i++)
            {
                Vector3 pos = railStartPosition + new Vector3(i * railSpacing, 0f, 0f);
                Quaternion rot = Quaternion.Euler(railStartRotation);
                GameObject obj = Instantiate(railPrefab, pos, rot, transform);
                railPool.Add(obj);
                ApplyRailRenderingLayerMask(obj);
            }
        }
    }

    void Update()
    {
        if (!hasCamera) return;

        float camX = targetCamera.transform.position.x;

        // --- Pool Cliffs ---
        GameObject firstCliff = cliffPool[cliffIndex];
        if (camX - firstCliff.transform.position.x > cliffSpacing * 2)
        {
            GameObject obj = cliffPool[cliffIndex];
            float newX = cliffPool[(cliffIndex + cliffPoolSize - 1) % cliffPoolSize].transform.position.x + cliffSpacing;
            obj.transform.position = new Vector3(newX, cliffStartPosition.y, cliffStartPosition.z);
            obj.transform.rotation = Quaternion.Euler(cliffStartRotation);
            cliffIndex = (cliffIndex + 1) % cliffPoolSize;
        }

        // --- Pool Rails ---
        if (railPrefab != null && railPool.Count > 0)
        {
            GameObject firstRail = railPool[railIndex];
            if (camX - firstRail.transform.position.x > railSpacing * 2)
            {
                GameObject obj = railPool[railIndex];
                float furthestRailX = GetFurthestRailX();
                float newX = furthestRailX + railSpacing;
                obj.transform.position = new Vector3(newX, railStartPosition.y, railStartPosition.z);
                obj.transform.rotation = Quaternion.Euler(railStartRotation);
                railIndex = (railIndex + 1) % railPoolSize;
            }
        }
    }

    private void ApplyRailRenderingLayerMask(GameObject railObj)
    {
        var renderers = railObj.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].renderingLayerMask = RailRenderingLayerMask;
        }
    }

    private float GetFurthestRailX()
    {
        float maxX = float.MinValue;
        for (int i = 0; i < railPool.Count; i++)
        {
            float railX = railPool[i].transform.position.x;
            if (railX > maxX)
                maxX = railX;
        }

        return maxX == float.MinValue ? railStartPosition.x : maxX;
    }
}
