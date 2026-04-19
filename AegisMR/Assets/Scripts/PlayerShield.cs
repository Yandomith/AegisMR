using UnityEngine;

/// <summary>
/// A virtual shield that follows the player's left (or right) hand controller.
/// When an arrow enters the shield's collider, it is blocked and destroyed.
/// Attach this script to the Shield GameObject in your scene.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerShield : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The OVRCameraRig in the scene (auto-found if left blank).")]
    public OVRCameraRig cameraRig;

    [Tooltip("Optional haptics component for block feedback.")]
    public ControllerHaptics haptics;

    [Header("Hand Settings")]
    [Tooltip("True = left hand holds the shield; False = right hand.")]
    public bool useLeftHand = true;

    [Tooltip("Local position offset in front of the controller.")]
    public Vector3 positionOffset = new Vector3(0f, 0f, 0.05f);

    [Header("Shield Collider Size")]
    public Vector3 colliderSize = new Vector3(0.45f, 0.55f, 0.06f);

    // ── Internal ──────────────────────────────────────────────────────────────
    private Transform _handAnchor;

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        // Find camera rig
        if (cameraRig == null)
            cameraRig = FindFirstObjectByType<OVRCameraRig>();

        if (cameraRig != null)
            _handAnchor = useLeftHand ? cameraRig.leftHandAnchor : cameraRig.rightHandAnchor;
        else
            Debug.LogWarning("[PlayerShield] OVRCameraRig not found!");

        // Configure collider
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger   = true;
        box.size        = colliderSize;

        // Kinematic rigidbody required for trigger detection while moving
        Rigidbody rb  = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;
    }

    void Update()
    {
        if (_handAnchor == null) return;

        // Track hand controller position and rotation
        transform.position = _handAnchor.TransformPoint(positionOffset);
        transform.rotation = _handAnchor.rotation;
    }

    // ─────────────────────────────────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        Arrow arrow = other.GetComponent<Arrow>();
        if (arrow == null) return;

        Debug.Log("[PlayerShield] Arrow blocked!");

        // Haptic pulse on the shield hand
        if (haptics != null)
            haptics.Pulse();

        arrow.OnHit();
    }

    // ─────────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, colliderSize);
    }
#endif
}
