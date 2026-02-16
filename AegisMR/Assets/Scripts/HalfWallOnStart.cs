using UnityEngine;
using System;

public class HalfWallOnStart : MonoBehaviour
{
    private bool hasScaled = false;

    /// <summary>
    /// Static event that fires when ANY wall has been halved.
    /// Subscribe to this to be notified globally without needing a reference to specific walls.
    /// </summary>
    public static event Action<HalfWallOnStart> OnAnyWallScaled;

    /// <summary>
    /// Instance event for this specific wall.
    /// </summary>
    public UnityEngine.Events.UnityEvent OnScaled;

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

        // Half height
        originalScale.y *= 0.5f;
        t.localScale = originalScale;
        Debug.Log("Scaled half wall");

        // Emit instance event
        OnScaled?.Invoke();

        // Emit static event so any listener can respond
        OnAnyWallScaled?.Invoke(this);
    }
}
