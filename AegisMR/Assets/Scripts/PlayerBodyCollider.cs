using UnityEngine;

/// <summary>
/// Attach this to the CenterEyeAnchor (inside OVRCameraRig > TrackingSpace).
/// It registers hits from incoming arrows and forwards damage to PlayerHealth.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerBodyCollider : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The PlayerHealth component in the scene.")]
    public PlayerHealth playerHealth;

    [Header("Settings")]
    [Tooltip("Radius of the hit sphere around the player's head (metres).")]
    [Range(0.1f, 0.5f)]
    public float colliderRadius = 0.2f;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        // Sphere trigger around the player's head
        SphereCollider sphere  = GetComponent<SphereCollider>();
        sphere.isTrigger       = true;
        sphere.radius          = colliderRadius;

        // Kinematic rigidbody required for trigger callbacks on a moving object
        Rigidbody rb  = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;
    }

    void Start()
    {
        // Auto-find PlayerHealth if not assigned
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    // ─────────────────────────────────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        Arrow arrow = other.GetComponent<Arrow>();
        if (arrow == null) return;

        Debug.Log("[PlayerBodyCollider] Hit by arrow!");

        if (playerHealth != null)
            playerHealth.TakeDamage(arrow.damage);

        arrow.OnHit();
    }
}
