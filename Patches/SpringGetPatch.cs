using SPT.Reflection.Patching;
using EFT.Animations;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using Comfort.Common;
using EFT;

namespace CameraRotationMod.Patches
{
    /// <summary>
    /// Patch Spring.Get() to add our custom offset to the return value with smooth transitions
    /// Handles both ADS transitions and Stance toggling with framerate-independent interpolation
    /// Uses Unity's SmoothDamp for guaranteed stability at any frame rate
    /// </summary>
    public class SpringGetPatch : ModulePatch
    {
        // Track transition states
        private static bool _wasAiming = false;
        private static bool _wasInStance = false;
        private static bool _isInitialized = false;
        
        // Track GameWorld to detect raid changes and reset state
        private static GameWorld _lastGameWorld = null;
        
        // Advanced ADS Transition (Shouldering) state
        private static bool _isInShoulderingPhase = false;
        private static float _shoulderingStartTime = 0f;
        
        // Stance transition shouldering state
        private static bool _isInStanceShoulderingPhase = false;
        private static float _stanceShoulderingStartTime = 0f;
        private static Stance _previousStance = Stance.Default;
        
        // Smooth interpolation for transitions using SmoothDamp
        private static Vector3 _currentRotation = Vector3.zero;
        private static Vector3 _targetRotation = Vector3.zero;
        private static Vector3 _rotationVelocity = Vector3.zero;
        
        private static Vector3 _currentPosition = Vector3.zero;
        private static Vector3 _targetPosition = Vector3.zero;
        private static Vector3 _positionVelocity = Vector3.zero;
        
        // Shouldering rotation offset (separate from the main rotation target)
        private static Vector3 _currentShoulderingRotation = Vector3.zero;
        private static Vector3 _targetShoulderingRotation = Vector3.zero;
        private static Vector3 _shoulderingRotationVelocity = Vector3.zero;
        
        /// <summary>
        /// Reset all state - called when entering new raid or GameWorld changes
        /// </summary>
        public static void ResetState()
        {
            _wasAiming = false;
            _wasInStance = false;
            _isInitialized = false;
            _lastGameWorld = null;
            _isInShoulderingPhase = false;
            _shoulderingStartTime = 0f;
            _isInStanceShoulderingPhase = false;
            _stanceShoulderingStartTime = 0f;
            _previousStance = Stance.Default;
            _currentRotation = Vector3.zero;
            _targetRotation = Vector3.zero;
            _rotationVelocity = Vector3.zero;
            _currentPosition = Vector3.zero;
            _targetPosition = Vector3.zero;
            _positionVelocity = Vector3.zero;
            _currentShoulderingRotation = Vector3.zero;
            _targetShoulderingRotation = Vector3.zero;
            _shoulderingRotationVelocity = Vector3.zero;
        }
        
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Spring), nameof(Spring.Get));
        }
        
        /// <summary>
        /// Convert transition speed (user-facing) to SmoothDamp smoothTime (seconds)
        /// Higher speed = faster = lower smoothTime
        /// </summary>
        private static float SpeedToSmoothTime(float speed)
        {
            // Clamp speed to avoid division by zero and extreme values
            speed = Mathf.Clamp(speed, 0.5f, 20f);
            // Convert: speed 1 → 0.25s, speed 4 → 0.0625s, speed 12 → 0.02s
            return 0.25f / speed;
        }
        
        [PatchPostfix]
        private static void PatchPostfix(Spring __instance, ref Vector3 __result)
        {
            // Check if this is a hands rotation or position spring we want to modify
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld?.MainPlayer?.ProceduralWeaponAnimation?.HandsContainer == null)
                return;
            
            // Detect GameWorld change (new raid) and reset spring state
            if (_lastGameWorld != gameWorld)
            {
                ResetState();
                _lastGameWorld = gameWorld;
            }

            var pwa = gameWorld.MainPlayer.ProceduralWeaponAnimation;
            var handsRotation = pwa.HandsContainer.HandsRotation;
            var handsPosition = pwa.HandsContainer.HandsPosition;

            // Early exit if this spring is not one we care about (hands only, camera handled in PlayerSpringPatch)
            if (__instance != handsRotation && __instance != handsPosition)
                return;

            bool isRotationSpring = __instance == handsRotation;
            bool isPositionSpring = __instance == handsPosition;
            
            // Check if any features are actually enabled
            bool resetOnADSEnabled = Plugin._ResetOnADS?.Value ?? false;
            bool defaultPositionEnabled = Plugin._DefaultHandsPositionEnabled?.Value ?? false;
            
            bool isAiming = pwa.IsAiming;
            
            // Check if ANY feature is enabled that could potentially affect this spring
            // Stances are always enabled when in stance mode, so check if we're in a stance
            bool isInAnyStance = StanceManager.IsInStance;
            bool anyRotationFeatureEnabled = isInAnyStance || resetOnADSEnabled;
            bool anyPositionFeatureEnabled = isInAnyStance || defaultPositionEnabled || resetOnADSEnabled;
            
            // Early exit only if NO features are enabled at all
            if (isRotationSpring && !anyRotationFeatureEnabled)
                return;
            if (isPositionSpring && !anyPositionFeatureEnabled)
                return;

            // Check if player is holding a firearm - if not, force Default stance
            bool isHoldingFirearm = StanceManager.IsHoldingFirearm();
            bool isInStance = isHoldingFirearm && StanceManager.IsInStance;

            // Use StanceManager to get target values based on current state
            // StanceManager handles: Default <-> Stance <-> ADS transitions
            // If not holding firearm, always use Default (zero) values
            Vector3 desiredRotation = isHoldingFirearm ? StanceManager.GetTargetRotation(isAiming) : Vector3.zero;
            Vector3 desiredPosition = isHoldingFirearm ? StanceManager.GetTargetPosition(isAiming) : Vector3.zero;

            // Detect state changes (ADS or Stance toggle) to reinitialize spring
            bool stateChanged = (isAiming != _wasAiming) || (isInStance != _wasInStance);
            bool justStartedAiming = isAiming && !_wasAiming;
            
            // Detect stance change (different from just entering/exiting stance)
            Stance currentStance = StanceManager.CurrentStance;
            bool stanceChanged = currentStance != _previousStance;
            
            // Advanced ADS Transition (Shouldering Effect)
            bool advancedADSEnabled = Plugin._EnableAdvancedADSTransitions?.Value ?? false;
            bool scaleByWeaponStats = Plugin._ScaleByWeaponStats?.Value ?? true;
            
            // Get EFT's calculated AimingSpeed (based on weapon weight + ergonomics)
            // Typically ranges from ~0.4 (heavy/low ergo) to ~2.5+ (light/high ergo)
            float eftAimingSpeed = pwa.AimingSpeed;
            float scaleIntensity = Plugin._WeaponStatsScaleIntensity?.Value ?? 1f;
            
            // Calculate raw multipliers
            float rawAimingSpeedMultiplier = eftAimingSpeed;
            float rawInverseAimingSpeed = 1f / Mathf.Max(eftAimingSpeed, 0.5f);
            
            // Lerp between 1 (no effect) and full effect based on intensity
            // 0 = no scaling, 1 = normal, 2 = exaggerated
            float aimingSpeedMultiplier = scaleByWeaponStats ? Mathf.LerpUnclamped(1f, rawAimingSpeedMultiplier, scaleIntensity) : 1f;
            float inverseAimingSpeed = scaleByWeaponStats ? Mathf.LerpUnclamped(1f, rawInverseAimingSpeed, scaleIntensity) : 1f;
            
            // Get base config values
            float baseThrowDuration = Plugin._ADSShoulderThrowDuration?.Value ?? 0.15f;
            float baseThrowSpeed = Plugin._ADSShoulderThrowSpeed?.Value ?? 2f;
            float baseSettleSpeed = Plugin._ADSShoulderSettleSpeed?.Value ?? 1.5f;
            float baseThrowForward = Plugin._ADSShoulderThrowForward?.Value ?? 0.02f;
            float baseThrowUp = Plugin._ADSShoulderThrowUp?.Value ?? -0.015f;
            
            // Apply weapon stat scaling
            // Duration: heavy weapons = longer duration (inverse of aiming speed)
            float throwDuration = baseThrowDuration * inverseAimingSpeed;
            // Speeds: light/high-ergo = faster (multiply by aiming speed)
            float throwSpeed = baseThrowSpeed * aimingSpeedMultiplier;
            float settleSpeed = baseSettleSpeed * aimingSpeedMultiplier;
            // Throw amounts: heavy = more dramatic throw (inverse of aiming speed)
            float throwForward = baseThrowForward * inverseAimingSpeed;
            float throwUp = baseThrowUp * inverseAimingSpeed;
            
            // Start shouldering phase when entering ADS with advanced transitions enabled
            if (justStartedAiming && advancedADSEnabled && isHoldingFirearm)
            {
                _isInShoulderingPhase = true;
                _shoulderingStartTime = Time.time;
            }
            
            // End shouldering phase when no longer aiming or duration exceeded
            if (!isAiming)
            {
                _isInShoulderingPhase = false;
            }
            else if (_isInShoulderingPhase)
            {
                if (Time.time - _shoulderingStartTime >= throwDuration)
                {
                    _isInShoulderingPhase = false;
                }
            }
            
            // Stance transition shouldering effect
            bool affectStanceTransition = Plugin._AffectStanceTransitionToo?.Value ?? true;
            float stanceTransitionIntensity = Plugin._AdvancedStanceTransitionIntensity?.Value ?? 1f;
            
            // Calculate stance-specific weapon scaling (uses its own intensity slider)
            float stanceAimingSpeedMultiplier = affectStanceTransition ? Mathf.LerpUnclamped(1f, rawAimingSpeedMultiplier, stanceTransitionIntensity) : 1f;
            float stanceInverseAimingSpeed = affectStanceTransition ? Mathf.LerpUnclamped(1f, rawInverseAimingSpeed, stanceTransitionIntensity) : 1f;
            
            // Calculate stance-specific throw values
            float stanceThrowDuration = baseThrowDuration * stanceInverseAimingSpeed;
            float stanceThrowSpeed = baseThrowSpeed * stanceAimingSpeedMultiplier;
            float stanceThrowForward = baseThrowForward * stanceInverseAimingSpeed;
            float stanceThrowUp = baseThrowUp * stanceInverseAimingSpeed;
            
            // Get rotation throw config values (shared between ADS and stance)
            float baseThrowYaw = Plugin._ADSShoulderThrowYaw?.Value ?? 0f;
            float baseThrowPitch = Plugin._ADSShoulderThrowPitch?.Value ?? 0f;
            float baseThrowRoll = Plugin._ADSShoulderThrowRoll?.Value ?? 0f;
            
            // Calculate ADS rotation throw (scaled by weapon stats)
            float throwYaw = baseThrowYaw * inverseAimingSpeed;
            float throwPitch = baseThrowPitch * inverseAimingSpeed;
            float throwRoll = baseThrowRoll * inverseAimingSpeed;
            
            // Calculate stance rotation throw (uses stance intensity slider)
            float stanceThrowYaw = baseThrowYaw * stanceInverseAimingSpeed;
            float stanceThrowPitch = baseThrowPitch * stanceInverseAimingSpeed;
            float stanceThrowRoll = baseThrowRoll * stanceInverseAimingSpeed;
            
            // Start stance shouldering phase when changing stances (and not aiming)
            if (stanceChanged && advancedADSEnabled && affectStanceTransition && isHoldingFirearm && !isAiming)
            {
                _isInStanceShoulderingPhase = true;
                _stanceShoulderingStartTime = Time.time;
                
                // Play aiming rattle sound on stance change
                PlayStanceChangeSound(gameWorld.MainPlayer);
            }
            
            // End stance shouldering phase when duration exceeded or started aiming
            if (isAiming)
            {
                _isInStanceShoulderingPhase = false;
            }
            else if (_isInStanceShoulderingPhase)
            {
                if (Time.time - _stanceShoulderingStartTime >= stanceThrowDuration)
                {
                    _isInStanceShoulderingPhase = false;
                }
            }
            
            // Update previous stance tracker
            _previousStance = currentStance;
            
            // Calculate transition speed for SmoothDamp
            float transitionSpeed;
            if (_isInShoulderingPhase)
            {
                transitionSpeed = throwSpeed;
            }
            else if (_isInStanceShoulderingPhase)
            {
                transitionSpeed = stanceThrowSpeed;
            }
            else if (isAiming && advancedADSEnabled)
            {
                transitionSpeed = settleSpeed;
            }
            else if (isAiming)
            {
                // Non-advanced ADS transition: base speed scaled by weapon stats if enabled
                float baseADSSpeed = Plugin._ADSTransitionSpeed?.Value ?? 2f;
                transitionSpeed = scaleByWeaponStats ? (baseADSSpeed * aimingSpeedMultiplier) : baseADSSpeed;
            }
            else
            {
                // Stance transition: base speed scaled by weapon stats if advanced stance transitions enabled
                float baseStanceSpeed = Plugin._StanceTransitionSpeed?.Value ?? 1f;
                transitionSpeed = (advancedADSEnabled && affectStanceTransition) 
                    ? (baseStanceSpeed * stanceAimingSpeedMultiplier) 
                    : baseStanceSpeed;
            }
            // Convert transition speed to SmoothDamp smoothTime
            // Higher transitionSpeed = faster approach = lower smoothTime
            float smoothTime = SpeedToSmoothTime(transitionSpeed);
            
            // Apply shouldering offset during throw phase (ADS or stance transition)
            Vector3 shoulderingPositionOffset = Vector3.zero;
            Vector3 shoulderingRotationOffset = Vector3.zero;
            
            // Get overall throw intensity multipliers
            float adsThrowIntensity = Plugin._ADSShoulderThrowIntensity?.Value ?? 1f;
            float stanceThrowIntensity = Plugin._StanceShoulderThrowIntensity?.Value ?? 1f;
            
            if (_isInShoulderingPhase)
            {
                shoulderingPositionOffset = new Vector3(0f, throwUp * adsThrowIntensity, throwForward * adsThrowIntensity);
                shoulderingRotationOffset = new Vector3(throwPitch * adsThrowIntensity, throwYaw * adsThrowIntensity, throwRoll * adsThrowIntensity);
            }
            else if (_isInStanceShoulderingPhase)
            {
                // Invert vertical movement for stance transitions (weapon moves closer instead of away)
                shoulderingPositionOffset = new Vector3(0f, stanceThrowUp * stanceThrowIntensity, stanceThrowForward * stanceThrowIntensity);
                shoulderingRotationOffset = new Vector3(stanceThrowPitch * stanceThrowIntensity, stanceThrowYaw * stanceThrowIntensity, stanceThrowRoll * stanceThrowIntensity);
            }
            
            // ALWAYS update targets to match desired state (for real-time slider adjustments)
            _targetRotation = desiredRotation;
            _targetPosition = desiredPosition + shoulderingPositionOffset;
            _targetShoulderingRotation = shoulderingRotationOffset;
            
            if (stateChanged)
            {
                // First time initialization - start at target
                if (!_isInitialized)
                {
                    _currentRotation = desiredRotation;
                    _currentPosition = desiredPosition;
                    _isInitialized = true;
                }
                // else: State changed but already initialized
                // SmoothDamp will smoothly transition, reset velocities for cleaner start
                _rotationVelocity = Vector3.zero;
                _positionVelocity = Vector3.zero;
                
                _wasAiming = isAiming;
                _wasInStance = isInStance;
            }
            
            float deltaTime = Time.deltaTime;
            
            // Use Unity's SmoothDamp - mathematically stable at any frame rate
            _currentRotation = Vector3.SmoothDamp(_currentRotation, _targetRotation, ref _rotationVelocity, smoothTime, Mathf.Infinity, deltaTime);
            _currentPosition = Vector3.SmoothDamp(_currentPosition, _targetPosition, ref _positionVelocity, smoothTime, Mathf.Infinity, deltaTime);
            _currentShoulderingRotation = Vector3.SmoothDamp(_currentShoulderingRotation, _targetShoulderingRotation, ref _shoulderingRotationVelocity, smoothTime, Mathf.Infinity, deltaTime);
            
            // When very close to target, snap to target to prevent micro-jitter
            const float positionSnapThreshold = 0.0001f;  // 0.1mm
            const float rotationSnapThreshold = 0.01f;    // 0.01 degrees
            
            if (Vector3.SqrMagnitude(_currentRotation - _targetRotation) < rotationSnapThreshold * rotationSnapThreshold)
            {
                _currentRotation = _targetRotation;
            }
            
            if (Vector3.SqrMagnitude(_currentPosition - _targetPosition) < positionSnapThreshold * positionSnapThreshold)
            {
                _currentPosition = _targetPosition;
            }
            
            if (Vector3.SqrMagnitude(_currentShoulderingRotation - _targetShoulderingRotation) < rotationSnapThreshold * rotationSnapThreshold)
            {
                _currentShoulderingRotation = _targetShoulderingRotation;
            }

            // Apply the interpolated values based on which spring this is
            if (isRotationSpring)
            {
                // Add both the stance/ADS rotation AND the shouldering rotation offset
                __result = _currentRotation + _currentShoulderingRotation + __instance.Current;
            }
            else if (isPositionSpring)
            {
                __result = _currentPosition + __instance.Current;
            }
        }
        
        /// <summary>
        /// Play the aim rattle sound when changing stances (same sound as when ADS)
        /// </summary>
        private static void PlayStanceChangeSound(Player player)
        {
            if (player?.HandsController is Player.FirearmController fc)
            {
                var soundPlayer = fc.WeaponSoundPlayer;
                if (soundPlayer != null)
                {
                    // Get user-configured volume multiplier (0 = mute, 1 = normal, 2 = louder)
                    float volumeMultiplier = Plugin._StanceChangeSoundVolume?.Value ?? 1f;
                    if (volumeMultiplier <= 0f) return; // Skip if muted
                    
                    // Calculate volume similar to EFT's CalculateAimingSoundVolume
                    // TotalErgonomics / 100 - 1, clamped and scaled
                    float ergo = fc.TotalErgonomics / 100f - 1f;
                    float volume = Mathf.Clamp(ergo * ergo, 0.1f, 0.2f);
                    
                    // Apply covert movement modifier if available
                    if (player.MovementContext != null)
                    {
                        volume *= player.MovementContext.CovertEquipmentNoise;
                    }
                    
                    // Apply user volume multiplier
                    volume *= volumeMultiplier;
                    
                    soundPlayer.PlayAimingSound(volume);
                }
            }
        }
        
        /// <summary>
    }
}
