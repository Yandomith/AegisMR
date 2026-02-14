using UnityEngine;

public class HalfWallScaler : MonoBehaviour
{
    // Call this AFTER the prefab is spawned
    public void MakeHalfWall(GameObject spawnedWall)
    {
        if (spawnedWall == null) return;

        Transform t = spawnedWall.transform;

        // Store original scale
        Vector3 originalScale = t.localScale;
        float originalHeight = originalScale.y;

        // 1️⃣ Scale to half height
        originalScale.y *= 0.5f;
        t.localScale = originalScale;

        // // 2️⃣ Move wall downward so bottom stays aligned
        // // Because scaling shrinks from center
        // float offset = originalHeight * 0.25f;

        // // Use wall's local up direction (important for rotated walls)
        // t.position -= t.up * offset;
    }
}
