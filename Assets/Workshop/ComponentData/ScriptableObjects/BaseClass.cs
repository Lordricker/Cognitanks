using UnityEngine;

public enum ComponentCategory
{
    Turret,
    Armor,
    TurretAI,
    NavAI,
    EngineFrame
}

[CreateAssetMenu(fileName = "Base", menuName = "Scriptable Objects/Base")]
public abstract class ComponentData : ScriptableObject
{
    public string title;
    public string description;
    public int cost;
    public int weight;
    public GameObject modelPrefab;
    public ComponentCategory category;
    [Tooltip("Unique ID for this component (must be unique across all components)")]
    public string id { get { return title; } }

    // Unique instance ID for inventory copies
    public string instanceId;
}

