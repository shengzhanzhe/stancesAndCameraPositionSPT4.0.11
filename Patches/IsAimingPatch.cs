using SPT.Reflection.Patching;
using EFT.Animations;
using System.Reflection;
using HarmonyLib;

namespace CameraRotationMod.Patches
{
    // This patch monitors ADS state and applies/resets rotations accordingly
    public class IsAimingPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // Patch the IsAiming property setter
            return AccessTools.PropertySetter(typeof(ProceduralWeaponAnimation), nameof(ProceduralWeaponAnimation.IsAiming));
        }

        [PatchPostfix]
        private static void PatchPostfix(ProceduralWeaponAnimation __instance, bool value)
        {
            // When ADS state changes, update rotations
            if (__instance.HandsContainer != null)
            {
                CameraRotationMod.Events.RotationEvents.ApplyRotationAndPosition(__instance);
            }
        }
    }
}
