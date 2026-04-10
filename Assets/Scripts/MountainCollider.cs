using UnityEngine;

public class MountainCollider : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Character")
        {
            Debug.Log("Mountain trigger hit!");
            other.GetComponent<CollisionDeathHandler>()?.TriggerDeath();
        }
    }
}