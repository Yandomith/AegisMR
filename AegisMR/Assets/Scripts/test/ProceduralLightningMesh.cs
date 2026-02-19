using UnityEngine;
using System.Collections.Generic;

public class ProceduralLightningMesh : MonoBehaviour
{
    [Header("Lightning Settings")]
    public int minBoltsPerTarget = 1; // Minimum bolts per target
    public int maxBoltsPerTarget = 3; // Maximum bolts per target
    [Tooltip("GameObject/Transform where lightning starts")]
    public Transform startPoint;
    [Tooltip("GameObjects/Transforms where lightning ends (can have multiple)")]
    public Transform[] endPoints;

    [Header("Distance Scaling")]
    [Tooltip("Enable automatic scaling based on distance")]
    public bool scaleWithDistance = true;
    [Tooltip("The distance these settings are tuned for (units)")]
    public float referenceDistance = 1f;
    [Tooltip("Minimum scale factor to prevent too thin at very short distances")]
    public float minScaleFactor = 0.2f;
    [Tooltip("Maximum scale factor to prevent too thick at very long distances")]
    public float maxScaleFactor = 5f;

    [Header("Base Visual Settings (at reference distance)")]
    public float spreadRadius = 0f; // Very tight clustering
    public int generations = 4; // Good detail level
    public float displacement = 0.2f; // Base displacement - tight
    public float roughness = 0.6f; // Moderate decay
    public float sway = 0.8f; // Balanced path bias
    public float lineWidth = 0.01f; // Thin main lines
    public bool taperThickness = false;
    public float thicknessFalloff = 0.4f;

    [Header("Crackling Effect")]
    [Tooltip("Chance of a sudden 'snap' displacement (0-1)")]
    public float snapProbability = 0.15f;
    [Tooltip("How much larger snap displacements are")]
    public float snapMultiplier = 3f;
    [Tooltip("Variation in bolt intensity (0=uniform, 1=varied)")]
    public float intensityVariation = 0.4f;

    [Header("Attack Mode")]
    [Tooltip("Use as attack (trigger on/off) instead of always active")]
    public bool attackMode = false;
    [Tooltip("How long the lightning stays active when fired (seconds)")]
    public float attackDuration = 0.5f;
    [Tooltip("Cooldown between attacks (seconds)")]
    public float attackCooldown = 1f;
    [Tooltip("Damage dealt per second to targets with IDamageable interface")]
    public float damagePerSecond = 10f;
    [Tooltip("Auto-find targets by tag instead of manual assignment")]
    public bool autoTargetByTag = false;
    [Tooltip("Tag to search for targets (e.g., 'Enemy')")]
    public string targetTag = "Enemy";
    [Tooltip("Max distance to auto-target")]
    public float targetRange = 10f;
    [Tooltip("Max number of auto-targets")]
    public int maxAutoTargets = 3;

    [Header("Animation")]
    public bool animate = true;
    [Tooltip("How often the overall lightning shape regenerates (seconds)")]
    public float shapeUpdateInterval = 0.3f; // Fast crackling reshapes
    [Tooltip("How often the sub-details/energy flicker (seconds)")]
    public float detailUpdateInterval = 0.02f; // Rapid micro-jitter
    [Tooltip("Amount of detail displacement for flickering")]
    public float detailDisplacement = 0.025f; // Subtle high-frequency noise

    [Header("Appearance")]
    public Color coreColor = new Color(0.7f, 1f, 1f, 1f); // Bright cyan-white core
    public Color edgeColor = new Color(0.2f, 0.6f, 1f, 1f); // Blue edge
    [Tooltip("Use additive blending for glow effect")]
    public bool additiveBlending = true;
    [Tooltip("Assign a VR-compatible material (recommended for Quest). If null, creates material at runtime.")]
    public Material lightningMaterial;

    private Mesh lightningMesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private float shapeTimer;
    private float detailTimer;
    private List<List<Vector3>> cachedBoltShapes; // Main shapes that persist
    private List<List<Vector3>> cachedBoltWidths; // Width data for each bolt
    private Vector3 lastStartPos;
    private List<Vector3> lastEndPositions = new List<Vector3>();
    private bool isActive = false;
    private float activeTimer = 0f;
    private float cooldownTimer = 0f;
    private HashSet<GameObject> damagedTargets = new HashSet<GameObject>();

    // Cached scaled values (computed per frame based on distance)
    private float scaledSpreadRadius;
    private float scaledDisplacement;
    private float scaledLineWidth;
    private float scaledDetailDisplacement;

    /// <summary>
    /// Calculate scale factor based on distance between two points
    /// </summary>
    private float GetDistanceScaleFactor(Vector3 start, Vector3 end)
    {
        if (!scaleWithDistance || referenceDistance <= 0) return 1f;

        float actualDistance = Vector3.Distance(start, end);
        float scaleFactor = actualDistance / referenceDistance;
        return Mathf.Clamp(scaleFactor, minScaleFactor, maxScaleFactor);
    }

    /// <summary>
    /// Update scaled values based on average distance to all endpoints
    /// </summary>
    private void UpdateScaledValues()
    {
        if (!scaleWithDistance || startPoint == null || endPoints == null || endPoints.Length == 0)
        {
            // Use base values
            scaledSpreadRadius = spreadRadius;
            scaledDisplacement = displacement;
            scaledLineWidth = lineWidth;
            scaledDetailDisplacement = detailDisplacement;
            return;
        }

        // Calculate average distance to all endpoints
        float totalDistance = 0f;
        int validCount = 0;
        foreach (Transform endPoint in endPoints)
        {
            if (endPoint != null)
            {
                totalDistance += Vector3.Distance(startPoint.position, endPoint.position);
                validCount++;
            }
        }

        if (validCount == 0)
        {
            scaledSpreadRadius = spreadRadius;
            scaledDisplacement = displacement;
            scaledLineWidth = lineWidth;
            scaledDetailDisplacement = detailDisplacement;
            return;
        }

        float avgDistance = totalDistance / validCount;
        float scale = Mathf.Clamp(avgDistance / referenceDistance, minScaleFactor, maxScaleFactor);

        // Apply scaling to distance-dependent parameters
        scaledSpreadRadius = spreadRadius * scale;
        scaledDisplacement = displacement * scale;
        scaledLineWidth = lineWidth * scale;
        scaledDetailDisplacement = detailDisplacement * scale;
    }

    void Start()
    {
        // Create mesh components
        lightningMesh = new Mesh();
        lightningMesh.name = "Lightning Mesh";

        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = lightningMesh;

        meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // Use assigned material if available (recommended for VR)
        if (lightningMaterial != null)
        {
            meshRenderer.material = new Material(lightningMaterial);
            meshRenderer.material.color = coreColor;
        }
        else
        {
            // Create lightning material at runtime (may not work on Quest)
            Material mat = null;
            
            if (additiveBlending)
            {
                // Try to find additive shader
                Shader shader = Shader.Find("Particles/Standard Unlit");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                
                if (shader != null)
                {
                    mat = new Material(shader);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_ZWrite", 0);
                    mat.renderQueue = 3000;
                    mat.enableInstancing = true;
                }
            }
            else
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                
                if (shader != null)
                {
                    mat = new Material(shader);
                }
            }
            
            if (mat != null)
            {
                mat.color = coreColor;
                meshRenderer.material = mat;
            }
            else
            {
                Debug.LogWarning("Lightning: Could not find shader. Assign a material in the Inspector for VR builds.");
            }
        }

        // Cache initial positions
        if (startPoint != null) lastStartPos = startPoint.position;
        if (endPoints != null)
        {
            foreach (Transform endPoint in endPoints)
            {
                if (endPoint != null) lastEndPositions.Add(endPoint.position);
            }
        }

        // In attack mode, start hidden
        if (attackMode)
        {
            meshRenderer.enabled = false;
            isActive = false;
        }
        else
        {
            isActive = true;
            GenerateLightningShape();
        }
    }

    void Update()
    {
        // Handle attack mode timing
        if (attackMode)
        {
            // Update cooldown
            if (cooldownTimer > 0)
            {
                cooldownTimer -= Time.deltaTime;
            }

            // Handle active lightning duration
            if (isActive)
            {
                activeTimer -= Time.deltaTime;

                // Deactivate when duration ends
                if (activeTimer <= 0)
                {
                    DeactivateLightning();
                }
                else
                {
                    // Continue updating while active
                    UpdateActiveLightning();

                    // Deal damage
                    DealDamageToTargets();
                }
            }

            return; // Skip normal update logic in attack mode
        }

        // Normal continuous mode
        if (!isActive) return;

        // Check if endpoints exist
        if (startPoint == null || endPoints == null || endPoints.Length == 0) return;

        // Check if any endpoint has moved
        bool positionChanged = false;

        // Check start point
        if (lastStartPos != startPoint.position)
        {
            positionChanged = true;
            lastStartPos = startPoint.position;
        }

        // Check all end points
        for (int i = 0; i < endPoints.Length; i++)
        {
            if (endPoints[i] != null)
            {
                // Ensure lastEndPositions list is long enough
                while (lastEndPositions.Count <= i)
                {
                    lastEndPositions.Add(Vector3.zero);
                }

                if (lastEndPositions[i] != endPoints[i].position)
                {
                    positionChanged = true;
                    lastEndPositions[i] = endPoints[i].position;
                }
            }
        }

        if (animate)
        {
            shapeTimer += Time.deltaTime;
            detailTimer += Time.deltaTime;

            // Regenerate entire lightning shape if timer or position changed
            if (shapeTimer >= shapeUpdateInterval || positionChanged)
            {
                GenerateLightningShape();
                shapeTimer = 0;
                detailTimer = 0; // Reset detail timer too
            }
            // Only update sub-details (flicker effect)
            else if (detailTimer >= detailUpdateInterval)
            {
                UpdateLightningDetails();
                detailTimer = 0;
            }
        }
        else if (positionChanged)
        {
            // Even if not animating, update if position changed
            GenerateLightningShape();
        }
    }

    // ===== ATTACK MODE METHODS =====

    /// <summary>
    /// Fire the lightning attack. Returns true if successful.
    /// </summary>
    public bool FireLightning()
    {
        if (!attackMode)
        {
            Debug.LogWarning("Lightning: Not in attack mode!");
            return false;
        }

        if (!CanFire())
        {
            return false;
        }

        // Auto-find targets if enabled
        if (autoTargetByTag)
        {
            FindAutoTargets();
        }

        // Check if we have valid targets
        if (startPoint == null || endPoints == null || endPoints.Length == 0)
        {
            Debug.LogWarning("Lightning: No valid targets!");
            return false;
        }

        // Activate lightning
        isActive = true;
        activeTimer = attackDuration;
        cooldownTimer = attackCooldown;
        meshRenderer.enabled = true;
        damagedTargets.Clear();

        GenerateLightningShape();

        return true;
    }

    /// <summary>
    /// Fire lightning at specific targets
    /// </summary>
    public bool FireLightningAt(Transform[] targets)
    {
        if (!attackMode || !CanFire()) return false;

        endPoints = targets;
        return FireLightning();
    }

    /// <summary>
    /// Check if lightning can be fired
    /// </summary>
    public bool CanFire()
    {
        return !isActive && cooldownTimer <= 0;
    }

    /// <summary>
    /// Get remaining cooldown time
    /// </summary>
    public float GetCooldownRemaining()
    {
        return Mathf.Max(0, cooldownTimer);
    }

    void DeactivateLightning()
    {
        isActive = false;
        meshRenderer.enabled = false;
        damagedTargets.Clear();
    }

    void UpdateActiveLightning()
    {
        shapeTimer += Time.deltaTime;
        detailTimer += Time.deltaTime;

        // Regenerate entire lightning shape
        if (shapeTimer >= shapeUpdateInterval)
        {
            GenerateLightningShape();
            shapeTimer = 0;
            detailTimer = 0;
        }
        // Only update sub-details (flicker effect)
        else if (detailTimer >= detailUpdateInterval)
        {
            UpdateLightningDetails();
            detailTimer = 0;
        }
    }

    void DealDamageToTargets()
    {
        if (damagePerSecond <= 0 || endPoints == null) return;

        float damageThisFrame = damagePerSecond * Time.deltaTime;

        foreach (Transform target in endPoints)
        {
            if (target == null) continue;

            // Try to get IDamageable component
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damageThisFrame);
            }
        }
    }

    void FindAutoTargets()
    {
        if (startPoint == null) return;

        // Find all objects with the target tag
        GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(targetTag);
        List<Transform> validTargets = new List<Transform>();

        foreach (GameObject target in potentialTargets)
        {
            float distance = Vector3.Distance(startPoint.position, target.transform.position);
            if (distance <= targetRange)
            {
                validTargets.Add(target.transform);
            }
        }

        // Sort by distance and take closest ones
        validTargets.Sort((a, b) =>
            Vector3.Distance(startPoint.position, a.position).CompareTo(
            Vector3.Distance(startPoint.position, b.position))
        );

        // Take max number of targets
        int targetCount = Mathf.Min(validTargets.Count, maxAutoTargets);
        endPoints = new Transform[targetCount];
        for (int i = 0; i < targetCount; i++)
        {
            endPoints[i] = validTargets[i];
        }
    }

    // ===== NORMAL GENERATION METHODS =====

    void GenerateLightningShape()
    {
        // Check if endpoints are assigned
        if (startPoint == null || endPoints == null || endPoints.Length == 0)
        {
            Debug.LogWarning("Lightning: Start point or End points not assigned!");
            return;
        }

        // Update scaled values based on current distance
        UpdateScaledValues();

        // Generate multiple lightning bolts and cache them
        cachedBoltShapes = new List<List<Vector3>>();

        // Loop through each endpoint
        foreach (Transform endPoint in endPoints)
        {
            if (endPoint == null) continue;

            // Calculate per-target scale factor for this specific endpoint
            float targetScale = GetDistanceScaleFactor(startPoint.position, endPoint.position);
            float targetSpread = spreadRadius * targetScale;
            float targetDisplacement = displacement * targetScale;

            // Randomize bolt count for this target
            int boltCount = Random.Range(minBoltsPerTarget, maxBoltsPerTarget + 1);

            // Generate multiple bolts to this endpoint
            for (int i = 0; i < boltCount; i++)
            {
                // Get positions from transforms and randomize for variation
                Vector3 randomStart = startPoint.position + Random.insideUnitSphere * targetSpread;
                Vector3 randomEnd = endPoint.position + Random.insideUnitSphere * targetSpread;

                // Generate main bolt using midpoint displacement with scaled values
                List<Vector3> boltPoints = GenerateLightningMidpoint(randomStart, randomEnd, targetDisplacement);
                cachedBoltShapes.Add(boltPoints);
            }
        }

        // Render the cached shapes
        CreateMeshFromMultipleBolts(cachedBoltShapes);
    }

    void UpdateLightningDetails()
    {
        if (cachedBoltShapes == null || cachedBoltShapes.Count == 0) return;

        // Add flickering detail to the cached shapes
        List<List<Vector3>> flickeredBolts = new List<List<Vector3>>();

        foreach (List<Vector3> originalBolt in cachedBoltShapes)
        {
            List<Vector3> flickeredBolt = new List<Vector3>();

            for (int i = 0; i < originalBolt.Count; i++)
            {
                Vector3 point = originalBolt[i];

                // Add small random jitter for energy flicker (using scaled value)
                if (i > 0 && i < originalBolt.Count - 1) // Don't move start/end points
                {
                    Vector3 randomOffset = Random.insideUnitSphere * scaledDetailDisplacement;
                    point += randomOffset;
                }

                flickeredBolt.Add(point);
            }

            flickeredBolts.Add(flickeredBolt);
        }

        // Render the flickered version
        CreateMeshFromMultipleBolts(flickeredBolts);
    }

    // Midpoint Displacement Algorithm - High-voltage crackling effect
    List<Vector3> GenerateLightningMidpoint(Vector3 start, Vector3 end, float displace)
    {
        List<Vector3> points = new List<Vector3>();
        points.Add(start);
        points.Add(end);

        Vector3 mainDirection = (end - start).normalized;
        float totalLength = Vector3.Distance(start, end);

        // Per-bolt intensity variation for natural look
        float boltIntensity = 1f - Random.Range(0f, intensityVariation);

        for (int gen = 0; gen < generations; gen++)
        {
            List<Vector3> newPoints = new List<Vector3>();
            float currentDisplacement = displace * boltIntensity * Mathf.Pow(roughness, gen);

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 p1 = points[i];
                Vector3 p2 = points[i + 1];

                newPoints.Add(p1);

                Vector3 midpoint = (p1 + p2) * 0.5f;

                // Progress factor - less displacement at endpoints
                float progress = (float)i / Mathf.Max(1, points.Count - 1);
                float progressFactor = Mathf.Sin(progress * Mathf.PI); // Smooth curve, max at center
                progressFactor = Mathf.Lerp(0.3f, 1f, progressFactor); // Never fully zero

                Vector3 direction = (p2 - p1).normalized;
                Vector3 perpendicular = GetPerpendicular(direction);
                Vector3 perpendicular2 = Vector3.Cross(direction, perpendicular);

                // Check for "snap" - sudden larger displacement (crackling effect)
                bool isSnap = Random.value < snapProbability && gen < generations - 2;
                float snapFactor = isSnap ? snapMultiplier : 1f;

                // Offset calculation with snap possibility
                float offsetMagnitude = currentDisplacement * progressFactor * snapFactor;
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector3 offset = (perpendicular * Mathf.Cos(angle) + perpendicular2 * Mathf.Sin(angle)) * offsetMagnitude;

                // Apply sway - pull towards direct path
                midpoint += offset;
                Vector3 directPoint = Vector3.Lerp(start, end, progress + 0.5f / points.Count);
                midpoint = Vector3.Lerp(midpoint, directPoint, sway * 0.25f);

                newPoints.Add(midpoint);
            }

            newPoints.Add(points[points.Count - 1]);
            points = newPoints;
        }

        return points;
    }

    Vector3 GetPerpendicular(Vector3 direction)
    {
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up);
        if (perpendicular.magnitude < 0.1f)
        {
            perpendicular = Vector3.Cross(direction, Vector3.right);
        }
        return perpendicular.normalized;
    }

    void CreateMeshFromMultipleBolts(List<List<Vector3>> allBolts)
    {
        List<Vector3> allVertices = new List<Vector3>();
        List<int> allTriangles = new List<int>();
        List<Vector2> allUvs = new List<Vector2>();

        // Use scaled line width
        float currentLineWidth = scaledLineWidth;

        // Combine all bolts into one mesh
        foreach (List<Vector3> boltPoints in allBolts)
        {
            if (boltPoints.Count < 2) continue;

            int vertexOffset = allVertices.Count;

            // Create quad strip for this bolt
            for (int i = 0; i < boltPoints.Count - 1; i++)
            {
                // Convert world positions to local space
                Vector3 current = transform.InverseTransformPoint(boltPoints[i]);
                Vector3 next = transform.InverseTransformPoint(boltPoints[i + 1]);

                // Calculate width with optional taper
                float segmentWidth = currentLineWidth;
                if (taperThickness)
                {
                    float progress = (float)i / (boltPoints.Count - 1);
                    segmentWidth = currentLineWidth * Mathf.Lerp(1f, 1f - thicknessFalloff, progress);
                }

                // Calculate perpendicular direction for width
                Vector3 direction = (next - current).normalized;
                Vector3 perpendicular = Vector3.Cross(direction, Vector3.forward);
                if (perpendicular.magnitude < 0.1f) perpendicular = Vector3.Cross(direction, Vector3.up);
                perpendicular = perpendicular.normalized * segmentWidth * 0.5f;

                // Create 4 vertices for this segment (quad)
                int baseIndex = allVertices.Count;

                allVertices.Add(current - perpendicular);
                allVertices.Add(current + perpendicular);
                allVertices.Add(next - perpendicular);
                allVertices.Add(next + perpendicular);

                // UVs
                float uvY = (float)i / (boltPoints.Count - 1);
                allUvs.Add(new Vector2(0, uvY));
                allUvs.Add(new Vector2(1, uvY));
                allUvs.Add(new Vector2(0, uvY + 1f / (boltPoints.Count - 1)));
                allUvs.Add(new Vector2(1, uvY + 1f / (boltPoints.Count - 1)));

                // Two triangles to form a quad
                allTriangles.Add(baseIndex + 0);
                allTriangles.Add(baseIndex + 1);
                allTriangles.Add(baseIndex + 2);

                allTriangles.Add(baseIndex + 2);
                allTriangles.Add(baseIndex + 1);
                allTriangles.Add(baseIndex + 3);
            }
        }

        // Update mesh with ALL bolts combined
        lightningMesh.Clear();
        lightningMesh.vertices = allVertices.ToArray();
        lightningMesh.triangles = allTriangles.ToArray();
        lightningMesh.uv = allUvs.ToArray();
        lightningMesh.RecalculateNormals();
        lightningMesh.RecalculateBounds();
    }
}