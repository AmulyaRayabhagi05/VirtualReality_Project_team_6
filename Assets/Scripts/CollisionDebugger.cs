using UnityEngine;

public class CollisionDebugger : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[CollisionDebugger] OnCollisionEnter hit: {collision.gameObject.name} on layer {collision.gameObject.layer}");
    }

    void OnCollisionStay(Collision collision)
    {
        Debug.Log($"[CollisionDebugger] OnCollisionStay with: {collision.gameObject.name}");
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[CollisionDebugger] OnTriggerEnter hit: {other.gameObject.name} on layer {other.gameObject.layer}");
    }
}
