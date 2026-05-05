using UnityEngine;

public class InfoStandTrigger : MonoBehaviour
{
    [Header("Info Content")]
    public string title;
    [TextArea(3, 10)]
    public string body;

    public void TryOpen()
    {
        var mgr = StandInfoPanelManager.instance != null
            ? StandInfoPanelManager.instance
            : FindObjectOfType<StandInfoPanelManager>();

        if (mgr == null)
        {
            var go = new GameObject("StandInfoPanelManager");
            mgr = go.AddComponent<StandInfoPanelManager>();
        }

        mgr.Toggle(title, body);
    }
}
