using Unity.Collections;
using UnityEngine;
using Unity.Netcode;
using TMPro;

/// <summary>
/// Networked enemy instance that syncs stats and visuals across clients.
/// </summary>
public class Enemy : NetworkBehaviour
{
    [Header("Source Data")]
    public EnemyType enemyData;

    [Header("UI")]
    public TextMeshProUGUI statsLabel;

    // Synced stats
    private readonly NetworkVariable<int> hp = new();
    private readonly NetworkVariable<int> attack = new();
    private readonly NetworkVariable<int> speed = new();
    private readonly NetworkVariable<FixedString32Bytes> enemyName = new();
    private readonly NetworkVariable<Color> syncedColor = new();
    private readonly NetworkVariable<Color> labelColor = new();

    void Start()
    {
        // Delay label update to ensure network sync occurs
        Invoke(nameof(UpdateLabel), 0.1f);
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("[Enemy] OnNetworkSpawn");

        // **Removed random stat mutation from OnNetworkSpawn — GameManager handles this

        // Sync UI and visuals whenever values change
        hp.OnValueChanged += (_, _) => UpdateLabel();
        attack.OnValueChanged += (_, _) => UpdateLabel();
        speed.OnValueChanged += (_, _) => UpdateLabel();
        enemyName.OnValueChanged += (_, _) => UpdateLabel();
        syncedColor.OnValueChanged += (_, newVal) => ApplyColor(newVal);
        labelColor.OnValueChanged += (_, newVal) => SetLabelColorInternal(newVal);

        // Immediate setup for host
        UpdateLabel();
        ApplyColor(syncedColor.Value);
        SetLabelColorInternal(labelColor.Value);
    }

    /// <summary>
    /// Called by the server after instantiating this enemy.
    /// Assigns final values already adjusted by GameManager.
    /// </summary>
    public void SetupFromServer(EnemyType type)
    {
        // ✅ Use already-mutated values from GameManager (do not add random offsets here)
        hp.Value = type.health;
        attack.Value = type.attackPower;
        speed.Value = (int)type.speed;
        enemyName.Value = type.enemyName;

        if (IsServer) syncedColor.Value = type.color;

        // Host immediately applies visuals
        ApplyColor(type.color);
    }

    private void UpdateLabel()
    {
        if (!statsLabel) return;

        statsLabel.text = $"{enemyName.Value}\nHP: {hp.Value}\nATK: {attack.Value}\nSPD: {speed.Value}";
        Debug.Log($"[Enemy] Updated label: {statsLabel.text}");
    }

    private void ApplyColor(Color color)
    {
        if (TryGetComponent(out Renderer renderer))
        {
            var mat = new Material(renderer.sharedMaterial) { color = color };
            renderer.material = mat;
            Debug.Log($"[Enemy] Applied color: {color}");
        }
    }

    private void SetLabelColorInternal(Color color)
    {
        if (statsLabel)
        {
            statsLabel.color = color;
            Debug.Log($"[Enemy] Label color applied: {color}");
        }
    }

    /// <summary>
    /// Called by server to sync label color across clients.
    /// </summary>
    public void SetLabelColor(Color color)
    {
        if (IsServer) labelColor.Value = color;
        if (IsHost) SetLabelColorInternal(color);
    }
}
