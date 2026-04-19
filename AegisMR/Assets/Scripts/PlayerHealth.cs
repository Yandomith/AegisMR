using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tracks the player's HP. Attach to any persistent GameObject in the scene.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Min(1f)]
    public float maxHealth = 100f;

    [Header("Events")]
    [Tooltip("Fires whenever HP changes. Passes (currentHP, maxHP).")]
    public UnityEvent<float, float> OnDamaged;

    [Tooltip("Fires when HP reaches 0.")]
    public UnityEvent OnDeath;

    // ── Public properties ─────────────────────────────────────────────────────
    public float CurrentHealth => _currentHealth;
    public float MaxHealth     => maxHealth;
    public bool  IsDead        => _isDead;

    // ── Internal ──────────────────────────────────────────────────────────────
    private float _currentHealth;
    private bool  _isDead = false;

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        _currentHealth = maxHealth;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Reduce HP by amount. Fires OnDeath if HP reaches 0.</summary>
    public void TakeDamage(float amount)
    {
        if (_isDead) return;

        _currentHealth = Mathf.Max(0f, _currentHealth - amount);
        Debug.Log($"[PlayerHealth] Took {amount} damage → {_currentHealth}/{maxHealth} HP");

        OnDamaged?.Invoke(_currentHealth, maxHealth);

        if (_currentHealth <= 0f)
            Die();
    }

    /// <summary>Restore HP by amount (capped at maxHealth).</summary>
    public void Heal(float amount)
    {
        if (_isDead) return;

        _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
        OnDamaged?.Invoke(_currentHealth, maxHealth);
    }

    /// <summary>Reset HP and death state.</summary>
    public void ResetHealth()
    {
        _isDead        = false;
        _currentHealth = maxHealth;
        OnDamaged?.Invoke(_currentHealth, maxHealth);
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        Debug.Log("[PlayerHealth] Player died!");
        OnDeath?.Invoke();
    }
}
