using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Complete system validation and quick-start script for the Turret & Projectile System.
/// This script performs comprehensive checks and provides easy setup for any arena.
/// </summary>
public class TurretProjectileSystemValidator : MonoBehaviour
{
    [Header("Validation Results")]
    [SerializeField] private bool systemValid = false;
    [SerializeField] private List<string> validationErrors = new List<string>();
    [SerializeField] private List<string> validationWarnings = new List<string>();
    
    [Header("Quick Setup Options")]
    public bool autoFixIssues = true;
    public bool createTestScenario = false;
    
    [Header("System Components Status")]
    [SerializeField] private bool projectileManagerReady = false;
    [SerializeField] private bool turretAIMasterReady = false;
    [SerializeField] private int turretControllersFound = 0;
    [SerializeField] private int tankAssembliesFound = 0;
    
    void Start()
    {
        ValidateCompleteSystem();
        
        if (createTestScenario)
        {
            CreateTestScenario();
        }
    }
    
    [ContextMenu("Validate Complete System")]
    public void ValidateCompleteSystem()
    {
        Debug.Log("=== TURRET & PROJECTILE SYSTEM VALIDATION ===");
        
        validationErrors.Clear();
        validationWarnings.Clear();
        
        // Check core managers
        ValidateProjectileManager();
        ValidateTurretAIMaster();
        
        // Check scene components
        ValidateTurretControllers();
        ValidateTankAssemblies();
        
        // Check data assets
        ValidateDataAssets();
        
        // Check layer configuration
        ValidateLayerSetup();
        
        // Final validation
        systemValid = validationErrors.Count == 0;
        
        DisplayValidationResults();
        
        if (autoFixIssues && validationErrors.Count > 0)
        {
            AttemptAutoFix();
        }
    }
    
    void ValidateProjectileManager()
    {
        projectileManagerReady = ProjectileManager.Instance != null;
        
        if (!projectileManagerReady)
        {
            validationErrors.Add("ProjectileManager not found - projectiles cannot be spawned");
        }
        else
        {
            var manager = ProjectileManager.Instance;
            if (manager.rifleBulletPrefab == null) validationWarnings.Add("No rifle bullet prefab assigned");
            if (manager.shotgunPelletPrefab == null) validationWarnings.Add("No shotgun pellet prefab assigned");
            if (manager.artilleryShellPrefab == null) validationWarnings.Add("No artillery shell prefab assigned");
            if (manager.sniperBulletPrefab == null) validationWarnings.Add("No sniper bullet prefab assigned");
        }
    }
    
    void ValidateTurretAIMaster()
    {
        turretAIMasterReady = TurretAIMaster.Instance != null;
        
        if (!turretAIMasterReady)
        {
            validationWarnings.Add("TurretAIMaster not found - AI behaviors may not work optimally");
        }
    }
      void ValidateTurretControllers()
    {
        var turretControllers = FindObjectsByType<TurretController>(FindObjectsSortMode.None);
        turretControllersFound = turretControllers.Length;
        
        if (turretControllersFound == 0)
        {
            validationWarnings.Add("No TurretControllers found in scene");
            return;
        }
        
        foreach (var controller in turretControllers)
        {
            // Check required components
            if (controller.turretData == null)
            {
                validationErrors.Add($"TurretController '{controller.name}' missing TurretData");
            }
            
            if (controller.firePoint == null)
            {
                validationWarnings.Add($"TurretController '{controller.name}' missing fire point");
            }
            
            // Check layer masks
            if (controller.tankLayerMask == 0)
            {
                validationErrors.Add($"TurretController '{controller.name}' has empty tank layer mask");
            }
            
            // Check turret data vision cone
            if (controller.turretData != null && controller.turretData.visionCone <= 0)
            {
                validationErrors.Add($"TurretData '{controller.turretData.name}' has invalid vision cone");
            }
        }
    }
      void ValidateTankAssemblies()
    {
        var tankAssemblies = FindObjectsByType<TankAssembly>(FindObjectsSortMode.None);
        tankAssembliesFound = tankAssemblies.Length;
        
        if (tankAssembliesFound == 0)
        {
            validationWarnings.Add("No TankAssembly components found - integration may not work");
        }
    }
    
    void ValidateDataAssets()
    {
        // Check if we can find turret data assets
        var turretDataAssets = Resources.FindObjectsOfTypeAll<TurretData>();
        if (turretDataAssets.Length == 0)
        {
            validationWarnings.Add("No TurretData assets found");
        }
        else
        {
            // Check vision cone field exists
            foreach (var data in turretDataAssets)
            {
                try
                {
                    float visionCone = data.visionCone;
                    if (visionCone <= 0)
                    {
                        validationWarnings.Add($"TurretData '{data.name}' has invalid vision cone value");
                    }
                }
                catch
                {
                    validationErrors.Add($"TurretData '{data.name}' missing visionCone field");
                }
            }
        }
        
        // Check AI data assets
        var aiDataAssets = Resources.FindObjectsOfTypeAll<TurretAIData>();
        if (aiDataAssets.Length == 0)
        {
            validationWarnings.Add("No TurretAIData assets found");
        }
    }
      void ValidateLayerSetup()
    {
        // This is a basic check - users should configure layers properly
        var turretControllers = FindObjectsByType<TurretController>(FindObjectsSortMode.None);
        foreach (var controller in turretControllers)
        {
            if (controller.tankLayerMask == -1)
            {
                validationWarnings.Add($"TurretController '{controller.name}' using default layer mask (may hit everything)");
            }
        }
    }
    
    void DisplayValidationResults()
    {
        Debug.Log($"=== VALIDATION COMPLETE ===");
        Debug.Log($"System Valid: {systemValid}");
        Debug.Log($"Errors: {validationErrors.Count}, Warnings: {validationWarnings.Count}");
        
        if (validationErrors.Count > 0)
        {
            Debug.LogError("ERRORS FOUND:");
            foreach (var error in validationErrors)
            {
                Debug.LogError($"  - {error}");
            }
        }
        
        if (validationWarnings.Count > 0)
        {
            Debug.LogWarning("WARNINGS:");
            foreach (var warning in validationWarnings)
            {
                Debug.LogWarning($"  - {warning}");
            }
        }
        
        if (systemValid)
        {
            Debug.Log("✅ SYSTEM READY FOR COMBAT!");
        }
        else
        {
            Debug.LogError("❌ SYSTEM HAS ISSUES - CHECK ERRORS ABOVE");
        }
    }
    
    void AttemptAutoFix()
    {
        Debug.Log("Attempting to auto-fix issues...");
        
        // Create ProjectileManager if missing
        if (!projectileManagerReady)
        {
            var combatSetup = FindFirstObjectByType<ArenaCombatSystemSetup>();
            if (combatSetup != null)
            {
                combatSetup.SetupCombatSystem();
                Debug.Log("Created ProjectileManager via ArenaCombatSystemSetup");
            }
            else
            {
                // Create basic ProjectileManager
                GameObject managerObj = new GameObject("ProjectileManager");
                managerObj.AddComponent<ProjectileManager>();
                Debug.Log("Created basic ProjectileManager");
            }
        }
        
        // Create TurretAIMaster if missing
        if (!turretAIMasterReady)
        {
            GameObject masterObj = new GameObject("TurretAIMaster");
            masterObj.AddComponent<TurretAIMaster>();
            DontDestroyOnLoad(masterObj);
            Debug.Log("Created TurretAIMaster");
        }
          // Fix missing fire points
        var turretControllers = FindObjectsByType<TurretController>(FindObjectsSortMode.None);
        foreach (var controller in turretControllers)
        {
            if (controller.firePoint == null)
            {
                GameObject firePoint = new GameObject("FirePoint");
                firePoint.transform.SetParent(controller.transform);
                firePoint.transform.localPosition = Vector3.forward * 0.5f;
                controller.firePoint = firePoint.transform;
                Debug.Log($"Created fire point for {controller.name}");
            }
        }
        
        Debug.Log("Auto-fix attempt complete. Re-run validation to check results.");
    }
    
    void CreateTestScenario()
    {
        Debug.Log("Creating test scenario...");
        
        // Create test target
        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.name = "TestTarget";
        target.transform.position = transform.position + Vector3.forward * 10f + Vector3.up * 2f;
        target.transform.localScale = Vector3.one * 2f;
        target.GetComponent<Renderer>().material.color = Color.red;
        target.AddComponent<SimpleTargetMovement>();
        
        // Add test controller if not present
        if (FindFirstObjectByType<TurretProjectileTestController>() == null)
        {
            var testController = new GameObject("TestController");
            var testComp = testController.AddComponent<TurretProjectileTestController>();
            testComp.testTarget = target.transform;
            testComp.autoTest = false; // Manual control by default
        }
        
        Debug.Log("Test scenario created! Use Space to fire, T to cycle turrets, A to toggle AI");
    }
    
    [ContextMenu("Quick Setup for Arena")]
    public void QuickSetupForArena()
    {
        // Add combat system setup if not present
        if (FindFirstObjectByType<ArenaCombatSystemSetup>() == null)
        {
            var setupObj = new GameObject("ArenaCombatSystemSetup");
            var setup = setupObj.AddComponent<ArenaCombatSystemSetup>();
            setup.SetupCombatSystem();
        }
        
        // Validate after setup
        ValidateCompleteSystem();
    }
    
    void OnGUI()
    {
        if (!systemValid && validationErrors.Count > 0)
        {
            GUILayout.BeginArea(new Rect(10, Screen.height - 150, 400, 140));
            GUI.backgroundColor = Color.red;
            GUILayout.BeginVertical("box");
            GUILayout.Label("⚠️ TURRET SYSTEM ISSUES DETECTED");
            GUILayout.Label($"Errors: {validationErrors.Count}");
            
            if (GUILayout.Button("Auto-Fix Issues"))
            {
                AttemptAutoFix();
                ValidateCompleteSystem();
            }
            
            if (GUILayout.Button("Quick Arena Setup"))
            {
                QuickSetupForArena();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
            GUI.backgroundColor = Color.white;
        }
        else if (systemValid)
        {
            GUILayout.BeginArea(new Rect(10, Screen.height - 60, 200, 50));
            GUI.backgroundColor = Color.green;
            GUILayout.BeginVertical("box");
            GUILayout.Label("✅ Combat System Ready!");
            GUILayout.EndVertical();
            GUILayout.EndArea();
            GUI.backgroundColor = Color.white;
        }
    }
}
