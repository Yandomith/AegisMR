using UnityEngine;

public class HalfWallOnStart : MonoBehaviour
{
    private bool hasScaled = false;

    void Start()
    {
        // Delay one frame so MRUK finishes stretching
        Invoke(nameof(ScaleHalf), 0.05f);
    }

    public UnityEngine.Events.UnityEvent OnScaled;

    void ScaleHalf()
    {
        if (hasScaled) return;
        hasScaled = true;

        Transform t = transform;

        Vector3 originalScale = t.localScale;
        
        // Half height
        originalScale.y *= 0.5f;
        t.localScale = originalScale;

        OnScaled?.Invoke();
    }
    
}
