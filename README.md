# Camera Rotation & Position Mod for SPT

Take full control of your first-person view and weapon handling with this highly customizable mod.

## Features

### Camera Position Control
- Adjust your camera forward/backward, up/down, and sideways
- Fine-tune your view to your personal preference
- Toggle camera position on/off independently

### 3 Custom Weapon Stances
Create up to **3 unique weapon ready positions**, each with:
- **Rotation control**: Pitch (up/down tilt), Yaw (left/right turn), Roll (weapon cant)
- **Position control**: Forward/backward, Up/down, Sideways offsets
- Enable/disable each stance individually in the cycle
- Smooth, adjustable transitions between stances

### Flexible Stance Controls
- **Hotkey Toggle**: Press a single key (default: V) to cycle through stances
  - Works even while holding movement keys (W, A, S, D)
- **Mouse Wheel Cycling** (optional): Hold a modifier key (default: Left Alt) and scroll to cycle stances
  - Scroll up = next stance, scroll down = previous stance
- **Use Only Stances mode**: Skip the default position and cycle only through your custom stances

### Tactical Sprint Animations
- Enable compact tac sprint animations per stance (like Modern Warfare style)
- Configurable limits based on:
  - Weapon weight
  - Weapon length (inventory cells)
  - Weapon ergonomics
  - Special allowance for bullpup weapons

### ADS Behavior
- Option to smoothly reset positions when aiming down sights
- Customizable ADS hand positions and rotations
- Adjustable transition speed for smooth or snappy feel

### Fully Configurable
- All settings adjustable in-game via **BepInEx Configuration Manager** (F1)
- No file editing required - tweak values and see changes instantly
- Advanced settings available for fine-tuning

## Installation

1. Download the release or build from source
2. Copy `CameraRotationMod.dll` to `BepInEx/plugins/`
3. Launch the game
4. Press F1 to open Configuration Manager and customize settings

## Configuration Options

### Settings
| Option | Default | Description |
|--------|---------|-------------|
| Enable Camera Position | true | Toggle camera position offsets |
| Reset Positions When Aiming | true | Smoothly transition to defaults when ADS |
| ADS Transition Speed | 0.5 | How quickly hands transition (0.5-20) |
| Stance Toggle Hotkey | V | Key to cycle through stances |
| Enable Mouse Wheel Stance Cycle | false | Use mouse wheel + modifier to cycle |
| Mouse Wheel Modifier Key | Left Alt | Hold while scrolling to cycle stances |
| Use Only Stances | false | Skip default position in cycle |
| Enable Stance 1/2/3 in Cycle | true/true/false | Include each stance in the cycle |
| Stance Transition Speed | 1 | Speed of stance transitions (0.5-20) |

### Per-Stance Settings
Each stance (1, 2, 3) has:
- **Rotation**: Pitch, Yaw, Roll (-45 to +45 degrees)
- **Position**: Forward/Backward, Up/Down, Sideways (-0.5 to +0.5)
- **Sprint Animation**: Enable tac sprint for this stance

### Tac Sprint Settings (Advanced)
- Weight limits (standard and bullpup weapons)
- Length limit (inventory cells)
- Ergonomics minimum requirement

## Building from Source

1. Clone the repository
2. Ensure reference DLLs are in the `References/` folder
3. Run `dotnet build -c Release`
4. Output: `bin/Release/net472/CameraRotationMod.dll`

## Known Issues

- Stance transition speed modifier affects ADS → stance transition too
- Ending tactical sprint causes weapon to warp towards low sprint position
- It's possible to switch stances while running - because of that, if a stance has tactical sprint enabled, the weapon instantly switches to tactical sprint mode

## Compatibility

- **SPT Version**: 4.0.11+
- Compatible with most other mods
- May conflict with other stance/weapon position mods

## Credits

- Original camera offset concept inspired by hazelify's VCO mod
- Author: shwng

## Support

[Buy Me a Coffee](https://buymeacoffee.com/shengzhanzhe)
