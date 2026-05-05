using UnityEngine;
public class ObjectMenuTrigger : MonoBehaviour
{
    public void TryOpenMenu()
    {
        // Be forgiving: if the manager wasn't placed in this scene, try to find one.
        var mgr = ObjectMenuManager.instance != null
            ? ObjectMenuManager.instance
            : FindObjectOfType<ObjectMenuManager>();

        if (mgr != null)
        {
            ObjectMenuManager.instance = mgr;
            mgr.TryOpenMenu(this.gameObject);
            return;
        }

        // If this logs, the highlight/outline can still work, but the menu cannot open.
        Debug.LogWarning("[ObjectMenuTrigger] No ObjectMenuManager found in scene; cannot open object menu.", this);
    }
}
