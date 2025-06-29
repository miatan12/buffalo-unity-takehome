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

        if (IsServer)
        {
            var clone = enemyData.Clone();
            clone.health += Random.Range(0, 5);
            clone.attackPower += Random.Range(0, 5);
            clone.speed += Random.Range(0, 3);

            hp.Value = clone.health;
            attack.Value = clone.attackPower;
            speed.Value = (int)clone.speed;
            enemyName.Value = clone.enemyName;
            syncedColor.Value = clone.color;

            var gm = FindObjectOfType<GameManager>();
            labelColor.Value = gm ? gm.GetLabelColorForTime() : Color.black;

            Debug.Log($"[Enemy][Server] Assigned: {enemyName.Value} | HP: {hp.Value} | ATK: {attack.Value} | SPD: {speed.Value}");
        }

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
    /// Assigns randomized values to this instance.
    /// </summary>
    public void SetupFromServer(EnemyType type)
    {
        hp.Value = type.health + Random.Range(0, 5);
        attack.Value = type.attackPower + Random.Range(0, 5);
        speed.Value = (int)(type.speed + Random.Range(0, 3));
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
