# Stance System Implementation

## Overview
Added a tactical stance toggling system that allows you to switch between **Default**, **Stance**, and **ADS** positions/rotations with smooth spring-physics transitions.

## Features

### Three Position States:
1. **Default** - Your normal hip-fire position
   - Rotation is always 0,0,0 (hardcoded)
   - Position can be customized via sliders

2. **Stance** - Tactical stance (custom position/rotation)
   - Both position AND rotation fully customizable
   - Toggle with hotkey (default: T)
   
3. **ADS** - Aiming down sights
   - Configurable position/rotation (in "ADS Default Values (Advanced)")
   - Automatically activates when aiming
   - Can be disabled via "Reset Positions When Aiming" setting

### Transition System:
- **Spring Physics**: All transitions use realistic spring simulation
- **Configurable Speed**: Separate speed controls for:
  - ADS Transition Speed (0.5-20)
  - Stance Transition Speed (0.5-20)
- **Natural Feel**: Weapon has weight/momentum with slight overshoot and smooth settling

## Configuration Structure

### Settings Tab:
- **Enable Camera Position**: Camera offset toggles
- **Reset Positions When Aiming**: Enable ADS position override
- **ADS Transition Speed**: How fast to transition when entering/exiting ADS
- **Stance Toggle Hotkey**: Key to toggle between Default ↔ Stance (default: T)
- **Stance Transition Speed**: How fast to transition between Default and Stance
- **Enable Default Hands/Arms Position**: Enable custom default position
- **Enable Stance Hands/Arms Rotation**: Enable rotation in stance mode
- **Enable Stance Hands/Arms Position**: Enable position in stance mode

### Default Hands/Arms Positions:
- **Default Hands Forward/Backward Offset** (Z-axis)
- **Default Hands Up/Down Offset** (Y-axis)
- **Default Hands Sideways Offset** (X-axis)
- Note: Default rotation is always 0,0,0

### Stance Hands/Arms Rotations:
- **Stance Hands Pitch** (X-axis, up/down tilt)
- **Stance Hands Yaw** (Y-axis, left/right turn)
- **Stance Hands Roll** (Z-axis, weapon cant)

### Stance Hands/Arms Positions:
- **Stance Hands Forward/Backward Offset** (Z-axis)
- **Stance Hands Up/Down Offset** (Y-axis)
- **Stance Hands Sideways Offset** (X-axis)

### ADS Default Values (Advanced):
- **ADS Hands Pitch/Yaw/Roll Rotation**
- **ADS Hands Forward/Backward/Up/Down/Sideways Offset**

## State Transition Logic

```
┌─────────┐    Stance Toggle (T)    ┌─────────┐
│ DEFAULT │◄─────────────────────────►│ STANCE  │
│ Pos: X  │                          │ Pos: Y  │
│ Rot: 0  │                          │ Rot: Z  │
└─────────┘                          └─────────┘
     │                                     │
     │ ADS (Right Click)                  │ ADS (Right Click)
     ▼                                     ▼
┌─────────────────────────────────────────────┐
│                   ADS                       │
│  (Overrides Default/Stance if enabled)     │
│         Position: Configurable              │
│         Rotation: Configurable              │
└─────────────────────────────────────────────┘
```

## Technical Implementation

### Files Modified:
1. **Plugin.cs**: Added new config entries for Default/Stance positions
2. **StanceManager.cs**: NEW - Handles stance toggling and target value calculation
3. **SpringGetPatch.cs**: Updated to use StanceManager for all value calculations

### Key Classes:
- **StanceManager**: Static class managing stance state and providing target values
  - `IsInStance`: Current stance state (true/false)
  - `Update()`: Checks for hotkey press each frame
  - `GetTargetRotation(isAiming)`: Returns target rotation based on current state
  - `GetTargetPosition(isAiming)`: Returns target position based on current state

### Spring Physics:
- Force = Stiffness × Displacement - Damping × Velocity
- Stiffness = TransitionSpeed × 30
- Damping = √Stiffness × 2 (critical damping)
- Separate spring velocities for rotation and position

## Usage Guide

### Basic Setup:
1. **Set Default Position**: Configure "Default Hands/Arms Positions" sliders
2. **Set Stance**: Configure "Stance Hands/Arms Positions" and "Rotations" sliders
3. **Enable Features**: Toggle "Enable Stance Hands/Arms Rotation" and "Position"
4. **Test**: Press T to toggle between Default and Stance

### Advanced:
- Adjust transition speeds for different feels:
  - Low (1-3): Slow, heavy weapon feel
  - Medium (4-7): Balanced, realistic
  - High (8-20): Fast, arcade-like
  
- Configure ADS position separately for precise aiming

### Recommended Settings:
- **Default Position**: 0, 0, 0 (vanilla game feel)
- **Stance Position**: -0.05, -0.02, 0.1 (tactical close-quarters)
- **Stance Rotation**: 0, -5, 0 (slight weapon cant)
- **Transition Speed**: 5 (balanced)

## Hotkey Customization
You can change the stance toggle key in BepInEx Configuration Manager:
- Default: T
- Can be any keyboard key or key combination
- Changes are saved to config file

## Notes
- Stance state persists until toggled again (doesn't reset on weapon swap)
- ADS always overrides stance when "Reset On ADS" is enabled
- Camera position offsets work independently from hands/arms
- All transitions use the same spring physics for consistency
