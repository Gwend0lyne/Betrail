using UnityEngine;

public class NetFollowerAlongRail : MonoBehaviour
{
    [Header("Rails")]
    public Transform railStart;
    public Transform railEnd;
    public Transform railYRef;

    [Header("Filet 3D")]
    public Transform worldParent;       // ex: grotte
    public GameObject filet3DPrefab;    // doit/recevra BoxCollider
    public Vector3 filetWorldSize = new Vector3(2f, 12f, 0.6f);
    public float yAlignOffset = 0f;

    private GameObject filet3DInstance;

    public void SetT(float t01)
    {
        Debug.Log("SetT: " + t01);
        t01 = Mathf.Clamp01(t01);
        EnsureInstance();
        if (!filet3DInstance || !railStart || !railEnd) return;

        Vector3 pos = Vector3.Lerp(railStart.position, railEnd.position, t01);
        if (railYRef) pos.y = railYRef.position.y + yAlignOffset;
        pos.z = railStart.position.z;

        filet3DInstance.transform.position = pos;

        var col = filet3DInstance.GetComponent<BoxCollider>() ?? filet3DInstance.AddComponent<BoxCollider>();
        col.size = filetWorldSize;
        col.center = Vector3.zero;

        filet3DInstance.tag = "Filet";
        Debug.DrawLine(railStart.position, railEnd.position, Color.cyan);
        Debug.DrawRay(pos, Vector3.up * 2f, Color.green);
    }

    private void EnsureInstance()
    {
        if (filet3DInstance || !worldParent || !filet3DPrefab) return;
        filet3DInstance = Instantiate(filet3DPrefab, worldParent);
    }
}