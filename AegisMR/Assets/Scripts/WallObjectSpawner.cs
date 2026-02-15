using UnityEngine;

public class WallObjectSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    [Tooltip("The object prefab to spawn on the wall.")]
    public GameObject objectToSpawn;

    [Tooltip("Number of objects to instantiate.")]
    public int spawnCount = 1;

    [Tooltip("The area dimensions (width, height) within which to spawn objects relative to the wall center.")]
    public Vector2 spawnArea = new Vector2(1.0f, 1.0f);

    [Tooltip("Offset distance from the wall surface (local Z axis).")]
    public float surfaceOffset = 0.0f;

    [Tooltip("If true, spawned objects will be parented to this wall.")]
    public bool parentToWall = true;

    /// <summary>
    /// Spawns objects on the wall. Call this from the OnScaled event.
    /// </summary>
    public void SpawnObjects()
    {
        if (objectToSpawn == null)
        {
            Debug.LogWarning($"[{nameof(WallObjectSpawner)}] No objectToSpawn assigned.");
            return;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            SpawnSingleObject();
        }
    }

    private void SpawnSingleObject()
    {
        // Calculate random local position
        float randomX = Random.Range(-spawnArea.x * 0.5f, spawnArea.x * 0.5f);
        float randomY = Random.Range(-spawnArea.y * 0.5f, spawnArea.y * 0.5f);
        
        Vector3 localSpawnPos = new Vector3(randomX, randomY, surfaceOffset);

        // Calculate world position based on the current transform
        Vector3 worldSpawnPos = transform.TransformPoint(localSpawnPos);
        Quaternion spawnRotation = transform.rotation; // Match wall rotation

        GameObject instance = Instantiate(objectToSpawn, worldSpawnPos, spawnRotation);

        if (parentToWall)
        {
            instance.transform.SetParent(transform, true);
        }
    }

    // Optional: Visualize spawn area in Editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(new Vector3(0, 0, surfaceOffset), new Vector3(spawnArea.x, spawnArea.y, 0.1f));
    }
}
