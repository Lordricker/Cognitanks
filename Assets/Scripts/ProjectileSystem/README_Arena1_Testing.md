# Arena1 Turret Combat Testing Guide

## Quick Setup Instructions

### Step 1: Add Combat Setup to Arena1
1. Open `Arena1.unity` scene
2. Create an empty GameObject and name it "CombatSetup"
3. Add the `Arena1Setup` component to this GameObject
4. Leave all default settings (auto-setup enabled)

### Step 2: Add Validation (Optional)
1. Create another empty GameObject and name it "CombatValidator" 
2. Add the `Arena1CombatValidator` component to this GameObject
3. This will show real-time status and validation

### Step 3: Test the System
1. Press Play in Unity
2. The system will auto-configure when the scene starts
3. Watch the console for setup messages
4. Use the ArenaManager to spawn tanks (they'll have combat automatically)

## What Gets Set Up Automatically

When you press Play with `Arena1Setup` in the scene:

1. **ProjectileManager** - Handles all projectile spawning and pooling
2. **TurretAIMaster** - Manages AI behavior for all turrets
3. **Layer Configuration** - Sets up proper collision layers
4. **Debug Visuals** - Enables vision cone visualization
5. **Default Projectiles** - Creates rifle, shotgun, artillery, and sniper projectiles

## Testing Tank Combat

### Method 1: Using ArenaManager (Recommended)
1. Make sure some tank slots are active in the ArenaManager
2. Ensure tank slots have all required components (engine, turret, AI)
3. Press Play - tanks will spawn automatically with combat capabilities

### Method 2: Manual Testing
1. Add `TurretProjectileTestController` to any GameObject
2. This provides keyboard controls for manual testing:
   - **Space**: Fire current turret
   - **T**: Cycle through turrets  
   - **A**: Toggle AI on/off

## Expected Behavior

When everything is working correctly:

### Visual Indicators
- **Yellow wireframe cones** showing turret vision ranges
- **Colored projectiles** with trail effects
- **UI status** in corners showing system ready

### Combat Behavior
- Turrets automatically detect enemy tanks in their vision cones
- AI decides when and what to fire at based on TurretAI settings
- Projectiles spawn from fire points and travel realistically
- Different turret types have different vision cones and behaviors

### Debug Information
- Console logs showing setup progress
- Vision cone rays in Scene view (if debug enabled)
- Target detection and firing messages

## Troubleshooting

### Common Issues

**"No ProjectileManager found"**
- Solution: Make sure `Arena1Setup` component is added and auto-setup is enabled

**"Turrets not firing"**
- Check that tanks have `TurretData` and `TurretAI` assigned
- Verify layer masks are configured (tanks should be on layer 6)
- Ensure fire points exist on turrets

**"Projectiles not hitting"**
- Check projectile layer masks include tank and obstacle layers
- Verify colliders exist on tank components

**"No targets detected"**
- Make sure `TurretData` has a valid `visionCone` value (>0 degrees)
- Check that tank layer mask is set correctly
- Verify obstacle layer mask doesn't block all vision rays

### Debug Commands

Use the context menu on `Arena1Setup` GameObject:
- **Setup Arena1 Combat** - Manually trigger setup
- **Test Fire All Turrets** - Fire all turrets once
- **Show System Status** - Display detailed status

## Performance Notes

The system is optimized for arena combat:
- Object pooling for projectiles (50 projectiles by default)
- Efficient raycasting for vision cones
- Layer mask filtering for performance

## Integration with Existing Systems

The combat system integrates seamlessly with:
- **TankAssembly** - Automatically adds TurretController to spawned tanks
- **ArenaManager** - Uses existing spawn points and tank slots
- **WorkshopUI** - Reads turret and AI data from player inventory
- **CameraController** - Works with existing camera system

## Next Steps

Once Arena1 combat is working:
1. Test different turret types and AI behaviors
2. Adjust vision cone angles in TurretData assets
3. Tune projectile damage and speed values
4. Add visual effects for muzzle flash and explosions
5. Test with multiple tanks in combat scenarios

## Files Created

- `Arena1Setup.cs` - Main setup script for Arena1
- `Arena1CombatValidator.cs` - Validation and status display
- This `README_Arena1_Testing.md` file

## Related Documentation

See the main `README.md` in the ProjectileSystem folder for complete system documentation.
