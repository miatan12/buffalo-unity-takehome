using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

/// <summary>
/// Controls the environment and enemy spawning based on the time of day.
/// </summary>
public class GameManager : NetworkBehaviour
{
    [Header("Enemy Setup")]
    public List<EnemyType> allEnemyTypes;
    public GameObject enemyPrefab;

    [Header("Spawn Points")]
    public Transform spawnPoint_Archers;
    public Transform spawnPoint_Grunts;
    public Transform spawnPoint_RedOnly;
    public Transform spawnPoint_Any;

    [Header("UI")]
    public TextMeshProUGUI timeText;

    [Header("Environment")]
    [SerializeField] private Material groundMaterial;
    [SerializeField] private Light directionalLight;
    [SerializeField] private Material skyboxMaterial;

    private readonly NetworkVariable<TimeOfDay> syncedTimeOfDay = new(
        TimeOfDay.Morning,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Time-of-day visual presets
    private readonly Color morningSky = new(1f, 0.8f, 0.6f);
    private readonly Color afternoonSky = new(0.3f, 0.5f, 0.9f);
    private readonly Color nightSky = new(0.05f, 0.05f, 0.1f);

    private readonly Color morningGround = new(0.85f, 0.75f, 0.65f);
    private readonly Color afternoonGround = Color.white;
    private readonly Color nightGround = new(0.1f, 0.1f, 0.2f);

    private readonly Color morningLight = new(1f, 0.95f, 0.8f);
    private readonly Color afternoonLight = new(1f, 0.9f, 0.6f);
    private readonly Color nightLight = new(0.2f, 0.2f, 0.4f);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            var selected = (TimeOfDay)Random.Range(0, 3);
            syncedTimeOfDay.Value = selected;
            ApplyEnvironmentSettings(selected);
            SpawnEnemies();
        }

        syncedTimeOfDay.OnValueChanged += (_, newVal) => ApplyEnvironmentSettings(newVal);
        ApplyEnvironmentSettings(syncedTimeOfDay.Value);

        if (IsServer && !IsClient)
        {
            // This is a dedicated server — disable all visuals
            DisableVisuals();
        }
    }

    private void DisableVisuals()
    {
        if (directionalLight) directionalLight.enabled = false;
        if (timeText) timeText.enabled = false;
        RenderSettings.skybox = null;
        if (groundMaterial) groundMaterial.color = Color.black;

        // Optional: disable scene cameras, UI, etc.
        foreach (var cam in Camera.allCameras)
            cam.enabled = false;

        foreach (var canvas in FindObjectsOfType<Canvas>())
            canvas.enabled = false;
    }

    private void ApplyEnvironmentSettings(TimeOfDay time)
    {
        Debug.Log($"[GameManager] ApplyEnvironmentSettings: {time}");

        if (timeText)
        {
            timeText.text = $"Time of Day: {time}";
            timeText.color = Color.black;
        }

        if (!groundMaterial || !directionalLight || !skyboxMaterial)
        {
            Debug.LogWarning("[GameManager] Missing environment references.");
            return;
        }

        switch (time)
        {
            case TimeOfDay.Morning:
                SetLighting(morningSky, morningGround, morningLight, 1.3f, 1.2f, 25f, 30f, 1.2f, 0.2f);
                break;
            case TimeOfDay.Afternoon:
                SetLighting(afternoonSky, afternoonGround, afternoonLight, 1.1f, 0.9f, 70f, 120f, 1.5f, 0.4f);
                break;
            case TimeOfDay.Night:
                SetLighting(nightSky, nightGround, nightLight, 0.6f, 0.4f, 340f, 20f, 0.4f, 0.1f);
                break;
        }
    }

    private void SetLighting(Color sky, Color ground, Color light, float exposure, float atmosphere, float lightX, float lightY, float intensity, float gloss)
    {
        RenderSettings.ambientLight = light;
        RenderSettings.skybox = skyboxMaterial;
        RenderSettings.skybox.SetColor("_Tint", sky);
        RenderSettings.skybox.SetFloat("_Exposure", exposure);
        RenderSettings.skybox.SetFloat("_AtmosphereThickness", atmosphere);

        groundMaterial.color = ground;
        groundMaterial.SetFloat("_Glossiness", gloss);

        directionalLight.color = light;
        directionalLight.intensity = intensity;
        directionalLight.transform.rotation = Quaternion.Euler(lightX, lightY, 0f);
    }

    private void SpawnEnemies()
    {
        SpawnFiltered(spawnPoint_Archers, e => e.enemyClass == EnemyClass.Archer);
        SpawnFiltered(spawnPoint_Grunts, e => e.enemyClass == EnemyClass.Grunt);
        SpawnFiltered(spawnPoint_RedOnly, e => e.enemyName == "Red");
        SpawnFiltered(spawnPoint_Any, _ => true);
    }

    private void SpawnFiltered(Transform point, System.Predicate<EnemyType> filter)
    {
        var candidates = allEnemyTypes.FindAll(filter);
        Debug.Log($"[GameManager] Found {candidates.Count} valid enemies at {point.name}");

        var filtered = new List<EnemyType>();
        foreach (var type in candidates)
        {
            var clone = type.Clone();
            AdjustStatsForTime(clone);
            clone.spawnRate = Mathf.Clamp(clone.spawnRate, 0f, 1f);
            filtered.Add(clone);
        }

        if (filtered.Count == 0)
        {
            Debug.LogWarning($"[GameManager] No valid enemies for {point.name}");
            return;
        }

        var chosen = WeightedRandom(filtered);
        Debug.Log($"[GameManager] Spawning: {chosen.enemyName} | ATK: {chosen.attackPower}, HP: {chosen.health}, SPD: {chosen.speed}");

        var enemy = Instantiate(enemyPrefab, point.position, Quaternion.identity);
        enemy.GetComponent<NetworkObject>().Spawn();
        enemy.GetComponent<Enemy>().SetupFromServer(chosen);
        enemy.GetComponent<Renderer>().material.color = chosen.color;
        enemy.GetComponent<Enemy>().SetLabelColor(GetLabelColorForTime());
    }

    private void AdjustStatsForTime(EnemyType enemy)
    {
        switch (syncedTimeOfDay.Value)
        {
            case TimeOfDay.Morning:
                if (enemy.enemyClass == EnemyClass.Archer)
                    enemy.spawnRate += Random.Range(0.2f, 0.4f);
                if (enemy.enemyName.ToLower() == "brown")
                    enemy.spawnRate -= Random.Range(0.1f, 0.3f);
                break;
            case TimeOfDay.Afternoon:
                if (enemy.enemyClass == EnemyClass.Assassin) return;
                if (enemy.enemyClass == EnemyClass.Grunt)
                    enemy.attackPower += 1;
                else
                    enemy.spawnRate += Random.Range(-0.2f, 0.2f);
                break;
            case TimeOfDay.Night:
                if (enemy.enemyClass == EnemyClass.Assassin)
                    enemy.speed += Random.Range(0f, 2f);
                break;
        }
    }

    private EnemyType WeightedRandom(List<EnemyType> list)
    {
        float total = 0f;
        foreach (var e in list) total += e.spawnRate;

        float roll = Random.Range(0, total);
        float sum = 0f;

        foreach (var e in list)
        {
            sum += e.spawnRate;
            if (roll <= sum) return e;
        }

        return list[0];
    }

    public Color GetLabelColorForTime()
    {
        return syncedTimeOfDay.Value == TimeOfDay.Night ? Color.white : Color.black;
    }
}

public enum TimeOfDay
{
    Morning,
    Afternoon,
    Night
}
