using UnityEngine;

/// <summary>
/// Simple validation script to check Arena1 readiness for turret combat testing.
/// Run this in Play mode to see if everything is properly configured.
/// </summary>
public class Arena1CombatValidator : MonoBehaviour
{
    [Header("Validation Settings")]
    public bool runValidationOnStart = true;
    public bool showContinuousStatus = true;

    private bool systemReady = false;
    private string statusMessage = "Checking...";

    void Start()
    {
        if (runValidationOnStart)
        {
            ValidateSystem();
        }
    }

    void Update()
    {
        if (showContinuousStatus)
        {
            ValidateSystem();
        }
    }

    [ContextMenu("Validate Arena1 Combat System")]
    public void ValidateSystem()
    {
        var messages = new System.Collections.Generic.List<string>();
        bool allGood = true;

        // Check ProjectileManager
        if (ProjectileManager.Instance == null)
        {
            messages.Add("❌ ProjectileManager missing");
            allGood = false;
        }
        else
        {
            var manager = ProjectileManager.Instance;
            if (manager.rifleBulletPrefab == null || manager.shotgunPelletPrefab == null ||
                manager.artilleryShellPrefab == null || manager.sniperBulletPrefab == null)
            {
                messages.Add("⚠️ ProjectileManager missing some prefabs");
            }
            else
            {
                messages.Add("✅ ProjectileManager ready");
            }
        }

        // Check TurretAIMaster
        if (TurretAIMaster.Instance == null)
        {
            messages.Add("❌ TurretAIMaster missing");
            allGood = false;
        }
        else
        {
            messages.Add("✅ TurretAIMaster ready");
        }

        // Check TurretControllers
        var turretControllers = FindObjectsByType<TurretController>(FindObjectsSortMode.None);
        if (turretControllers.Length == 0)
        {
            messages.Add("⚠️ No TurretControllers found (tanks not spawned yet?)");
        }
        else
        {
            int configuredCount = 0;
            foreach (var controller in turretControllers)
            {
                if (controller.firePoint != null && controller.turretData != null)
                {
                    configuredCount++;
                }
            }
            messages.Add($"✅ Found {turretControllers.Length} turrets, {configuredCount} configured");
        }

        // Check ArenaManager
        var arenaManager = FindFirstObjectByType<ArenaManager>();
        if (arenaManager == null)
        {
            messages.Add("❌ ArenaManager missing");
            allGood = false;
        }
        else
        {
            int activeTanks = 0;
            foreach (var slot in arenaManager.tankSlots)
            {
                if (slot != null && slot.isActive && slot.engineFramePrefab != null)
                {
                    activeTanks++;
                }
            }
            messages.Add($"✅ ArenaManager ready, {activeTanks} active tanks");
        }

        // Check for setup script
        var setupScript = FindFirstObjectByType<Arena1Setup>();
        if (setupScript == null)
        {
            messages.Add("⚠️ Arena1Setup script not found - add it to setup combat");
        }
        else
        {
            messages.Add("✅ Arena1Setup script found");
        }

        systemReady = allGood;
        statusMessage = string.Join("\n", messages);

        // Log status
        Debug.Log("=== ARENA1 COMBAT VALIDATION ===\n" + statusMessage);
    }

    void OnGUI()
    {
        // Show validation status in bottom-left corner
        GUILayout.BeginArea(new Rect(10, Screen.height - 200, 400, 190));
        
        GUI.backgroundColor = systemReady ? Color.green : Color.yellow;
        GUILayout.BeginVertical("box");
        
        GUILayout.Label(systemReady ? "✅ Arena1 Combat System Status" : "⚠️ Arena1 Combat System Status");
        GUILayout.Space(5);
        
        GUILayout.Label(statusMessage);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Re-validate System"))
        {
            ValidateSystem();
        }
        
        if (!systemReady && GUILayout.Button("Auto-Setup Combat"))
        {
            var setupScript = FindFirstObjectByType<Arena1Setup>();
            if (setupScript != null)
            {
                setupScript.SetupArena1Combat();
            }
            else
            {
                Debug.LogWarning("Arena1Setup script not found! Add it to a GameObject in the scene.");
            }
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
        GUI.backgroundColor = Color.white;
    }
}
