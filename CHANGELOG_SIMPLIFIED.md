# Changelog

## v1.1.0 (February 2026)

### Bug Fixes
- Fixed visual flicker at the end of ADS transitions, particularly noticeable with large stance rotations (e.g., -30°)
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
- Relocated advanced settings to the Advanced tab (F1 → Advanced):
  - Reset on ADS
  - ADS Transition Speed
  - Per-stance rotation/position enable toggles

### Code Cleanup
- Removed 6 unused patch files
- Eliminated deprecated code and comments
- Resolved all compiler warnings

### File Structure
```
Plugin.cs, StanceManager.cs, PlayerSpringPatch.cs, SpringGetPatch.cs
```
