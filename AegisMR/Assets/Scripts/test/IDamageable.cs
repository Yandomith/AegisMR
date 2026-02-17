using UnityEngine;

/// <summary>
/// Interface for objects that can take damage from lightning attacks
/// </summary>
public interface IDamageable
{
    void TakeDamage(float damage);
}
