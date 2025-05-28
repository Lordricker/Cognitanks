# Turret and Projectile System Documentation

## Overview
This system provides comprehensive turret AI, vision, targeting, and projectile physics for tank combat scenarios. The system includes vision cones, target detection, AI behaviors, and a complete projectile system with pooling.

## Core Components

### 1. TurretController
**Location:** `Assets/Models/Prefabs/TankControls/TurretController.cs`

The main turret controller that handles:
- **Vision System**: 3D cone-based target detection using raycasting
- **Target Analysis**: Extracts tank component data (HP, armor, engine stats)
- **AI Integration**: Works with TurretAI for decision making
- **Firing System**: Complete projectile spawning and trajectory calculation
- **Target Tracking**: Maintains detected and visible target lists

**Key Features:**
- Configurable vision cone angle (from TurretData)
- Multiple ray casting for accurate 3D vision
- Target scoring based on distance, angle, and HP
- Lead prediction for moving targets
- Support for different firing patterns (single shot, shotgun spread)
- Debug visualization for vision cones and targets

### 2. ProjectileManager
**Location:** `Assets/Scripts/ProjectileSystem/ProjectileManager.cs`

Singleton manager for projectile spawning and pooling:
- **Object Pooling**: Efficient projectile reuse for performance
- **Multiple Projectile Types**: Rifle, Shotgun, Artillery, Sniper
- **Automatic Configuration**: Different stats per projectile type
- **Collision Management**: Proper layer mask handling

### 3. Projectile
**Location:** `Assets/Scripts/ProjectileSystem/Projectile.cs`

Individual projectile behavior:
- **Physics Movement**: Raycast-based collision detection
- **Damage System**: Tank component targeting
- **Area Damage**: Explosion radius for artillery
- **Knockback Effects**: Different force levels
- **Visual Effects**: Trail renderers and hit effects
- **Pooling Support**: Automatic return to pool on hit/timeout

### 4. TurretAIData & TurretAIMaster
**Location:** `Assets/Workshop/ComponentData/ScriptableObjects/TurretAIData.cs`
**Location:** `Assets/AiEditor/AIFiles/TurretAIMaster.cs`

AI behavior system with 5 distinct behaviors:
- **Aggressive**: Fire at any target in range immediately
- **Conservative**: Wait for closer targets, prefer low HP enemies
- **Defensive**: Only fire when being fired upon
- **Sniper**: Prefer distant targets, wait for accuracy
- **Sweeper**: Constantly rotate and scan, rapid fire

## Setup Instructions

### Arena Setup
1. Add `ArenaCombatSystemSetup` component to any GameObject in your arena scene
2. Configure layer masks for tanks and obstacles
3. The system will auto-configure on scene start

### Tank Setup
1. Add `TurretController` to turret objects in your tank prefabs
2. Assign `turretBarrel` transform (the part that should rotate toward targets)
3. Set `turretData` reference to your turret's ScriptableObject
4. The system will auto-create fire points if needed

### TurretData Configuration
Add the following to your TurretData ScriptableObjects:
```csharp
[Header("Vision System")]
[Range(10f, 90f)]
public float visionCone = 45f; // Vision cone angle in degrees
```

### Recommended Vision Cone Settings
- **Rifle Turrets**: 45° (balanced detection)
- **Shotgun Turrets**: 70° (wide area coverage)
- **Artillery Turrets**: 35° (focused targeting)
- **Sniper Turrets**: 30° (precise targeting)

## Integration with Existing Systems

### TankAssembly Integration
The system integrates with your existing `TankAssembly` system:
```csharp
// TurretController is automatically instantiated on turret prefabs
// TurretData and TurretAI are assigned from player inventory
```

### AI Behavior Configuration
Create different AI assets for varied turret behaviors:
- `TurretAIData.asset` - Default aggressive behavior
- `ConservativeTurretAI.asset` - Conservative targeting
- `DefensiveTurretAI.asset` - Defensive behavior
- `SniperTurretAI.asset` - Long-range precision
- `SweeperTurretAI.asset` - Area denial

## Testing & Debugging

### Test Scripts
- `TurretProjectileTestController`: Manual testing with keyboard controls
- `ArenaProjectileSetup`: Basic arena setup for testing
- `ArenaCombatSystemSetup`: Complete system setup and validation

### Debug Features
- Vision cone visualization (yellow wireframe)
- Target detection lines (red lines to targets)
- Debug logs for firing and target acquisition
- Gizmos for projectile explosion radius

### Test Controls
- **Space**: Manual fire from current turret
- **T**: Cycle through turrets
- **A**: Toggle AI on/off

## Performance Considerations

### Optimizations Implemented
- **Object Pooling**: Projectiles are reused to avoid garbage collection
- **Cached Ray Directions**: Vision cone rays calculated once per frame
- **Layer Mask Filtering**: Efficient collision detection
- **Distance Culling**: Targets beyond range are ignored

### Recommended Settings
- **Vision Ray Count**: 20 rays (balance between accuracy and performance)
- **Pool Size**: 100 projectiles (adjust based on arena size)
- **Vision Update Rate**: Every frame (can be reduced for performance)

## Troubleshooting

### Common Issues
1. **Projectiles not spawning**: Check if ProjectileManager exists and has prefabs assigned
2. **Turrets not detecting targets**: Verify tank layer masks are set correctly
3. **No AI behavior**: Ensure TurretAIData is assigned to turrets
4. **Vision cone not working**: Check that TurretData has visionCone field

### Debug Commands
Use the context menu on `ArenaCombatSystemSetup`:
- "Setup Combat System" - Initialize all systems
- "Test Fire All Turrets" - Fire all turrets once
- "Show System Status" - Display system configuration

## File Structure
```
Assets/
├── Scripts/ProjectileSystem/
│   ├── Projectile.cs
│   ├── ProjectileManager.cs
│   ├── ProjectilePrefabData.cs
│   ├── ArenaProjectileSetup.cs
│   ├── ArenaCombatSystemSetup.cs
│   └── TurretProjectileTestController.cs
├── Models/Prefabs/TankControls/
│   └── TurretController.cs
├── Workshop/ComponentData/ScriptableObjects/
│   └── TurretData.cs (modified)
└── AiEditor/AIFiles/
    ├── TurretAIData.cs
    └── TurretAIMaster.cs
```

## Future Enhancements
- **Ballistic Trajectories**: Arc-based projectile paths for artillery
- **Advanced AI**: Machine learning integration
- **Damage Resistance**: Armor-based damage reduction
- **Special Ammo Types**: Different projectile behaviors
- **Multi-barrel Turrets**: Simultaneous firing from multiple points
