# Changelog

## v1.0.5 (February 2026)

### New Features

#### Stance Transition Shouldering Effect
- **Affect Stance Transition Too** (toggle, default: true) - When enabled, applies the same shouldering throw effect when switching between stances (not just ADS)
- **Stance Change Sound Volume** (0-2, default: 1) - Volume multiplier for the aim rattle sound played when switching stances. 0 = muted, 1 = normal, 2 = louder

#### Rotation Throw Effects
- **Shoulder Throw Yaw** (-15 to 15, default: 6) - Yaw rotation during throw phase. Positive = rotate right. Applied to both ADS and stance transitions
- **Shoulder Throw Pitch** (-15 to 15, default: -3) - Pitch rotation during throw phase. Positive = rotate up. Applied to both ADS and stance transitions
- **Shoulder Throw Roll** (-15 to 15, default: -1.5) - Roll/tilt rotation during throw phase. Positive = tilt right. Applied to both ADS and stance transitions

#### Overall Throw Intensity Multipliers
- **ADS Shoulder Throw Intensity** (0-2, default: 1) - Overall intensity multiplier for ADS throw effect. Multiplies Forward, Up, Yaw, Pitch, Roll amounts. 0 = no throw, 1 = use config values, 2 = double effect
- **Stance Shoulder Throw Intensity** (0-2, default: 0.75) - Overall intensity multiplier for stance switch throw effect. Same behavior as ADS intensity

#### Weapon Stats Scaling for Stance Transitions
- **Advanced Stance Transition Stat Intensity** (0.01-2, default: 1) - How strongly weapon weight/ergonomics affects stance transition speed and shouldering throw amounts. Uses a separate intensity slider from ADS transitions

### Technical Details
- Rotation throw uses the same smooth interpolation system as position throw (SmoothDamp)
- Rotation and position shouldering offsets are tracked separately for proper blending
- Stance transitions have their own weapon stat scaling that is independent from ADS scaling
- Stance change sound uses WeaponSoundPlayer.PlayAimingSound() with ergonomics-based volume calculation
- All new settings are under the Advanced ADS Transitions section
- Most new settings marked as IsAdvanced (require enabling Advanced Settings in ConfigurationManager)

---

## v1.1.0 (February 2026)

### Bug Fixes
- Fixed visual flicker at the end of ADS transitions, particularly noticeable with large stance rotations (e.g., -30 degrees)
- Resolved conflicting code that interfered with smooth transitions

### Stability Improvements
- Added bounds checking to prevent extreme visual glitches
- Clamped physics timestep to maintain stability during frame drops
- Implemented NaN detection with automatic state reset

### Performance Optimizations
- Cached configuration values to reduce per-frame overhead
- Cached GameWorld reference to eliminate redundant lookups
- Camera offset now updates only when values change

### Configuration Changes
- Relocated advanced settings to the Advanced tab (F1 -> Advanced):
  - Reset on ADS
  - ADS Transition Speed
  - Per-stance rotation/position enable toggles

### Code Cleanup
- Removed 6 unused patch files
- Eliminated deprecated code and comments
- Resolved all compiler warnings
