using UnityEngine;

public class PanelTrigger : MonoBehaviour
{
    public GameObject[] panelsToShow; // référence du grand panel

    public void TriggerPanels()
    {
        foreach (var panel in panelsToShow)
        {
            var isActive = panel.activeSelf;
            panel.SetActive(!isActive);
        }
        
    }
}
