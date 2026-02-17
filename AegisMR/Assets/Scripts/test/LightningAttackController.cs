using UnityEngine;

/// <summary>
/// Example controller for triggering lightning attacks
/// </summary>
public class LightningAttackController : MonoBehaviour
{
    public ProceduralLightningMesh lightning;
    public KeyCode fireKey = KeyCode.Mouse0; // Left mouse button
    
    void Update()
    {
        if (lightning == null) return;
        
        // Fire lightning on key press
        if (Input.GetKeyDown(fireKey))
        {
            if (lightning.CanFire())
            {
                bool success = lightning.FireLightning();
                if (success)
                {
                    Debug.Log("Lightning fired!");
                }
                else
                {
                    Debug.Log("Failed to fire lightning - no targets?");
                }
            }
            else
            {
                float cooldown = lightning.GetCooldownRemaining();
                Debug.Log($"Lightning on cooldown: {cooldown:F1}s remaining");
            }
        }
    }
    
    // Example: Fire at specific transform
    public void FireAtTarget(Transform target)
    {
        if (lightning != null && lightning.CanFire())
        {
            lightning.FireLightningAt(new Transform[] { target });
        }
    }
    
    // Example: Fire at multiple targets
    public void FireAtTargets(Transform[] targets)
    {
        if (lightning != null && lightning.CanFire())
        {
            lightning.FireLightningAt(targets);
        }
    }
}
