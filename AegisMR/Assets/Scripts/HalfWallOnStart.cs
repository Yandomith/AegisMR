using UnityEngine;

public class HalfWallOnStart : MonoBehaviour
{
    private bool hasScaled = false;

    void Start()
    {
        // Delay one frame so MRUK finishes stretching
        Invoke(nameof(ScaleHalf), 0.05f);
    }

    void ScaleHalf()
    {
        if (hasScaled) return;
        hasScaled = true;

        Transform t = transform;

        Vector3 originalScale = t.localScale;
        float originalHeight = originalScale.y;

        // Half height
        originalScale.y *= 0.5f;
        t.localScale = originalScale;

        // // Move downward so bottom stays aligned
        // float offset = originalHeight * 0.25f;
        // t.position -= t.up * offset;
    }
}
