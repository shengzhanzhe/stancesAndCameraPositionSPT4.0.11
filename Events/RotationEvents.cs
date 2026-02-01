using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Animations;
using UnityEngine;
using System;
using System.Reflection;

namespace CameraRotationMod.Events
{
    public static class RotationEvents
    {
        private static ManualLogSource Logger;
        
        private static float _savedHipFireFOV = 75f; // Store the original FOV before ADS
        private static bool _wasAiming = false; // Track ADS state changes
        private static FieldInfo _fovField = null; // Reflection field for FOV
        
        public static ConfigEntry<bool> _PositionEnabled;
        public static ConfigEntry<bool> _HandsRotationEnabled;
        public static ConfigEntry<bool> _HandsPositionEnabled;
        public static ConfigEntry<bool> _ResetOnADS;
        public static ConfigEntry<bool> _ADSFovEnabled;
        public static ConfigEntry<float> _ADSFovValue;
        public static ConfigEntry<float> _HandsPitchRotation;
        public static ConfigEntry<float> _HandsYawRotation;
        public static ConfigEntry<float> _HandsRollRotation;
        public static ConfigEntry<float> _ForwardBackwardOffset;
        public static ConfigEntry<float> _UpDownOffset;
        public static ConfigEntry<float> _SidewaysOffset;
        public static ConfigEntry<float> _HandsForwardBackwardOffset;
        public static ConfigEntry<float> _HandsUpDownOffset;
        public static ConfigEntry<float> _HandsSidewaysOffset;
        public static ConfigEntry<float> _ADSHandsPitchRotation;
        public static ConfigEntry<float> _ADSHandsYawRotation;
        public static ConfigEntry<float> _ADSHandsRollRotation;
        public static ConfigEntry<float> _ADSHandsForwardBackwardOffset;
        public static ConfigEntry<float> _ADSHandsUpDownOffset;
        public static ConfigEntry<float> _ADSHandsSidewaysOffset;

        public static void Initialize(
            ConfigEntry<bool> PositionEnabled,
            ConfigEntry<bool> HandsRotationEnabled,
            ConfigEntry<bool> HandsPositionEnabled,
            ConfigEntry<bool> ResetOnADS,
            ConfigEntry<bool> ADSFovEnabled,
            ConfigEntry<float> ADSFovValue,
            ConfigEntry<float> HandsPitchRotation,
            ConfigEntry<float> HandsYawRotation,
            ConfigEntry<float> HandsRollRotation,
            ConfigEntry<float> ForwardBackwardOffset,
            ConfigEntry<float> UpDownOffset,
            ConfigEntry<float> SidewaysOffset,
            ConfigEntry<float> HandsForwardBackwardOffset,
            ConfigEntry<float> HandsUpDownOffset,
            ConfigEntry<float> HandsSidewaysOffset,
            ConfigEntry<float> ADSHandsPitchRotation,
            ConfigEntry<float> ADSHandsYawRotation,
            ConfigEntry<float> ADSHandsRollRotation,
            ConfigEntry<float> ADSHandsForwardBackwardOffset,
            ConfigEntry<float> ADSHandsUpDownOffset,
            ConfigEntry<float> ADSHandsSidewaysOffset)
        {
            _PositionEnabled = PositionEnabled;
            _PositionEnabled.SettingChanged += RotationSettingChanged;

            _HandsRotationEnabled = HandsRotationEnabled;
            _HandsRotationEnabled.SettingChanged += RotationSettingChanged;

            _HandsPositionEnabled = HandsPositionEnabled;
            _HandsPositionEnabled.SettingChanged += RotationSettingChanged;

            _ResetOnADS = ResetOnADS;
            _ResetOnADS.SettingChanged += RotationSettingChanged;

            _ADSFovEnabled = ADSFovEnabled;
            _ADSFovEnabled.SettingChanged += RotationSettingChanged;

            _ADSFovValue = ADSFovValue;
            _ADSFovValue.SettingChanged += RotationSettingChanged;

            _HandsPitchRotation = HandsPitchRotation;
            _HandsPitchRotation.SettingChanged += RotationSettingChanged;

            _HandsYawRotation = HandsYawRotation;
            _HandsYawRotation.SettingChanged += RotationSettingChanged;

            _HandsRollRotation = HandsRollRotation;
            _HandsRollRotation.SettingChanged += RotationSettingChanged;

            _ForwardBackwardOffset = ForwardBackwardOffset;
            _ForwardBackwardOffset.SettingChanged += RotationSettingChanged;

            _UpDownOffset = UpDownOffset;
            _UpDownOffset.SettingChanged += RotationSettingChanged;

            _SidewaysOffset = SidewaysOffset;
            _SidewaysOffset.SettingChanged += RotationSettingChanged;

            _HandsForwardBackwardOffset = HandsForwardBackwardOffset;
            _HandsForwardBackwardOffset.SettingChanged += RotationSettingChanged;

            _HandsUpDownOffset = HandsUpDownOffset;
            _HandsUpDownOffset.SettingChanged += RotationSettingChanged;

            _HandsSidewaysOffset = HandsSidewaysOffset;
            _HandsSidewaysOffset.SettingChanged += RotationSettingChanged;

            _ADSHandsPitchRotation = ADSHandsPitchRotation;
            _ADSHandsPitchRotation.SettingChanged += RotationSettingChanged;

            _ADSHandsYawRotation = ADSHandsYawRotation;
            _ADSHandsYawRotation.SettingChanged += RotationSettingChanged;

            _ADSHandsRollRotation = ADSHandsRollRotation;
            _ADSHandsRollRotation.SettingChanged += RotationSettingChanged;

            _ADSHandsForwardBackwardOffset = ADSHandsForwardBackwardOffset;
            _ADSHandsForwardBackwardOffset.SettingChanged += RotationSettingChanged;

            _ADSHandsUpDownOffset = ADSHandsUpDownOffset;
            _ADSHandsUpDownOffset.SettingChanged += RotationSettingChanged;

            _ADSHandsSidewaysOffset = ADSHandsSidewaysOffset;
            _ADSHandsSidewaysOffset.SettingChanged += RotationSettingChanged;
        }

        public static void RotationSettingChanged(object sender, EventArgs e)
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null || gameWorld.RegisteredPlayers == null)
            {
                return;
            }

            if (gameWorld != null)
            {
                if (gameWorld.MainPlayer != null)
                {
                    if (gameWorld.MainPlayer.ProceduralWeaponAnimation != null)
                    {
                        ApplyRotationAndPosition(gameWorld.MainPlayer.ProceduralWeaponAnimation);
                    }
                }
            }
        }

        public static void ApplyRotationAndPosition(ProceduralWeaponAnimation pwa)
        {
            if (pwa.HandsContainer != null)
            {
                // Check if player is aiming down sights
                bool isAiming = pwa.IsAiming;
                bool shouldReset = _ResetOnADS.Value && isAiming;

                // Handle ADS FOV switching
                if (_ADSFovEnabled.Value)
                {
                    var gameWorld = Singleton<GameWorld>.Instance;
                    if (gameWorld?.MainPlayer != null)
                    {
                        // Get FOV field using reflection if not cached
                        if (_fovField == null)
                        {
                            var settingsType = Singleton<SharedGameSettingsClass>.Instance.Game.Settings.GetType();
                            _fovField = settingsType.GetField("FieldOfView", BindingFlags.Public | BindingFlags.Instance);
                        }

                        if (_fovField != null)
                        {
                            var settings = Singleton<SharedGameSettingsClass>.Instance.Game.Settings;
                            
                            // Detect ADS state change
                            if (isAiming && !_wasAiming)
                            {
                                // Just started aiming - save current FOV
                                _savedHipFireFOV = (int)_fovField.GetValue(settings);
                                // Set ADS FOV
                                _fovField.SetValue(settings, (int)_ADSFovValue.Value);
                            }
                            else if (!isAiming && _wasAiming)
                            {
                                // Just stopped aiming - restore original FOV
                                _fovField.SetValue(settings, (int)_savedHipFireFOV);
                            }
                        }
                    }
                }

                // Update ADS state tracking
                _wasAiming = isAiming;

                // Define target values based on ADS state
                Vector3 targetCameraPosition = new Vector3(0.04f, 0.04f, 0.04f);
                Vector3 targetHandsRotation;
                Vector3 targetHandsPosition;

                // Camera position
                if (_PositionEnabled.Value && !shouldReset)
                {
                    targetCameraPosition = new Vector3(
                        _SidewaysOffset.Value,
                        _UpDownOffset.Value,
                        _ForwardBackwardOffset.Value
                    );
                }

                // Hands use ADS defaults when aiming, custom values when not
                if (shouldReset)
                {
                    // Use ADS default values
                    targetHandsRotation = new Vector3(
                        _ADSHandsPitchRotation.Value,
                        _ADSHandsYawRotation.Value,
                        _ADSHandsRollRotation.Value
                    );
                    targetHandsPosition = new Vector3(
                        _ADSHandsSidewaysOffset.Value,
                        _ADSHandsUpDownOffset.Value,
                        _ADSHandsForwardBackwardOffset.Value
                    );
                }
                else
                {
                    // Use custom hip-fire values (or zero if disabled)
                    targetHandsRotation = _HandsRotationEnabled.Value ? new Vector3(
                        _HandsPitchRotation.Value,
                        _HandsYawRotation.Value,
                        _HandsRollRotation.Value
                    ) : Vector3.zero;

                    targetHandsPosition = _HandsPositionEnabled.Value ? new Vector3(
                        _HandsSidewaysOffset.Value,
                        _HandsUpDownOffset.Value,
                        _HandsForwardBackwardOffset.Value
                    ) : new Vector3(0.04f, 0.04f, 0.04f);
                }

                // Apply values directly to Camera Position (not using spring system)
                if (_PositionEnabled.Value && !shouldReset)
                {
                    pwa.HandsContainer.CameraOffset = targetCameraPosition;
                }
                else if (shouldReset)
                {
                    // Reset camera to default during ADS
                    pwa.HandsContainer.CameraOffset = new Vector3(0.04f, 0.04f, 0.04f);
                }

                // Apply to Hands Rotation spring - ONLY modify Zero (base position), let Current handle physics
                if (_HandsRotationEnabled.Value || shouldReset)
                {
                    // Set Zero as the base position - Current will oscillate around this
                    pwa.HandsContainer.HandsRotation.Zero = targetHandsRotation;
                    
                    // Debug logging
                    if (UnityEngine.Time.frameCount % 120 == 0) // Log every 2 seconds at 60fps
                    {
                        UnityEngine.Debug.LogWarning($"[CameraRotationMod] Setting HandsRotation.Zero to {targetHandsRotation}, Enabled={_HandsRotationEnabled.Value}, shouldReset={shouldReset}");
                    }
                }
                else
                {
                    // Reset to default when disabled
                    pwa.HandsContainer.HandsRotation.Zero = Vector3.zero;
                }

                // Apply to Hands Position spring - ONLY modify Zero (base position), let Current handle physics
                if (_HandsPositionEnabled.Value || shouldReset)
                {
                    // Set Zero as the base position - Current will oscillate around this
                    pwa.HandsContainer.HandsPosition.Zero = targetHandsPosition;
                    
                    // Debug logging
                    if (UnityEngine.Time.frameCount % 120 == 0) // Log every 2 seconds at 60fps
                    {
                        UnityEngine.Debug.LogWarning($"[CameraRotationMod] Setting HandsPosition.Zero to {targetHandsPosition}, Enabled={_HandsPositionEnabled.Value}, shouldReset={shouldReset}");
                    }
                }
                else
                {
                    // Reset to default when disabled
                    pwa.HandsContainer.HandsPosition.Zero = new Vector3(0.04f, 0.04f, 0.04f);
                }
            }
        }
    }
}
