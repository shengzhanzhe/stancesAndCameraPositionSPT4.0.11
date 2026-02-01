using SPT.Reflection.Patching;
using EFT.Animations;
using System.Reflection;

namespace CameraRotationMod.Patches
{
    // This patch runs every frame to continuously apply our spring values
    public class UpdateWeaponVariablesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // UpdateWeaponVariables is called every frame
            return typeof(ProceduralWeaponAnimation).GetMethod("UpdateWeaponVariables", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ProceduralWeaponAnimation __instance)
        {
            // Apply our custom rotation and position every frame
            if (__instance.HandsContainer != null)
            {
                CameraRotationMod.Events.RotationEvents.ApplyRotationAndPosition(__instance);
            }
        }
    }
}
