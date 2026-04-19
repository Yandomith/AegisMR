using UnityEngine;
using UnityEngine.SceneManagement;
using Meta.XR.MRUtilityKit;
using System.Collections;

/// <summary>
/// Spawns arrows from outside the player's real room, aimed at the player's head.
/// Waits for MRUK room data to be available before starting.
/// </summary>
public class ArrowSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Arrow prefab with Arrow.cs attached.")]
    public GameObject arrowPrefab;

    [Tooltip("OVRCameraRig in the scene (auto-found if blank).")]
    public OVRCameraRig cameraRig;

    [Tooltip("PlayerHealth in the scene (auto-found if blank).")]
    public PlayerHealth playerHealth;

    [Header("Spawn Settings")]
    [Tooltip("Seconds between arrow spawns.")]
    [Min(0.5f)]
    public float spawnInterval = 3f;

    [Tooltip("How many metres beyond the room radius to spawn arrows.")]
    [Min(0.5f)]
    public float spawnDistance = 3f;

    [Tooltip("Random height variance above/below player head (metres).")]
    public float heightVariance = 0.4f;

    [Tooltip("Number of arrows fired per volley.")]
    [Min(1)]
    public int arrowsPerVolley = 1;

    [Header("Arrow Settings")]
    public float arrowSpeed  = 9f;
    public float arrowDamage = 25f;

    [Header("Game Over")]
    [Tooltip("Seconds to wait after death before reloading the scene.")]
    public float gameOverDelay = 2f;

    // ── Internal ──────────────────────────────────────────────────────────────
    private bool _isSpawning = false;
    private bool _gameOver   = false;

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (cameraRig == null)
            cameraRig = FindFirstObjectByType<OVRCameraRig>();

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        // Subscribe to player death
        if (playerHealth != null)
            playerHealth.OnDeath.AddListener(HandlePlayerDeath);

        StartCoroutine(WaitForRoomThenSpawn());
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnDeath.RemoveListener(HandlePlayerDeath);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Room wait + spawn loop
    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator WaitForRoomThenSpawn()
    {
        Debug.Log("[ArrowSpawner] Waiting for MRUK room...");

        // Poll until MRUK has a valid room
        while (MRUK.Instance == null || MRUK.Instance.GetCurrentRoom() == null)
            yield return new WaitForSeconds(0.5f);

        Debug.Log("[ArrowSpawner] Room ready — starting arrow spawns.");
        _isSpawning = true;
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (_isSpawning && !_gameOver)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (!_gameOver)
            {
                for (int i = 0; i < arrowsPerVolley; i++)
                    SpawnArrow();
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Spawn single arrow
    // ─────────────────────────────────────────────────────────────────────────

    void SpawnArrow()
    {
        if (arrowPrefab == null)
        {
            Debug.LogWarning("[ArrowSpawner] arrowPrefab not assigned!");
            return;
        }

        if (cameraRig == null) return;

        Transform head          = cameraRig.centerEyeAnchor;
        Vector3   playerHeadPos = head.position;

        // ── Determine spawn position ──────────────────────────────────────────
        Vector3 roomCenter  = playerHeadPos; // fallback: use player position
        float   roomRadius  = 1.5f;

        MRUKRoom room = MRUK.Instance?.GetCurrentRoom();
        if (room != null)
        {
            Bounds bounds = room.GetRoomBounds();
            roomCenter    = bounds.center;
            roomRadius    = Mathf.Max(bounds.extents.x, bounds.extents.z);
        }

        // Random horizontal direction
        float   angle     = Random.Range(0f, 360f);
        Vector3 direction = new Vector3(
            Mathf.Sin(angle * Mathf.Deg2Rad),
            0f,
            Mathf.Cos(angle * Mathf.Deg2Rad)
        );

        // Place arrow outside the room at head height ± variance
        Vector3 spawnPos = roomCenter + direction * (roomRadius + spawnDistance);
        spawnPos.y = playerHeadPos.y + Random.Range(-heightVariance, heightVariance);

        // ── Aim at player head ─────────────────────────────────────────────────
        Vector3 travelDir = (playerHeadPos - spawnPos).normalized;

        // ── Instantiate ────────────────────────────────────────────────────────
        GameObject arrowObj = Instantiate(
            arrowPrefab,
            spawnPos,
            Quaternion.LookRotation(travelDir)
        );

        Arrow arrow = arrowObj.GetComponent<Arrow>();
        if (arrow != null)
        {
            arrow.speed  = arrowSpeed;
            arrow.damage = arrowDamage;
            arrow.Initialize(travelDir);
        }

        Debug.Log($"[ArrowSpawner] Arrow spawned from {spawnPos:F1} → player at {playerHeadPos:F1}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Game over
    // ─────────────────────────────────────────────────────────────────────────

    void HandlePlayerDeath()
    {
        if (_gameOver) return;
        _gameOver    = true;
        _isSpawning  = false;
        StopAllCoroutines();

        Debug.Log($"[ArrowSpawner] Player died — reloading scene in {gameOverDelay}s.");
        StartCoroutine(ReloadScene());
    }

    IEnumerator ReloadScene()
    {
        yield return new WaitForSeconds(gameOverDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public controls
    // ─────────────────────────────────────────────────────────────────────────

    public void StopSpawning()  => _isSpawning = false;
    public void StartSpawning() 
    {
        if (!_isSpawning && !_gameOver)
        {
            _isSpawning = true;
            StartCoroutine(SpawnLoop());
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Visualize spawn ring
        Gizmos.color = Color.red;
        int   segments    = 32;
        float totalRadius = 1.5f + spawnDistance;
        for (int i = 0; i < segments; i++)
        {
            float a1 = (float)i       / segments * Mathf.PI * 2f;
            float a2 = (float)(i + 1) / segments * Mathf.PI * 2f;
            Vector3 p1 = transform.position + new Vector3(Mathf.Sin(a1), 0, Mathf.Cos(a1)) * totalRadius;
            Vector3 p2 = transform.position + new Vector3(Mathf.Sin(a2), 0, Mathf.Cos(a2)) * totalRadius;
            Gizmos.DrawLine(p1, p2);
        }
    }
#endif
}
