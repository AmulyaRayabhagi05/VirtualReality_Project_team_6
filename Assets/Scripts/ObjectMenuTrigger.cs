using UnityEngine;
public class ObjectMenuTrigger : MonoBehaviour
{
    public void TryOpenMenu()
    {
        if (ObjectMenuManager.instance != null)
        {
            ObjectMenuManager.instance.TryOpenMenu(this.gameObject);
        }
    }
}
