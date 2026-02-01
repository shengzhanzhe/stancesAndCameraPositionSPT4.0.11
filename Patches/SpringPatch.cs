using SPT.Reflection.Patching;
using EFT.Animations;
using System.Reflection;
using UnityEngine;
using HarmonyLib;

namespace CameraRotationMod.Patches
{
    // Patch Spring.FixedUpdate to prevent game from overriding our custom spring values
    public class SpringFixedUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Spring), nameof(Spring.FixedUpdate));
        }

        [PatchPrefix]
        private static bool PatchPrefix(Spring __instance)
        {
            // Check if this is a spring we're controlling
            // We identify our controlled springs by checking if they have very high damping (0.95f)
            // This is a marker we set in RotationEvents when we're controlling the spring
            
            if (__instance.Damping >= 0.94f && __instance.ReturnSpeed >= 4.5f)
            {
                // This is one of our controlled springs - override the FixedUpdate behavior
                // Instead of spring physics, just snap Current towards Zero very quickly
                __instance.Velocity = Vector3.zero; // Kill all velocity
                __instance.Current = Vector3.Lerp(__instance.Current, __instance.Zero, 0.5f); // Fast lerp to target
                __instance.ForceAccumulator = Vector3.zero; // Clear any accumulated forces
                __instance.ForceAccumulatorLimitless = Vector3.zero;
                
                return false; // Skip original method
            }
            
            return true; // Run original method for normal springs
        }
    }
}
