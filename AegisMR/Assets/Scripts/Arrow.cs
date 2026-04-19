using UnityEngine;

/// <summary>
/// Arrow projectile. Flies in a straight line toward the target direction set at spawn.
/// Destroyed on hitting Shield, PlayerBody, or after maxLifetime seconds.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class Arrow : MonoBehaviour
{
    [Header("Arrow Settings")]
    [Tooltip("Flight speed in units/second.")]
    public float speed = 10f;

    [Tooltip("Damage dealt to player on hit.")]
    public float damage = 25f;

    [Tooltip("Seconds before auto-destroy if nothing is hit.")]
    public float maxLifetime = 6f;

    // ── Internal ──────────────────────────────────────────────────────────────
    private Vector3 _direction;
    private bool    _hasHit   = false;
    private float   _lifetime = 0f;

    // ─────────────────────────────────────────────────────────────────────────
    // Setup
    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        // Kinematic rigidbody — we move it manually so physics doesn't deflect it
        Rigidbody rb  = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        // Trigger collider along the shaft
        CapsuleCollider cap = GetComponent<CapsuleCollider>();
        cap.isTrigger  = true;
        cap.direction  = 2;          // along Z axis (local forward)
        cap.radius     = 0.04f;
        cap.height     = 0.6f;
    }

    /// <summary>
    /// Call immediately after Instantiate to set the travel direction.
    /// </summary>
    public void Initialize(Vector3 travelDirection)
    {
        _direction          = travelDirection.normalized;
        transform.rotation  = Quaternion.LookRotation(_direction);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Update
    // ─────────────────────────────────────────────────────────────────────────

    void Update()
    {
        if (_hasHit) return;

        _lifetime += Time.deltaTime;
        if (_lifetime >= maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += _direction * (speed * Time.deltaTime);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Hit
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by PlayerBodyCollider or PlayerShield when this arrow is intercepted.
    /// </summary>
    public void OnHit()
    {
        if (_hasHit) return;
        _hasHit = true;
        Destroy(gameObject);
    }
}
