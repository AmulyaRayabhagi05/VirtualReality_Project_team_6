using UnityEngine;

/// <summary>
/// Temporary debug script — attach to your Character to see if ANY collision is detected.
/// Check the Console window when you hit a mountain.
/// Delete this script once collisions are confirmed working.
/// </summary>
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
