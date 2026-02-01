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
    /// Patch Spring.Get() to add our custom offset to the return value with smooth spring-based transitions
    /// Handles both ADS transitions and Stance toggling with independent spring physics
    /// </summary>
    public class SpringGetPatch : ModulePatch
    {
        // Track transition states
        private static bool _wasAiming = false;
        private static bool _wasInStance = false;
        private static bool _isInitialized = false;
        
        // Spring simulation for smooth transitions
        private static Vector3 _currentRotation = Vector3.zero;
        private static Vector3 _targetRotation = Vector3.zero;
        private static Vector3 _rotationVelocity = Vector3.zero;
        
        private static Vector3 _currentPosition = Vector3.zero;
        private static Vector3 _targetPosition = Vector3.zero;
        private static Vector3 _positionVelocity = Vector3.zero;
        
        // Spring physics parameters (calculated each frame based on transition speed)
        private static float _springStiffness = 150f;
        private static float _springDamping = 10f;
        
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Spring), nameof(Spring.Get));
        }

        private static int _debugFrameCounter = 0;
        
        [PatchPostfix]
        private static void PatchPostfix(Spring __instance, ref Vector3 __result)
        {
            // Check if this is a hands rotation or position spring we want to modify
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld?.MainPlayer?.ProceduralWeaponAnimation?.HandsContainer == null)
                return;

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
            bool stance1RotationEnabled = Plugin._Stance1HandsRotationEnabled?.Value ?? false;
            bool stance1PositionEnabled = Plugin._Stance1HandsPositionEnabled?.Value ?? false;
            bool stance2RotationEnabled = Plugin._Stance2HandsRotationEnabled?.Value ?? false;
            bool stance2PositionEnabled = Plugin._Stance2HandsPositionEnabled?.Value ?? false;
            bool stance3RotationEnabled = Plugin._Stance3HandsRotationEnabled?.Value ?? false;
            bool stance3PositionEnabled = Plugin._Stance3HandsPositionEnabled?.Value ?? false;
            bool defaultPositionEnabled = Plugin._DefaultHandsPositionEnabled?.Value ?? false;
            
            bool isAiming = pwa.IsAiming;
            
            // Check if ANY feature is enabled that could potentially affect this spring
            // We need to run spring physics even when transitioning FROM disabled states
            bool anyRotationFeatureEnabled = stance1RotationEnabled || stance2RotationEnabled || stance3RotationEnabled || resetOnADSEnabled;
            bool anyPositionFeatureEnabled = stance1PositionEnabled || stance2PositionEnabled || stance3PositionEnabled || defaultPositionEnabled || resetOnADSEnabled;
            
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
            
            // Determine which transition speed to use based on current state (not state changes)
            // If we're in ADS, use ADS speed. Otherwise use stance speed.
            float transitionSpeed = (isAiming && resetOnADSEnabled) ? 
                (Plugin._ADSTransitionSpeed?.Value ?? 5f) : 
                (Plugin._StanceTransitionSpeed?.Value ?? 5f);
            
            // ALWAYS update targets to match desired state (for real-time slider adjustments)
            _targetRotation = desiredRotation;
            _targetPosition = desiredPosition;
            
            if (stateChanged)
            {
                // First time initialization - start at target with zero velocity
                if (!_isInitialized)
                {
                    _currentRotation = desiredRotation;
                    _currentPosition = desiredPosition;
                    _rotationVelocity = Vector3.zero;
                    _positionVelocity = Vector3.zero;
                    _isInitialized = true;
                }
                else
                {
                    // State changed - give spring initial velocity to prevent snapping
                    // Calculate displacement from current position to new target
                    Vector3 rotDisplacement = _targetRotation - _currentRotation;
                    Vector3 posDisplacement = _targetPosition - _currentPosition;
                    
                    // Only set velocity if there's actual displacement (avoid zero/zero)
                    if (rotDisplacement.magnitude > 0.001f || posDisplacement.magnitude > 0.001f)
                    {
                        // Initialize velocity proportional to displacement and transition speed
                        float velocityMultiplier = transitionSpeed * 3f;
                        _rotationVelocity = rotDisplacement * velocityMultiplier;
                        _positionVelocity = posDisplacement * velocityMultiplier;
                    }
                    else
                    {
                        // Current already at target - spring has converged, reset to start smoothly from here
                        // Current already at target - spring has converged, reset to start smoothly from here
                        _currentRotation = Vector3.zero;
                        _currentPosition = Vector3.zero;
                        _rotationVelocity = desiredRotation * transitionSpeed * 3f;
                        _positionVelocity = desiredPosition * transitionSpeed * 3f;
                    }
                }
                
                _wasAiming = isAiming;
                _wasInStance = isInStance;
            }
            
            float deltaTime = Time.deltaTime;
            
            // Calculate spring stiffness and damping based on transition speed
            // Higher transition speed = stiffer spring = faster motion
            _springStiffness = transitionSpeed * 30f;
            _springDamping = Mathf.Sqrt(_springStiffness) * 2f; // Critical damping for smooth motion
            
            // Spring physics for rotation
            Vector3 rotationDisplacement = _targetRotation - _currentRotation;
            Vector3 rotationSpringForce = rotationDisplacement * _springStiffness;
            Vector3 rotationDampingForce = _rotationVelocity * _springDamping;
            Vector3 rotationAcceleration = rotationSpringForce - rotationDampingForce;
            
            _rotationVelocity += rotationAcceleration * deltaTime;
            _currentRotation += _rotationVelocity * deltaTime;
            
            // Spring physics for position
            Vector3 positionDisplacement = _targetPosition - _currentPosition;
            Vector3 positionSpringForce = positionDisplacement * _springStiffness;
            Vector3 positionDampingForce = _positionVelocity * _springDamping;
            Vector3 positionAcceleration = positionSpringForce - positionDampingForce;
            
            _positionVelocity += positionAcceleration * deltaTime;
            _currentPosition += _positionVelocity * deltaTime;

            // Apply the spring-simulated values based on which spring this is
            if (isRotationSpring)
            {
                // Only add offset if it's non-zero (avoid interfering with game's ADS detection)
                if (_currentRotation.sqrMagnitude > 0.0001f)
                {
                    __result = _currentRotation + __instance.Current;
                }
                // else: leave __result unchanged - spring returns its natural value
            }
            else if (isPositionSpring)
            {
                // Only add offset if it's non-zero (avoid interfering with game's ADS detection)
                if (_currentPosition.sqrMagnitude > 0.0001f)
                {
                    __result = _currentPosition + __instance.Current;
                }
                // else: leave __result unchanged - spring returns its natural value
            }
        }
    }
}
