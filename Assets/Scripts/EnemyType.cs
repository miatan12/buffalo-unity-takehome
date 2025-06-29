using UnityEngine;

/// <summary>
/// Types of enemies available in the game.
/// </summary>
public enum EnemyClass
{
    Grunt,
    Archer,
    Assassin
}

/// <summary>
/// ScriptableObject that defines the base properties of an enemy.
/// Used as a template at runtime for spawning.
/// </summary>
[CreateAssetMenu(fileName = "NewEnemyType", menuName = "Enemy/Create New Enemy Type")]
public class EnemyType : ScriptableObject
{
    [Header("Basic Info")]
    public string enemyName;
    public EnemyClass enemyClass;

    [Header("Stats")]
    public int attackPower;
    public int health;
    public float speed;

    [Header("Spawn Settings")]
    [Range(0f, 1f)] public float spawnRate;

    [Header("Visuals")]
    public Color color;
    public Material material; // Optional visual, not required at runtime

    /// <summary>
    /// Clones this enemy type at runtime.
    /// Used to apply time-based mutations safely.
    /// </summary>
    public EnemyType Clone()
    {
        return Instantiate(this);
    }
}
