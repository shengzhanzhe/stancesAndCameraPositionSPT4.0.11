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
    /// Uses true spring physics with configurable damping for overshoot control
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
        
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Spring), nameof(Spring.Get));
        }
        
        /// <summary>
        /// Custom spring physics with configurable stiffness and damping
        /// Lower damping = more overshoot/oscillation
        /// Higher damping = less overshoot, slower settling
        /// Critical damping = sqrt(4 * stiffness) for no overshoot
        /// </summary>
        private static Vector3 SpringDamp(Vector3 current, Vector3 target, ref Vector3 velocity, 
            float stiffness, float damping, float deltaTime)
        {
            // Spring force: F = -k * (current - target)
            Vector3 displacement = current - target;
            Vector3 springForce = -stiffness * displacement;
            
            // Damping force: F = -c * velocity
            Vector3 dampingForce = -damping * velocity;
            
            // Total acceleration (assuming mass = 1)
            Vector3 acceleration = springForce + dampingForce;
            
            // Semi-implicit Euler integration (more stable than explicit Euler)
            velocity += acceleration * deltaTime;
            current += velocity * deltaTime;
            
            return current;
        }
        
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
            
            // Calculate spring physics from ADS transition speed
            // Speed 1 = stiffness 75, Speed 2 = stiffness 150, Speed 3 = stiffness 300
            float transitionSpeed = Plugin._ADSTransitionSpeed?.Value ?? 2f;
            float stiffness = transitionSpeed * 75f;
            float damping = 50f; // Fixed damping for smooth stop without overshoot
            
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
                // else: State changed but already initialized
                // Let spring physics handle the transition naturally
                // Existing velocity is preserved for momentum
                
                _wasAiming = isAiming;
                _wasInStance = isInStance;
            }
            
            float deltaTime = Time.deltaTime;
            
            // Clamp deltaTime to prevent huge jumps on lag spikes
            deltaTime = Mathf.Min(deltaTime, 0.05f); // Max 50ms step
            
            // Use custom spring physics with configurable damping
            _currentRotation = SpringDamp(_currentRotation, _targetRotation, ref _rotationVelocity, stiffness, damping, deltaTime);
            _currentPosition = SpringDamp(_currentPosition, _targetPosition, ref _positionVelocity, stiffness, damping, deltaTime);

            // Apply the spring-simulated values based on which spring this is
            // Always apply offset - removed threshold check to prevent snap/flicker at end of transition
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
