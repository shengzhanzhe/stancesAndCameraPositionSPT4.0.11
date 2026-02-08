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
        
        // Smooth interpolation for transitions using SmoothDamp
        private static Vector3 _currentRotation = Vector3.zero;
        private static Vector3 _targetRotation = Vector3.zero;
        private static Vector3 _rotationVelocity = Vector3.zero;
        
        private static Vector3 _currentPosition = Vector3.zero;
        private static Vector3 _targetPosition = Vector3.zero;
        private static Vector3 _positionVelocity = Vector3.zero;
        
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
            _currentRotation = Vector3.zero;
            _targetRotation = Vector3.zero;
            _rotationVelocity = Vector3.zero;
            _currentPosition = Vector3.zero;
            _targetPosition = Vector3.zero;
            _positionVelocity = Vector3.zero;
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
            
            // Advanced ADS Transition (Shouldering Effect)
            bool advancedADSEnabled = Plugin._EnableAdvancedADSTransitions?.Value ?? false;
            
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
                float throwDuration = Plugin._ADSShoulderThrowDuration?.Value ?? 0.12f;
                if (Time.time - _shoulderingStartTime >= throwDuration)
                {
                    _isInShoulderingPhase = false;
                }
            }
            
            // Calculate spring physics with appropriate transition speed
            // ADS transitions use ADS speed, stance transitions use stance speed
            // Speed 1 = stiffness 75, Speed 2 = stiffness 150, Speed 3 = stiffness 300
            float transitionSpeed;
            if (_isInShoulderingPhase)
            {
                // Throw phase uses faster speed
                transitionSpeed = Plugin._ADSShoulderThrowSpeed?.Value ?? 12f;
            }
            else if (isAiming && advancedADSEnabled)
            {
                // Settle phase after throw
                transitionSpeed = Plugin._ADSShoulderSettleSpeed?.Value ?? 6f;
            }
            else if (isAiming)
            {
                transitionSpeed = Plugin._ADSTransitionSpeed?.Value ?? 2f;
            }
            else
            {
                transitionSpeed = Plugin._StanceTransitionSpeed?.Value ?? 1f;
            }
            // Convert transition speed to SmoothDamp smoothTime
            // Higher transitionSpeed = faster approach = lower smoothTime
            float smoothTime = SpeedToSmoothTime(transitionSpeed);
            
            // Apply shouldering offset during throw phase
            Vector3 shoulderingOffset = Vector3.zero;
            if (_isInShoulderingPhase)
            {
                float throwForward = Plugin._ADSShoulderThrowForward?.Value ?? 0.08f;
                float throwUp = Plugin._ADSShoulderThrowUp?.Value ?? 0.02f;
                shoulderingOffset = new Vector3(0f, throwUp, throwForward);
            }
            
            // ALWAYS update targets to match desired state (for real-time slider adjustments)
            _targetRotation = desiredRotation;
            _targetPosition = desiredPosition + shoulderingOffset;
            
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

            // Apply the interpolated values based on which spring this is
            if (isRotationSpring)
            {
                __result = _currentRotation + __instance.Current;
            }
            else if (isPositionSpring)
            {
                __result = _currentPosition + __instance.Current;
            }
        }
    }
}
