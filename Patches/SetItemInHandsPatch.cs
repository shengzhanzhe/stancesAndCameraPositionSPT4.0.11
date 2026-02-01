using SPT.Reflection.Patching;
using System.Reflection;
using HarmonyLib;
using EFT;
using Comfort.Common;
using EFT.InventoryLogic;
using BepInEx.Configuration;
using CameraRotationMod.Events;

namespace CameraRotationMod.Patches
{
    public class SetItemInHandsPatch : ModulePatch
    {
        public static ConfigEntry<bool> _PositionEnabled;
        public static ConfigEntry<bool> _HandsRotationEnabled;
        public static ConfigEntry<bool> _HandsPositionEnabled;
        public static ConfigEntry<float> _HandsPitchRotation;
        public static ConfigEntry<float> _HandsYawRotation;
        public static ConfigEntry<float> _HandsRollRotation;
        public static ConfigEntry<float> _ForwardBackwardOffset;
        public static ConfigEntry<float> _UpDownOffset;
        public static ConfigEntry<float> _SidewaysOffset;
        public static ConfigEntry<float> _HandsForwardBackwardOffset;
        public static ConfigEntry<float> _HandsUpDownOffset;
        public static ConfigEntry<float> _HandsSidewaysOffset;

        public static void Initialize(
            ConfigEntry<bool> PositionEnabled,
            ConfigEntry<bool> HandsRotationEnabled,
            ConfigEntry<bool> HandsPositionEnabled,
            ConfigEntry<float> HandsPitchRotation,
            ConfigEntry<float> HandsYawRotation,
            ConfigEntry<float> HandsRollRotation,
            ConfigEntry<float> ForwardBackwardOffset,
            ConfigEntry<float> UpDownOffset,
            ConfigEntry<float> SidewaysOffset,
            ConfigEntry<float> HandsForwardBackwardOffset,
            ConfigEntry<float> HandsUpDownOffset,
            ConfigEntry<float> HandsSidewaysOffset)
        {

            _PositionEnabled = PositionEnabled;
            _PositionEnabled.SettingChanged += RotationEvents.RotationSettingChanged;

            _HandsRotationEnabled = HandsRotationEnabled;
            _HandsRotationEnabled.SettingChanged += RotationEvents.RotationSettingChanged;

            _HandsPositionEnabled = HandsPositionEnabled;
            _HandsPositionEnabled.SettingChanged += RotationEvents.RotationSettingChanged;

            _HandsPitchRotation = HandsPitchRotation;
            _HandsPitchRotation.SettingChanged += RotationEvents.RotationSettingChanged;

            _HandsYawRotation = HandsYawRotation;
            _HandsYawRotation.SettingChanged += RotationEvents.RotationSettingChanged;

            _HandsRollRotation = HandsRollRotation;
            _HandsRollRotation.SettingChanged += RotationEvents.RotationSettingChanged;

            _ForwardBackwardOffset = ForwardBackwardOffset;
            _ForwardBackwardOffset.SettingChanged += RotationEvents.RotationSettingChanged;

            _UpDownOffset = UpDownOffset;
            _UpDownOffset.SettingChanged += RotationEvents.RotationSettingChanged;

            _SidewaysOffset = SidewaysOffset;
            _SidewaysOffset.SettingChanged += RotationEvents.RotationSettingChanged;

            _HandsForwardBackwardOffset = HandsForwardBackwardOffset;
            _HandsForwardBackwardOffset.SettingChanged += RotationEvents.RotationSettingChanged;

            _HandsUpDownOffset = HandsUpDownOffset;
            _HandsUpDownOffset.SettingChanged += RotationEvents.RotationSettingChanged;

            _HandsSidewaysOffset = HandsSidewaysOffset;
            _HandsSidewaysOffset.SettingChanged += RotationEvents.RotationSettingChanged;
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.method_129));
        }

        [PatchPostfix]
        private static void PatchPostfix(Item item)
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null || gameWorld.RegisteredPlayers == null)
            {
                return;
            }

            if (item != null)
            {
                if (gameWorld != null)
                {
                    if (gameWorld.MainPlayer != null)
                    {
                        if (gameWorld.MainPlayer.ProceduralWeaponAnimation != null)
                        {
                            if (gameWorld.MainPlayer.ProceduralWeaponAnimation.HandsContainer != null)
                            {
                                // Apply both rotation and position when item changes
                                RotationEvents.ApplyRotationAndPosition(gameWorld.MainPlayer.ProceduralWeaponAnimation);
                            }
                        }
                    }
                }
            }
        }
    }
}
