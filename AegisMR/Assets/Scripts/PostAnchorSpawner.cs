using UnityEngine;
using UnityEngine.Events;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;

/// <summary>
/// Spawns objects randomly in the scene after HalfWallOnStart emits that walls have been halved.
/// Attach this component to any GameObject in your scene.
/// </summary>
public class PostAnchorSpawner : MonoBehaviour
{
    public enum SpawnLocation
    {
        /// <summary>Spawn anywhere inside the room bounds.</summary>
        InRoom,
        /// <summary>Spawn on the floor surface.</summary>
        OnFloor,
        /// <summary>Spawn on walls.</summary>
        OnWall,
        /// <summary>Spawn on any horizontal surface (floor, tables, etc.).</summary>
        OnHorizontalSurface,
        /// <summary>Spawn at a fixed position relative to the room.</summary>
        FixedPosition
    }

    [Header("Spawning Settings")]
    [Tooltip("The prefab(s) to spawn. If multiple, one will be chosen randomly.")]
    public List<GameObject> prefabsToSpawn = new List<GameObject>();

    [Tooltip("Number of objects to spawn.")]
    [Min(1)]
    public int spawnCount = 1;

    [Tooltip("Where to spawn the objects.")]
    public SpawnLocation spawnLocation = SpawnLocation.OnFloor;

    [Tooltip("Minimum distance from walls/edges when spawning.")]
    [Min(0f)]
    public float minDistanceToEdge = 0.2f;

    [Tooltip("Height offset above the spawn surface.")]
    public float heightOffset = 0f;

    [Tooltip("Avoid spawning inside volume objects (tables, furniture, etc.).")]
    public bool avoidVolumes = true;

    [Tooltip("Random rotation around Y-axis for spawned objects.")]
    public bool randomYRotation = true;

    [Header("Fixed Position Settings")]
    [Tooltip("Used when SpawnLocation is FixedPosition. Offset from room center.")]
    public Vector3 fixedPositionOffset = Vector3.zero;

    [Header("Wall Listener Settings")]
    [Tooltip("Number of walls to wait for before spawning. Set to 0 to spawn after first wall is scaled.")]
    public int requiredWallCount = 1;

    [Header("Events")]
    [Tooltip("Invoked when all objects have been spawned.")]
    public UnityEvent OnSpawningComplete;

    [Tooltip("Invoked each time an object is spawned. Passes the spawned GameObject.")]
    public UnityEvent<GameObject> OnObjectSpawned;

    private List<GameObject> _spawnedObjects = new List<GameObject>();
    private bool _hasSpawned = false;
    private int _wallsScaledCount = 0;
    private List<HalfWallOnStart> _scaledWalls = new List<HalfWallOnStart>();

    /// <summary>
    /// Returns all objects spawned by this spawner.
    /// </summary>
    public IReadOnlyList<GameObject> SpawnedObjects => _spawnedObjects;

    private void OnEnable()
    {
        // Subscribe to the static event - no need to find wall objects
        HalfWallOnStart.OnAnyWallScaled += OnWallScaled;
        Debug.Log($"[{nameof(PostAnchorSpawner)}] Subscribed to HalfWallOnStart.OnAnyWallScaled event. Waiting for {requiredWallCount} wall(s) to be scaled.");
    }

    private void OnDisable()
    {
        // Unsubscribe from static event
        HalfWallOnStart.OnAnyWallScaled -= OnWallScaled;
    }

    private void OnWallScaled(HalfWallOnStart wall)
    {
        if (_hasSpawned) return;

        // Track this wall
        if (!_scaledWalls.Contains(wall))
        {
            _scaledWalls.Add(wall);
            _wallsScaledCount++;
        }

        Debug.Log($"[{nameof(PostAnchorSpawner)}] Wall scaled ({_wallsScaledCount}/{requiredWallCount}): {wall.name}");

        // Check if we've reached the required count
        if (_wallsScaledCount >= requiredWallCount)
        {
            SpawnAllObjects();
        }
    }

    /// <summary>
    /// Spawns all configured objects. Can be called manually.
    /// </summary>
    public void SpawnAllObjects()
    {
        if (_hasSpawned)
        {
            Debug.LogWarning($"[{nameof(PostAnchorSpawner)}] Already spawned objects.");
            return;
        }

        if (prefabsToSpawn == null || prefabsToSpawn.Count == 0)
        {
            Debug.LogWarning($"[{nameof(PostAnchorSpawner)}] No prefabs assigned to spawn.");
            return;
        }

        _hasSpawned = true;

        for (int i = 0; i < spawnCount; i++)
        {
            SpawnSingleObject();
        }

        OnSpawningComplete?.Invoke();
        Debug.Log($"[{nameof(PostAnchorSpawner)}] Spawned {_spawnedObjects.Count} objects.");
    }

    private void SpawnSingleObject()
    {
        GameObject prefab = GetRandomPrefab();
        if (prefab == null) return;

        Vector3? spawnPosition = GetSpawnPosition();
        if (!spawnPosition.HasValue)
        {
            Debug.LogWarning($"[{nameof(PostAnchorSpawner)}] Could not find valid spawn position.");
            return;
        }

        Quaternion rotation = GetSpawnRotation();

        Debug.Log($"[{nameof(PostAnchorSpawner)}] Spawning {prefab.name} at position {spawnPosition.Value}");

        GameObject instance = Instantiate(prefab, spawnPosition.Value, rotation);
        _spawnedObjects.Add(instance);

        OnObjectSpawned?.Invoke(instance);
    }

    private GameObject GetRandomPrefab()
    {
        if (prefabsToSpawn.Count == 0) return null;

        // Filter out null entries
        List<GameObject> validPrefabs = prefabsToSpawn.FindAll(p => p != null);
        if (validPrefabs.Count == 0) return null;

        return validPrefabs[Random.Range(0, validPrefabs.Count)];
    }

    private Vector3? GetSpawnPosition()
    {
        MRUKRoom room = null;

        // Try to get room from MRUK
        if (MRUK.Instance != null)
        {
            room = MRUK.Instance.GetCurrentRoom();

            // If no current room, try to get any room from the Rooms list
            if (room == null && MRUK.Instance.Rooms != null && MRUK.Instance.Rooms.Count > 0)
            {
                room = MRUK.Instance.Rooms[0];
                Debug.Log($"[{nameof(PostAnchorSpawner)}] Using first room from Rooms list.");
            }
        }

        // If still no room, try to get room from tracked walls' parent anchors
        if (room == null)
        {
            room = GetRoomFromWalls();
        }

        if (room == null)
        {
            Debug.LogWarning($"[{nameof(PostAnchorSpawner)}] No MRUK room found. Using fallback position.");
            return GetFallbackPosition();
        }

        Debug.Log($"[{nameof(PostAnchorSpawner)}] Found room: {room.name}, bounds: {room.GetRoomBounds()}");

        switch (spawnLocation)
        {
            case SpawnLocation.InRoom:
                return GetPositionInRoom(room);

            case SpawnLocation.OnFloor:
                return GetPositionOnSurface(room, MRUK.SurfaceType.FACING_UP,
                    new LabelFilter(MRUKAnchor.SceneLabels.FLOOR));

            case SpawnLocation.OnWall:
                return GetPositionOnSurface(room, MRUK.SurfaceType.VERTICAL,
                    new LabelFilter(MRUKAnchor.SceneLabels.WALL_FACE));

            case SpawnLocation.OnHorizontalSurface:
                return GetPositionOnSurface(room, MRUK.SurfaceType.FACING_UP, new LabelFilter());

            case SpawnLocation.FixedPosition:
                return GetFixedPosition(room);

            default:
                return null;
        }
    }

    private Vector3? GetPositionInRoom(MRUKRoom room)
    {
        if (room == null)
        {
            Debug.LogWarning($"[{nameof(PostAnchorSpawner)}] No room found for positioning.");
            return GetFallbackPosition();
        }

        Vector3? position = room.GenerateRandomPositionInRoom(minDistanceToEdge, avoidVolumes);
        if (position.HasValue)
        {
            return position.Value + Vector3.up * heightOffset;
        }

        return GetFallbackPosition();
    }

    private Vector3? GetPositionOnSurface(MRUKRoom room, MRUK.SurfaceType surfaceType, LabelFilter labelFilter)
    {
        if (room == null)
        {
            Debug.LogWarning($"[{nameof(PostAnchorSpawner)}] No room found for positioning.");
            return GetFallbackPosition();
        }

        if (room.GenerateRandomPositionOnSurface(surfaceType, minDistanceToEdge, labelFilter,
            out Vector3 position, out Vector3 normal))
        {
            return position + normal * heightOffset;
        }

        return GetFallbackPosition();
    }

    private Vector3? GetFixedPosition(MRUKRoom room)
    {
        if (room != null)
        {
            Bounds roomBounds = room.GetRoomBounds();
            return roomBounds.center + fixedPositionOffset;
        }

        // Fallback: use walls center if available
        Vector3 wallCenter = GetWallsCenter();
        return wallCenter + fixedPositionOffset;
    }

    private Vector3 GetFallbackPosition()
    {
        // First try: use walls center
        if (_scaledWalls.Count > 0)
        {
            Vector3 wallCenter = GetWallsCenter();
            Debug.Log($"[{nameof(PostAnchorSpawner)}] Using walls center as fallback: {wallCenter}");
            return wallCenter + Vector3.up * heightOffset;
        }

        // Second try: spawn at camera position with offset
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Vector3 camPos = mainCam.transform.position + mainCam.transform.forward * 1.5f + Vector3.down * 0.5f;
            Debug.Log($"[{nameof(PostAnchorSpawner)}] Using camera position as fallback: {camPos}");
            return camPos;
        }

        // Try OVRCameraRig
        var cameraRig = FindFirstObjectByType<OVRCameraRig>();
        if (cameraRig != null)
        {
            Vector3 rigPos = cameraRig.centerEyeAnchor.position + cameraRig.centerEyeAnchor.forward * 1.5f;
            Debug.Log($"[{nameof(PostAnchorSpawner)}] Using OVRCameraRig as fallback: {rigPos}");
            return rigPos;
        }

        Debug.LogWarning($"[{nameof(PostAnchorSpawner)}] All fallbacks failed, using transform.position");
        return transform.position;
    }

    /// <summary>
    /// Calculates the center point of all scaled walls.
    /// </summary>
    private Vector3 GetWallsCenter()
    {
        if (_scaledWalls.Count == 0)
        {
            return Vector3.zero;
        }

        Vector3 sum = Vector3.zero;
        int validCount = 0;

        foreach (var wall in _scaledWalls)
        {
            if (wall != null)
            {
                sum += wall.transform.position;
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return Vector3.zero;
        }

        return sum / validCount;
    }

    /// <summary>
    /// Tries to get an MRUKRoom from the scaled walls' parent anchors.
    /// </summary>
    private MRUKRoom GetRoomFromWalls()
    {
        foreach (var wall in _scaledWalls)
        {
            if (wall == null) continue;

            // Check the wall itself and its parents for MRUKAnchor
            var anchor = wall.GetComponentInParent<MRUKAnchor>();
            if (anchor != null && anchor.Room != null)
            {
                Debug.Log($"[{nameof(PostAnchorSpawner)}] Found room from wall anchor: {anchor.Room.name}");
                return anchor.Room;
            }
        }

        return null;
    }

    private Quaternion GetSpawnRotation()
    {
        if (randomYRotation)
        {
            return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        return Quaternion.identity;
    }

    /// <summary>
    /// Clears all spawned objects and allows spawning again.
    /// </summary>
    public void ClearSpawnedObjects()
    {
        foreach (var obj in _spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        _spawnedObjects.Clear();
        _hasSpawned = false;
    }

    /// <summary>
    /// Respawns all objects (clears existing and spawns new).
    /// </summary>
    public void Respawn()
    {
        ClearSpawnedObjects();
        SpawnAllObjects();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Visualize spawn area in editor
        Gizmos.color = Color.green;
        
        if (spawnLocation == SpawnLocation.FixedPosition)
        {
            Gizmos.DrawWireSphere(transform.position + fixedPositionOffset, 0.2f);
        }
    }
#endif
}
