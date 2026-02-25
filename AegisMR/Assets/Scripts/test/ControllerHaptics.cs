using UnityEngine;

/// <summary>
/// Simple controller vibration/haptics for Meta Quest controllers
/// </summary>
public class ControllerHaptics : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Which controller to vibrate")]
    public OVRInput.Controller controller = OVRInput.Controller.RTouch;
    
    [Tooltip("Vibration frequency (0-1)")]
    [Range(0f, 1f)]
    public float frequency = 0.5f;
    
    [Tooltip("Vibration amplitude/strength (0-1)")]
    [Range(0f, 1f)]
    public float amplitude = 0.5f;
    
    /// <summary>
    /// Vibrate the controller for a duration
    /// </summary>
    public void Vibrate(float duration)
    {
        StartCoroutine(VibrateCoroutine(duration));
    }
    
    /// <summary>
    /// Vibrate the controller with custom settings
    /// </summary>
    public void Vibrate(float duration, float freq, float amp)
    {
        StartCoroutine(VibrateCoroutine(duration, freq, amp));
    }
    
    /// <summary>
    /// Vibrate the controller for a single frame (call in Update for continuous)
    /// </summary>
    public void VibrateFrame()
    {
        OVRInput.SetControllerVibration(frequency, amplitude, controller);
    }
    
    /// <summary>
    /// Stop vibration immediately
    /// </summary>
    public void StopVibration()
    {
        OVRInput.SetControllerVibration(0, 0, controller);
    }
    
    /// <summary>
    /// Vibrate both controllers
    /// </summary>
    public void VibrateBoth(float duration)
    {
        StartCoroutine(VibrateCoroutine(duration, frequency, amplitude, OVRInput.Controller.LTouch));
        StartCoroutine(VibrateCoroutine(duration, frequency, amplitude, OVRInput.Controller.RTouch));
    }
    
    /// <summary>
    /// Quick pulse vibration
    /// </summary>
    public void Pulse()
    {
        Vibrate(0.1f, 0.8f, 0.8f);
    }
    
    private System.Collections.IEnumerator VibrateCoroutine(float duration)
    {
        yield return VibrateCoroutine(duration, frequency, amplitude, controller);
    }
    
    private System.Collections.IEnumerator VibrateCoroutine(float duration, float freq, float amp, OVRInput.Controller ctrl = OVRInput.Controller.None)
    {
        if (ctrl == OVRInput.Controller.None) ctrl = controller;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            OVRInput.SetControllerVibration(freq, amp, ctrl);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Stop vibration
        OVRInput.SetControllerVibration(0, 0, ctrl);
    }
    
    void OnDisable()
    {
        StopVibration();
    }
}
