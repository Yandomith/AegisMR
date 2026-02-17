using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Finds nearby surfaces using trigger collider and feeds them to ProceduralLightningMesh
/// </summary>
public class LightningTargetingSystem : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The lightning mesh component to feed endpoints to")]
    public ProceduralLightningMesh lightningMesh;

    [Header("Detection Settings")]
    [Tooltip("Radius to search for nearby surfaces")]
    public float detectionRadius = 2f;
    [Tooltip("Which layers to detect (e.g., walls, objects)")]
    public LayerMask targetLayers = ~0; // All layers by default
    [Tooltip("Maximum number of surface points to target")]
    public int maxTargets = 3;

    [Header("Surface Point Update")]
    [Tooltip("How often to recalculate surface points for objects in range (seconds)")]
    public float surfaceUpdateInterval = 0.1f;

    [Header("Retraction Settings")]
    [Tooltip("How fast lightning retracts back to start point (units/second)")]
    public float retractSpeed = 8f;
    [Tooltip("Distance from start point to consider fully retracted")]
    public float retractThreshold = 0.05f;

    // Internal - trigger detection
    private SphereCollider triggerCollider;
    private HashSet<Collider> collidersInRange = new HashSet<Collider>();
    private bool isDirty = true;

    // Internal - active target points (linked to colliders still in range)
    private float surfaceUpdateTimer = 0f;
    private Dictionary<Collider, GameObject> activeTargets = new Dictionary<Collider, GameObject>();

    // Internal - retracting points (no longer have a collider, moving back to origin)
    private List<GameObject> retractingPoints = new List<GameObject>();

    // Cached reference for mesh visibility
    private MeshRenderer lightningMeshRenderer;

    void Start()
    {
        if (lightningMesh == null)
        {
            lightningMesh = GetComponent<ProceduralLightningMesh>();
        }

        if (lightningMesh != null)
        {
            lightningMeshRenderer = lightningMesh.GetComponent<MeshRenderer>();
        }

        SetupTriggerCollider();
    }

    void SetupTriggerCollider()
    {
        triggerCollider = gameObject.AddComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = detectionRadius;

        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void Update()
    {
        if (lightningMesh == null) return;

        // Late-cache mesh renderer (in case it was created in lightning mesh's Start)
        if (lightningMeshRenderer == null)
        {
            lightningMeshRenderer = lightningMesh.GetComponent<MeshRenderer>();
        }

        Vector3 origin = Vector3.zero;
        if (lightningMesh.startPoint != null)
        {
            transform.position = lightningMesh.startPoint.position;
            origin = lightningMesh.startPoint.position;
        }

        // Update active surface points periodically
        surfaceUpdateTimer += Time.deltaTime;
        if ((isDirty || surfaceUpdateTimer >= surfaceUpdateInterval) && collidersInRange.Count > 0)
        {
            surfaceUpdateTimer = 0f;
            isDirty = false;
            UpdateActiveSurfacePoints();
        }

        // Update retracting points
        UpdateRetractingPoints(origin);

        // Combine active and retracting points for lightning
        UpdateLightningEndpoints();
    }

    void OnTriggerEnter(Collider other)
    {
        if ((targetLayers.value & (1 << other.gameObject.layer)) == 0) return;
        if (lightningMesh != null && other.transform == lightningMesh.startPoint) return;

        collidersInRange.Add(other);
        isDirty = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (collidersInRange.Remove(other))
        {
            // Move this target to retracting list
            if (activeTargets.TryGetValue(other, out GameObject targetObj))
            {
                activeTargets.Remove(other);
                if (targetObj != null)
                {
                    retractingPoints.Add(targetObj);
                }
            }
            isDirty = true;
        }
    }

    void UpdateActiveSurfacePoints()
    {
        if (lightningMesh == null || lightningMesh.startPoint == null) return;

        Vector3 origin = lightningMesh.startPoint.position;

        // Remove null/destroyed colliders
        List<Collider> toRemove = new List<Collider>();
        foreach (var kvp in activeTargets)
        {
            if (kvp.Key == null)
            {
                toRemove.Add(kvp.Key);
                if (kvp.Value != null) retractingPoints.Add(kvp.Value);
            }
        }
        foreach (var col in toRemove) activeTargets.Remove(col);

        collidersInRange.RemoveWhere(c => c == null);

        // Sort by distance
        List<Collider> sortedColliders = new List<Collider>(collidersInRange);
        sortedColliders.Sort((a, b) =>
            Vector3.Distance(origin, a.ClosestPoint(origin)).CompareTo(
            Vector3.Distance(origin, b.ClosestPoint(origin)))
        );

        // Track which colliders we're keeping
        HashSet<Collider> keptColliders = new HashSet<Collider>();

        int count = 0;
        foreach (Collider col in sortedColliders)
        {
            if (count >= maxTargets) break;
            if (col == null) continue;

            Vector3 closestPoint = col.ClosestPoint(origin);
            if (Vector3.Distance(closestPoint, origin) < 0.01f) continue;

            // Update existing or create new
            if (activeTargets.TryGetValue(col, out GameObject existingObj))
            {
                if (existingObj != null)
                {
                    existingObj.transform.position = closestPoint;
                    keptColliders.Add(col);
                    count++;
                }
            }
            else
            {
                GameObject pointObj = new GameObject($"LightningTarget_{count}");
                pointObj.transform.position = closestPoint;
                pointObj.transform.parent = transform;
                activeTargets[col] = pointObj;
                keptColliders.Add(col);
                count++;
            }
        }

        // Move excess targets to retracting
        List<Collider> excess = new List<Collider>();
        foreach (var kvp in activeTargets)
        {
            if (!keptColliders.Contains(kvp.Key))
            {
                excess.Add(kvp.Key);
                if (kvp.Value != null) retractingPoints.Add(kvp.Value);
            }
        }
        foreach (var col in excess) activeTargets.Remove(col);
    }

    void UpdateRetractingPoints(Vector3 origin)
    {
        for (int i = retractingPoints.Count - 1; i >= 0; i--)
        {
            GameObject point = retractingPoints[i];
            if (point == null)
            {
                retractingPoints.RemoveAt(i);
                continue;
            }

            // Move toward origin
            Vector3 currentPos = point.transform.position;
            Vector3 direction = (origin - currentPos).normalized;
            float distance = Vector3.Distance(currentPos, origin);

            if (distance <= retractThreshold)
            {
                // Fully retracted - remove
                Destroy(point);
                retractingPoints.RemoveAt(i);
            }
            else
            {
                // Move toward origin
                float moveAmount = retractSpeed * Time.deltaTime;
                if (moveAmount >= distance)
                {
                    point.transform.position = origin;
                }
                else
                {
                    point.transform.position = currentPos + direction * moveAmount;
                }
            }
        }
    }

    void UpdateLightningEndpoints()
    {
        List<Transform> allEndpoints = new List<Transform>();

        // Add active targets
        foreach (var kvp in activeTargets)
        {
            if (kvp.Value != null)
            {
                allEndpoints.Add(kvp.Value.transform);
            }
        }

        // Add retracting points
        foreach (GameObject point in retractingPoints)
        {
            if (point != null)
            {
                allEndpoints.Add(point.transform);
            }
        }

        lightningMesh.endPoints = allEndpoints.ToArray();

        // Hide mesh when no endpoints exist
        if (lightningMeshRenderer != null)
        {
            lightningMeshRenderer.enabled = allEndpoints.Count > 0;
        }
    }

    void ClearAllPoints()
    {
        foreach (var kvp in activeTargets)
        {
            if (kvp.Value != null) Destroy(kvp.Value);
        }
        activeTargets.Clear();

        foreach (GameObject obj in retractingPoints)
        {
            if (obj != null) Destroy(obj);
        }
        retractingPoints.Clear();
    }

    void OnDestroy()
    {
        ClearAllPoints();
    }

    public void SetDetectionRadius(float radius)
    {
        detectionRadius = radius;
        if (triggerCollider != null)
        {
            triggerCollider.radius = radius;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
