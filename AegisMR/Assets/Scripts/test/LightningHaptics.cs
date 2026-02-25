using UnityEngine;

/// <summary>
/// Dynamic haptic feedback based on lightning endpoint count
/// More endpoints = stronger vibration
/// </summary>
public class LightningHaptics : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The lightning mesh to monitor")]
    public ProceduralLightningMesh lightningMesh;
    
    [Header("Controller Settings")]
    [Tooltip("Which controller to vibrate")]
    public OVRInput.Controller controller = OVRInput.Controller.RTouch;
    
    [Header("Haptic Intensity")]
    [Tooltip("Amplitude when 1 endpoint active")]
    [Range(0f, 1f)]
    public float lowAmplitude = 0.2f;
    
    [Tooltip("Amplitude when 2 endpoints active")]
    [Range(0f, 1f)]
    public float mediumAmplitude = 0.5f;
    
    [Tooltip("Amplitude when 3+ endpoints active")]
    [Range(0f, 1f)]
    public float highAmplitude = 0.9f;
    
    [Tooltip("Vibration frequency")]
    [Range(0f, 1f)]
    public float frequency = 0.5f;
    
    [Header("Thresholds")]
    [Tooltip("Number of endpoints for medium intensity")]
    public int mediumThreshold = 2;
    [Tooltip("Number of endpoints for high intensity")]
    public int highThreshold = 3;
    
    private bool isVibrating = false;
    
    void Start()
    {
        // Auto-find lightning mesh if not assigned
        if (lightningMesh == null)
        {
            lightningMesh = GetComponent<ProceduralLightningMesh>();
        }
        if (lightningMesh == null)
        {
            lightningMesh = GetComponentInParent<ProceduralLightningMesh>();
        }
        if (lightningMesh == null)
        {
            lightningMesh = FindObjectOfType<ProceduralLightningMesh>();
        }
    }
    
    void Update()
    {
        if (lightningMesh == null) return;
        
        int endpointCount = GetActiveEndpointCount();
        
        if (endpointCount > 0)
        {
            float amplitude = GetAmplitudeForCount(endpointCount);
            OVRInput.SetControllerVibration(frequency, amplitude, controller);
            isVibrating = true;
        }
        else if (isVibrating)
        {
            // Stop vibration when no endpoints
            OVRInput.SetControllerVibration(0, 0, controller);
            isVibrating = false;
        }
    }
    
    int GetActiveEndpointCount()
    {
        if (lightningMesh.endPoints == null) return 0;
        
        int count = 0;
        foreach (Transform endpoint in lightningMesh.endPoints)
        {
            if (endpoint != null) count++;
        }
        return count;
    }
    
    float GetAmplitudeForCount(int count)
    {
        if (count >= highThreshold)
        {
            return highAmplitude;
        }
        else if (count >= mediumThreshold)
        {
            return mediumAmplitude;
        }
        else if (count >= 1)
        {
            return lowAmplitude;
        }
        return 0f;
    }
    
    void OnDisable()
    {
        if (isVibrating)
        {
            OVRInput.SetControllerVibration(0, 0, controller);
            isVibrating = false;
        }
    }
}
