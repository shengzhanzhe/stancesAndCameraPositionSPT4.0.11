# Changelog

## v1.1.0 (February 2026)

### Bug Fixes
- Fixed the annoying flicker/snap at the end of ADS transitions, especially noticeable with large stance rotations like -30°
- Removed some conflicting code that was fighting with the smooth transitions

### Safety Stuff
- Added bounds checking so arms won't stretch to infinity
- Clamped physics timestep to handle frame drops without exploding
- NaN detection - if values go invalid it auto-resets instead of breaking

### Performance
- Cached config values so we're not reading them every single frame
- Cached GameWorld reference (was looking it up 3+ times per frame lol)
- Camera offset only updates when it actually changes now

### Config Cleanup
- Moved some toggles to Advanced tab (F1 → Advanced) to reduce clutter:
  - Reset on ADS, ADS Transition Speed
  - Per-stance rotation/position enable toggles

### Code Cleanup
- Deleted 6 unused patch files that were just sitting there doing nothing
- Removed dead code and old comments
- 0 compiler warnings now

Final structure is just 4 files:
```
Plugin.cs, StanceManager.cs, PlayerSpringPatch.cs, SpringGetPatch.cs
```
