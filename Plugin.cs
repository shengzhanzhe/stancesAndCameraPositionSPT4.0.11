using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CameraRotationMod.Patches;
using UnityEngine;

namespace CameraRotationMod;

[BepInPlugin("shwng.camerarotation", "Camera Rotation & Position Mod", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    public static new ManualLogSource Logger;

    private const string Settings = "Settings";
    public static ConfigEntry<bool> _PositionEnabled;
    public static ConfigEntry<bool> _ResetOnADS;
    public static ConfigEntry<float> _ADSTransitionSpeed;
    public static ConfigEntry<KeyCode> _StanceToggleKey;
    public static ConfigEntry<bool> _EnableMouseWheelCycle;
    public static ConfigEntry<KeyCode> _MouseWheelModifierKey;
    public static ConfigEntry<float> _StanceTransitionSpeed;
    public static ConfigEntry<bool> _UseOnlyStances;
    public static ConfigEntry<bool> _EnableStance1;
    public static ConfigEntry<bool> _EnableStance2;
    public static ConfigEntry<bool> _EnableStance3;

    // Default position (always 0,0,0 rotation)
    private const string DefaultHandsPositions = "Default Hands/Arms Positions";
    public static ConfigEntry<bool> _DefaultHandsPositionEnabled;
    public static ConfigEntry<float> _DefaultHandsForwardBackwardOffset;
    public static ConfigEntry<float> _DefaultHandsUpDownOffset;
    public static ConfigEntry<float> _DefaultHandsSidewaysOffset;

    // Stance 1 position and rotation
    private const string Stance1HandsRotations = "Stance 1 Hands/Arms Rotations";
    public static ConfigEntry<float> _Stance1HandsPitchRotation; // X-axis
    public static ConfigEntry<float> _Stance1HandsYawRotation;   // Y-axis
    public static ConfigEntry<float> _Stance1HandsRollRotation;  // Z-axis

    private const string Stance1HandsPositions = "Stance 1 Hands/Arms Positions";
    public static ConfigEntry<float> _Stance1HandsForwardBackwardOffset; // Z-axis
    public static ConfigEntry<float> _Stance1HandsUpDownOffset;          // Y-axis
    public static ConfigEntry<float> _Stance1HandsSidewaysOffset;        // X-axis

    // Stance 1 Sprint Animation
    private const string Stance1Animation = "Stance 1 Animation";
    public static ConfigEntry<bool> _Stance1SprintAnimationEnabled;

    // Stance 2 position and rotation
    private const string Stance2HandsRotations = "Stance 2 Hands/Arms Rotations";
    public static ConfigEntry<float> _Stance2HandsPitchRotation; // X-axis
    public static ConfigEntry<float> _Stance2HandsYawRotation;   // Y-axis
    public static ConfigEntry<float> _Stance2HandsRollRotation;  // Z-axis

    private const string Stance2HandsPositions = "Stance 2 Hands/Arms Positions";
    public static ConfigEntry<float> _Stance2HandsForwardBackwardOffset; // Z-axis
    public static ConfigEntry<float> _Stance2HandsUpDownOffset;          // Y-axis
    public static ConfigEntry<float> _Stance2HandsSidewaysOffset;        // X-axis

    // Stance 2 Sprint Animation
    private const string Stance2Animation = "Stance 2 Animation";
    public static ConfigEntry<bool> _Stance2SprintAnimationEnabled;

    // Stance 3 position and rotation
    private const string Stance3HandsRotations = "Stance 3 Hands/Arms Rotations";
    public static ConfigEntry<float> _Stance3HandsPitchRotation; // X-axis
    public static ConfigEntry<float> _Stance3HandsYawRotation;   // Y-axis
    public static ConfigEntry<float> _Stance3HandsRollRotation;  // Z-axis

    private const string Stance3HandsPositions = "Stance 3 Hands/Arms Positions";
    public static ConfigEntry<float> _Stance3HandsForwardBackwardOffset; // Z-axis
    public static ConfigEntry<float> _Stance3HandsUpDownOffset;          // Y-axis
    public static ConfigEntry<float> _Stance3HandsSidewaysOffset;        // X-axis

    // Stance 3 Sprint Animation
    private const string Stance3Animation = "Stance 3 Animation";
    public static ConfigEntry<bool> _Stance3SprintAnimationEnabled;

    // Shared Tac Sprint Settings (Advanced)
    private const string TacSprintSettings = "Tac Sprint Settings (Advanced)";
    public static ConfigEntry<float> _TacSprintWeightLimit;
    public static ConfigEntry<float> _TacSprintWeightLimitBullpup;
    public static ConfigEntry<int> _TacSprintLengthLimit;
    public static ConfigEntry<float> _TacSprintErgoLimit;

    private const string Positions = "Positions";
    public static ConfigEntry<float> _ForwardBackwardOffset; // Z-axis
    public static ConfigEntry<float> _UpDownOffset;          // Y-axis
    public static ConfigEntry<float> _SidewaysOffset;        // X-axis

    private const string ADSDefaults = "ADS Default Values (Advanced)";
    public static ConfigEntry<float> _ADSHandsPitchRotation;
    public static ConfigEntry<float> _ADSHandsYawRotation;
    public static ConfigEntry<float> _ADSHandsRollRotation;
    public static ConfigEntry<float> _ADSHandsForwardBackwardOffset;
    public static ConfigEntry<float> _ADSHandsUpDownOffset;
    public static ConfigEntry<float> _ADSHandsSidewaysOffset;

    public void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Camera Rotation Mod has loaded!");

        // Enable patches
        new PlayerSpringPatch().Enable(); // Handles camera position
        new SpringGetPatch().Enable(); // Handles stance rotation/position transitions

        // Configuration settings
        _PositionEnabled = Config.Bind(
            Settings,
            "Enable Camera Position",
            true,
            new ConfigDescription("Enable or disable camera position offsets"));

        _ResetOnADS = Config.Bind(
            Settings,
            "Reset Positions When Aiming",
            true,
            new ConfigDescription("When enabled, smoothly transitions all positions to defaults when ADS",
            null,
            new ConfigurationManagerAttributes { IsAdvanced = true }));

        _ADSTransitionSpeed = Config.Bind(
            Settings,
            "ADS Transition Speed",
            2f,
            new ConfigDescription("How quickly hands transition between stance and ADS positions. 1 = slow, 2 = normal, 3+ = fast/snappy.",
            new AcceptableValueRange<float>(0.5f, 5f),
            new ConfigurationManagerAttributes { IsAdvanced = true }));

        _StanceToggleKey = Config.Bind(
            Settings,
            "Stance Toggle Hotkey",
            KeyCode.V,
            new ConfigDescription("Press this key to cycle through enabled stances: Default → Stance 1 → Stance 2 → Stance 3 → Default"));

        _UseOnlyStances = Config.Bind(
            Settings,
            "Use Only Stances",
            false,
            new ConfigDescription("When enabled, cycle skips Default (non-stance) position and only cycles through enabled stances"));

        // Camera Position Offsets
        _ForwardBackwardOffset = Config.Bind(
            Positions,
            "Forward/Backward Offset",
            0f,
            new ConfigDescription("Camera position forward/backward (positive = forward)",
            new AcceptableValueRange<float>(-0.5f, 0.5f),
            new ConfigurationManagerAttributes { Order = 3 }));

        _UpDownOffset = Config.Bind(
            Positions,
            "Up/Down Offset",
            0.02f,
            new ConfigDescription("Camera position up/down (positive = up)",
            new AcceptableValueRange<float>(-0.5f, 0.5f),
            new ConfigurationManagerAttributes { Order = 2 }));

        _SidewaysOffset = Config.Bind(
            Positions,
            "Sideways Offset",
            0f,
            new ConfigDescription("Camera position left/right (positive = right)",
            new AcceptableValueRange<float>(-0.5f, 0.5f),
            new ConfigurationManagerAttributes { Order = 1 }));

        _EnableStance1 = Config.Bind(
            Settings,
            "Enable Stance 1 in Cycle",
            true,
            new ConfigDescription("When enabled, Stance 1 is included in the stance cycle. When disabled, Stance 1 is skipped."));

        _EnableStance2 = Config.Bind(
            Settings,
            "Enable Stance 2 in Cycle",
            true,
            new ConfigDescription("When enabled, Stance 2 is included in the stance cycle. When disabled, Stance 2 is skipped."));

        _EnableStance3 = Config.Bind(
            Settings,
            "Enable Stance 3 in Cycle",
            false,
            new ConfigDescription("When enabled, Stance 3 is included in the stance cycle. When disabled, Stance 3 is skipped."));

        _EnableMouseWheelCycle = Config.Bind(
            Settings,
            "Enable Mouse Wheel Stance Cycle",
            false,
            new ConfigDescription("When enabled, hold the modifier key and scroll mouse wheel to cycle stances"));

        _MouseWheelModifierKey = Config.Bind(
            Settings,
            "Mouse Wheel Modifier Key",
            KeyCode.LeftAlt,
            new ConfigDescription("Hold this key while scrolling mouse wheel to cycle stances (when mouse wheel cycling is enabled)"));

        _StanceTransitionSpeed = Config.Bind(
            Settings,
            "Stance Transition Speed",
            2f,
            new ConfigDescription("How quickly hands transition between Default and Stance. Higher = faster/snappier, Lower = slower/smoother. Recommended: 3-10",
            new AcceptableValueRange<float>(0.5f, 20f)));

        // ADS Default Values (Advanced)
        _ADSHandsPitchRotation = Config.Bind(
            ADSDefaults,
            "ADS Hands Pitch Rotation",
            0f,
            new ConfigDescription("Hands pitch rotation (X-axis) when ADS with 'Reset On ADS' enabled. 0 = default game position",
            new AcceptableValueRange<float>(-45f, 45f),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = 26 }));

        _ADSHandsYawRotation = Config.Bind(
            ADSDefaults,
            "ADS Hands Yaw Rotation",
            0f,
            new ConfigDescription("Hands yaw rotation (Y-axis) when ADS with 'Reset On ADS' enabled. 0 = default game position",
            new AcceptableValueRange<float>(-45f, 45f),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = 25 }));

        _ADSHandsRollRotation = Config.Bind(
            ADSDefaults,
            "ADS Hands Roll Rotation",
            0f,
            new ConfigDescription("Hands roll rotation (Z-axis) when ADS with 'Reset On ADS' enabled. 0 = default game position",
            new AcceptableValueRange<float>(-45f, 45f),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = 24 }));

        _ADSHandsForwardBackwardOffset = Config.Bind(
            ADSDefaults,
            "ADS Hands Forward/Backward Offset",
            0f,
            new ConfigDescription("Hands position forward/backward (Z-axis) when ADS. Default is 0.04",
            new AcceptableValueRange<float>(-0.5f, 0.5f),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = 23 }));

        _ADSHandsUpDownOffset = Config.Bind(
            ADSDefaults,
            "ADS Hands Up/Down Offset",
            0f,
            new ConfigDescription("Hands position up/down (Y-axis) when ADS. Default is 0.04",
            new AcceptableValueRange<float>(-0.5f, 0.5f),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = 22 }));

        _ADSHandsSidewaysOffset = Config.Bind(
            ADSDefaults,
            "ADS Hands Sideways Offset",
            0f,
            new ConfigDescription("Hands position left/right (X-axis) when ADS. Default is 0.04",
            new AcceptableValueRange<float>(-0.5f, 0.5f),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = 21 }));

        // Default Hands Positions (no rotation - always 0,0,0) - Advanced
        _DefaultHandsPositionEnabled = Config.Bind(
            Settings,
            "Enable Default Hands/Arms Position",
            false,
            new ConfigDescription("Enable or disable default hands/arms position offsets when NOT in stance",
            null,
            new ConfigurationManagerAttributes { IsAdvanced = true }));

        _DefaultHandsForwardBackwardOffset = Config.Bind(
            DefaultHandsPositions,
            "Default Hands Forward/Backward Offset",
            0f,
            new ConfigDescription("Default hands/weapon position forward/backward (positive = forward). This is your normal hip-fire position.",
            new AcceptableValueRange<float>(-0.5f, 0.5f),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = 3 }));

        _DefaultHandsUpDownOffset = Config.Bind(
            DefaultHandsPositions,
            "Default Hands Up/Down Offset",
            0f,
            new ConfigDescription("Default hands/weapon position up/down (positive = up). This is your normal hip-fire position.",
            new AcceptableValueRange<float>(-0.5f, 0.5f),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = 2 }));

        _DefaultHandsSidewaysOffset = Config.Bind(
            DefaultHandsPositions,
            "Default Hands Sideways Offset",
            0f,
            new ConfigDescription("Default hands/weapon position left/right (positive = right). This is your normal hip-fire position.",
            new AcceptableValueRange<float>(-0.5f, 0.5f),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = 1 }));

        // Stance 1 Hands Rotation and Position
        _Stance1HandsPitchRotation = Config.Bind(
            Stance1HandsRotations,
            "Stance 1 Hands Pitch (X-Axis)",
            -15f,
            new ConfigDescription("Stance 1 hands/arms pitch rotation in degrees (up/down tilt)",
            new AcceptableValueRange<float>(-45f, 45f),
            new ConfigurationManagerAttributes { Order = 3 }));

        _Stance1HandsYawRotation = Config.Bind(
            Stance1HandsRotations,
            "Stance 1 Hands Yaw (Y-Axis)",
            -15f,
            new ConfigDescription("Stance 1 hands/arms yaw rotation in degrees (left/right turn)",
            new AcceptableValueRange<float>(-45f, 45f),
            new ConfigurationManagerAttributes { Order = 2 }));

        _Stance1HandsRollRotation = Config.Bind(
            Stance1HandsRotations,
            "Stance 1 Hands Roll (Z-Axis)",
            0f,
            new ConfigDescription("Stance 1 hands/arms roll rotation in degrees (weapon cant)",
            new AcceptableValueRange<float>(-45f, 45f),
            new ConfigurationManagerAttributes { Order = 1 }));

        _Stance1HandsForwardBackwardOffset = Config.Bind(
            Stance1HandsPositions,
            "Stance 1 Hands Forward/Backward Offset",
            -0.15f,
            new ConfigDescription("Stance 1 hands/weapon position forward/backward (positive = forward)",
            new AcceptableValueRange<float>(-0.5f, 0.5f),
            new ConfigurationManagerAttributes { Order = 3 }));

        _Stance1HandsUpDownOffset = Config.Bind(
            Stance1HandsPositions,
            "Stance 1 Hands Up/Down Offset",
            0f,
            new ConfigDescription("Stance 1 hands/weapon position up/down (positive = up)",
            new AcceptableValueRange<float>(-0.5f, 0.5f),
            new ConfigurationManagerAttributes { Order = 2 }));

        _Stance1HandsSidewaysOffset = Config.Bind(
            Stance1HandsPositions,
            "Stance 1 Hands Sideways Offset",
            0f,
            new ConfigDescription("Stance 1 hands/weapon position left/right (positive = right)",
            new AcceptableValueRange<float>(-0.5f, 0.5f),
            new ConfigurationManagerAttributes { Order = 1 }));

        // Stance 1 Sprint Animation
        _Stance1SprintAnimationEnabled = Config.Bind(
            Stance1Animation,
            "Enable Stance 1 Sprint Animation",
            true,
            new ConfigDescription("When enabled, uses a compact sprint animation when sprinting in Stance 1 (tac sprint style)"));

        // Stance 2 Hands Rotation and Position
        _Stance2HandsPitchRotation = Config.Bind(
            Stance2HandsRotations,
            "Stance 2 Hands Pitch (X-Axis)",
            0f,
            new ConfigDescription("Stance 2 hands/arms pitch rotation in degrees (up/down tilt)",
            new AcceptableValueRange<float>(-45f, 45f),
            new ConfigurationManagerAttributes { Order = 3 }));

        _Stance2HandsYawRotation = Config.Bind(
            Stance2HandsRotations,
            "Stance 2 Hands Yaw (Y-Axis)",
            -30f,
            new ConfigDescription("Stance 2 hands/arms yaw rotation in degrees (left/right turn)",
            new AcceptableValueRange<float>(-45f, 45f),
            new ConfigurationManagerAttributes { Order = 2 }));

        _Stance2HandsRollRotation = Config.Bind(
            Stance2HandsRotations,
            "Stance 2 Hands Roll (Z-Axis)",
            0f,
            new ConfigDescription("Stance 2 hands/arms roll rotation in degrees (weapon cant)",
            new AcceptableValueRange<float>(-45f, 45f),
            new ConfigurationManagerAttributes { Order = 1 }));

        _Stance2HandsForwardBackwardOffset = Config.Bind(
            Stance2HandsPositions,
            "Stance 2 Hands Forward/Backward Offset",
            0f,
            new ConfigDescription("Stance 2 hands/weapon position forward/backward (positive = forward)",
            new AcceptableValueRange<float>(-0.5f, 0.5f),
            new ConfigurationManagerAttributes { Order = 3 }));

        _Stance2HandsUpDownOffset = Config.Bind(
            Stance2HandsPositions,
            "Stance 2 Hands Up/Down Offset",
            0f,
            new ConfigDescription("Stance 2 hands/weapon position up/down (positive = up)",
            new AcceptableValueRange<float>(-0.5f, 0.5f),
            new ConfigurationManagerAttributes { Order = 2 }));

        _Stance2HandsSidewaysOffset = Config.Bind(
            Stance2HandsPositions,
            "Stance 2 Hands Sideways Offset",
            0f,
            new ConfigDescription("Stance 2 hands/weapon position left/right (positive = right)",
            new AcceptableValueRange<float>(-0.5f, 0.5f),
            new ConfigurationManagerAttributes { Order = 1 }));

        // Stance 2 Sprint Animation
        _Stance2SprintAnimationEnabled = Config.Bind(
            Stance2Animation,
            "Enable Stance 2 Sprint Animation",
            false,
            new ConfigDescription("When enabled, uses a compact sprint animation when sprinting in Stance 2 (tac sprint style)"));

        // Stance 3 Hands Rotation and Position
        _Stance3HandsPitchRotation = Config.Bind(
            Stance3HandsRotations,
            "Stance 3 Hands Pitch (X-Axis)",
            30f,
            new ConfigDescription("Stance 3 hands/arms pitch rotation in degrees (up/down tilt)",
            new AcceptableValueRange<float>(-45f, 45f),
            new ConfigurationManagerAttributes { Order = 3 }));

        _Stance3HandsYawRotation = Config.Bind(
            Stance3HandsRotations,
            "Stance 3 Hands Yaw (Y-Axis)",
            0f,
            new ConfigDescription("Stance 3 hands/arms yaw rotation in degrees (left/right turn)",
            new AcceptableValueRange<float>(-45f, 45f),
            new ConfigurationManagerAttributes { Order = 2 }));

        _Stance3HandsRollRotation = Config.Bind(
            Stance3HandsRotations,
            "Stance 3 Hands Roll (Z-Axis)",
            15f,
            new ConfigDescription("Stance 3 hands/arms roll rotation in degrees (weapon cant)",
            new AcceptableValueRange<float>(-45f, 45f),
            new ConfigurationManagerAttributes { Order = 1 }));

        _Stance3HandsForwardBackwardOffset = Config.Bind(
            Stance3HandsPositions,
            "Stance 3 Hands Forward/Backward Offset",
            0.03f,
            new ConfigDescription("Stance 3 hands/weapon position forward/backward (positive = forward)",
            new AcceptableValueRange<float>(-0.5f, 0.5f),
            new ConfigurationManagerAttributes { Order = 3 }));

        _Stance3HandsUpDownOffset = Config.Bind(
            Stance3HandsPositions,
            "Stance 3 Hands Up/Down Offset",
            0f,
            new ConfigDescription("Stance 3 hands/weapon position up/down (positive = up)",
            new AcceptableValueRange<float>(-0.5f, 0.5f),
            new ConfigurationManagerAttributes { Order = 2 }));

        _Stance3HandsSidewaysOffset = Config.Bind(
            Stance3HandsPositions,
            "Stance 3 Hands Sideways Offset",
            0.04f,
            new ConfigDescription("Stance 3 hands/weapon position left/right (positive = right)",
            new AcceptableValueRange<float>(-0.5f, 0.5f),
            new ConfigurationManagerAttributes { Order = 1 }));

        // Stance 3 Sprint Animation
        _Stance3SprintAnimationEnabled = Config.Bind(
            Stance3Animation,
            "Enable Stance 3 Sprint Animation",
            false,
            new ConfigDescription("When enabled, uses a compact sprint animation when sprinting in Stance 3 (tac sprint style)"));

        // Shared Tac Sprint Limits (Advanced)
        _TacSprintWeightLimit = Config.Bind(
            TacSprintSettings,
            "Tac Sprint Weight Limit",
            5.1f,
            new ConfigDescription("Maximum weapon weight (kg) to allow tac sprint animation. Default: 5.1kg",
            new AcceptableValueRange<float>(1f, 15f),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = 24 }));

        _TacSprintWeightLimitBullpup = Config.Bind(
            TacSprintSettings,
            "Tac Sprint Weight Limit (Bullpup)",
            5.75f,
            new ConfigDescription("Maximum weapon weight (kg) for bullpup weapons to allow tac sprint. Bullpups get a higher limit. Default: 5.75kg",
            new AcceptableValueRange<float>(1f, 15f),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = 23 }));

        _TacSprintLengthLimit = Config.Bind(
            TacSprintSettings,
            "Tac Sprint Length Limit",
            6,
            new ConfigDescription("Maximum weapon length (inventory cells) to allow tac sprint animation. Default: 6 cells",
            new AcceptableValueRange<int>(1, 10),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = 22 }));

        _TacSprintErgoLimit = Config.Bind(
            TacSprintSettings,
            "Tac Sprint Ergo Limit",
            35f,
            new ConfigDescription("Minimum weapon ergonomics to allow tac sprint animation. Default: 35",
            new AcceptableValueRange<float>(0f, 100f),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = 21 }));

        // Initialize StanceManager
        StanceManager.Initialize(_StanceToggleKey);

        // Note: RotationEvents and SetItemInHandsPatch are deprecated now
        // All logic is handled by StanceManager + SpringGetPatch
    }

    // Update is called every frame by Unity
    public void Update()
    {
        StanceManager.Update();
        StanceManager.UpdateTacSprint();
        UpdateCameraOffset();
    }
    
    private void UpdateCameraOffset()
    {
        var gameWorld = Comfort.Common.Singleton<EFT.GameWorld>.Instance;
        if (gameWorld?.MainPlayer?.ProceduralWeaponAnimation?.HandsContainer == null)
            return;
            
        var handsContainer = gameWorld.MainPlayer.ProceduralWeaponAnimation.HandsContainer;
        
        bool isEnabled = _PositionEnabled?.Value ?? false;
        
        Vector3 targetOffset = isEnabled ? new Vector3(
            _SidewaysOffset.Value,
            _UpDownOffset.Value,
            _ForwardBackwardOffset.Value
        ) : new Vector3(0.04f, 0.04f, 0.04f);
        
        handsContainer.CameraOffset = targetOffset;
    }
}