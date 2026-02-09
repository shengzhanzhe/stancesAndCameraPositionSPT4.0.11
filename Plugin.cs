using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CameraRotationMod.Patches;
using UnityEngine;

namespace CameraRotationMod;

[BepInPlugin("shwng.camerarotation", "shwng.FpsCameraStances", "0.9.9")]
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

    // FOV Settings
    private const string FOVSettings = "Field of View";
    public static ConfigEntry<bool> _FOVExpandEnabled;
    public static ConfigEntry<int> _FOVMinRange;
    public static ConfigEntry<int> _FOVMaxRange;

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

    // Advanced ADS Transition (Shouldering Effect)
    private const string AdvancedADSSettings = "Advanced ADS Transitions";
    public static ConfigEntry<bool> _EnableAdvancedADSTransitions;
    public static ConfigEntry<bool> _AffectStanceTransitionToo;
    public static ConfigEntry<float> _StanceChangeSoundVolume;
    public static ConfigEntry<float> _AdvancedStanceTransitionIntensity;
    public static ConfigEntry<bool> _ScaleByWeaponStats;
    public static ConfigEntry<float> _WeaponStatsScaleIntensity;
    public static ConfigEntry<float> _ADSShoulderThrowForward;
    public static ConfigEntry<float> _ADSShoulderThrowUp;
    public static ConfigEntry<float> _ADSShoulderThrowDuration;
    public static ConfigEntry<float> _ADSShoulderThrowSpeed;
    public static ConfigEntry<float> _ADSShoulderSettleSpeed;

    // Overall throw intensity multipliers
    public static ConfigEntry<float> _ADSShoulderThrowIntensity;
    public static ConfigEntry<float> _StanceShoulderThrowIntensity;

    // Rotation throw (yaw, pitch, roll) for ADS/Stance shouldering
    public static ConfigEntry<float> _ADSShoulderThrowYaw;
    public static ConfigEntry<float> _ADSShoulderThrowPitch;
    public static ConfigEntry<float> _ADSShoulderThrowRoll;

    public void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Camera Rotation Mod has loaded!");

        // Enable patches
        new PlayerSpringPatch().Enable(); // Handles camera position
        new SpringGetPatch().Enable(); // Handles stance rotation/position transitions
        new FOVSliderPatch().Enable(); // Extends FOV slider range in settings
        new FOVClampPatch().Enable(); // Allows FOV values outside default 50-75 range

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
            new ConfigDescription("When enabled, smoothly transitions all positions to defaults when ADS"));

        // Advanced ADS Transitions (Shouldering Effect)
        _EnableAdvancedADSTransitions = Config.Bind(
            AdvancedADSSettings,
            "Advanced ADS Transitions",
            false,
            new ConfigDescription("When enabled, weapon is thrown forward then pushed back when aiming to simulate shouldering",
            null,
            new ConfigurationManagerAttributes { Order = 8 }));

        _AffectStanceTransitionToo = Config.Bind(
            AdvancedADSSettings,
            "Affect Stance Transition Too",
            true,
            new ConfigDescription("When enabled (requires 'Advanced ADS Transitions'), applies the same shouldering effect when switching between stances",
            null,
            new ConfigurationManagerAttributes { Order = 8 }));

        _ADSShoulderThrowIntensity = Config.Bind(
            AdvancedADSSettings,
            "ADS Shoulder Throw Intensity",
            1f,
            new ConfigDescription("Overall intensity of ADS throw effect. Multiplies Forward, Up, Yaw, Pitch, Roll amounts. 0 = no throw, 1 = use config values, 2 = double effect.",
            new AcceptableValueRange<float>(0f, 2f),
            new ConfigurationManagerAttributes { Order = 7 }));

        _StanceShoulderThrowIntensity = Config.Bind(
            AdvancedADSSettings,
            "Stance Shoulder Throw Intensity",
            0.75f,
            new ConfigDescription("Overall intensity of stance switch throw effect. Multiplies Forward, Up, Yaw, Pitch, Roll amounts. 0 = no throw, 1 = use config values, 2 = double effect.",
            new AcceptableValueRange<float>(0f, 2f),
            new ConfigurationManagerAttributes { Order = 6 }));

        _StanceChangeSoundVolume = Config.Bind(
            AdvancedADSSettings,
            "Stance Change Sound Volume",
            1f,
            new ConfigDescription("Volume multiplier for the aim rattle sound when switching stances. 0 = muted, 1 = normal, 2 = louder. Requires 'Affect Stance Transition Too' enabled.",
            new AcceptableValueRange<float>(0f, 2f),
            new ConfigurationManagerAttributes { Order = 5 }));

        _AdvancedStanceTransitionIntensity = Config.Bind(
            AdvancedADSSettings,
            "Advanced Stance Transition Stat Intensity",
            1f,
            new ConfigDescription("How strongly weapon weight/ergonomics affects stance transition speed and shouldering. 0.01 = minimal effect, 1 = normal, 2 = exaggerated. Works when 'Affect Stance Transition Too' is enabled.",
            new AcceptableValueRange<float>(0.01f, 2f),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = 4 }));

        _ScaleByWeaponStats = Config.Bind(
            AdvancedADSSettings,
            "Scale by Weapon Stats",
            true,
            new ConfigDescription("When enabled, shouldering speed/duration/amount scales with weapon weight and ergonomics (uses EFT's AimingSpeed calculation). Heavy/low-ergo = slower, dramatic. Light/high-ergo = fast, subtle. Needs Enable Advanced ADS Transitions enabled to have an effect.",
            null,
            new ConfigurationManagerAttributes { Order = 3 }));

        _WeaponStatsScaleIntensity = Config.Bind(
            AdvancedADSSettings,
            "Advanced ADS Transition Stat Intensity",
            1f,
            new ConfigDescription("How strongly weapon stats affect ADS shouldering. 0 = no scaling (all weapons same), 1 = normal, 2 = exaggerated difference between light/heavy weapons.",
            new AcceptableValueRange<float>(0f, 2f),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = 2 }));

        _ADSShoulderThrowForward = Config.Bind(
            AdvancedADSSettings,
            "Shoulder Throw Forward Amount",
            0.02f,
            new ConfigDescription("Base forward throw distance. With 'Scale by Weapon Stats' enabled, this is multiplied by inverse AimingSpeed (heavy weapons throw more).",
            new AcceptableValueRange<float>(0f, 0.3f),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = 3 }));

        _ADSShoulderThrowUp = Config.Bind(
            AdvancedADSSettings,
            "Shoulder Throw Up Amount",
            -0.015f,
            new ConfigDescription("Base vertical offset during throw. Negative = down. With 'Scale by Weapon Stats', scales with inverse AimingSpeed.",
            new AcceptableValueRange<float>(-0.15f, 0.15f),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = 2 }));

        _ADSShoulderThrowDuration = Config.Bind(
            AdvancedADSSettings,
            "Shoulder Throw Duration",
            0.15f,
            new ConfigDescription("Base throw phase duration (seconds). With 'Scale by Weapon Stats', heavy weapons have longer duration.",
            new AcceptableValueRange<float>(0.01f, 0.5f),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = 1 }));

        _ADSShoulderThrowSpeed = Config.Bind(
            AdvancedADSSettings,
            "Shoulder Throw Speed",
            2f,
            new ConfigDescription("Base speed of throw motion. With 'Scale by Weapon Stats', multiplied by AimingSpeed (light weapons = faster).",
            new AcceptableValueRange<float>(0.5f, 5f),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = 0 }));

        _ADSShoulderSettleSpeed = Config.Bind(
            AdvancedADSSettings,
            "Shoulder Settle Speed",
            1.5f,
            new ConfigDescription("Base speed of settling to ADS. With 'Scale by Weapon Stats', multiplied by AimingSpeed.",
            new AcceptableValueRange<float>(0.5f, 5f),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = -1 }));

        _ADSShoulderThrowYaw = Config.Bind(
            AdvancedADSSettings,
            "Shoulder Throw Yaw",
            6f,
            new ConfigDescription("Yaw rotation during throw phase (degrees). Positive = rotate right. Applied to both ADS and stance transitions.",
            new AcceptableValueRange<float>(-15f, 15f),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = -2 }));

        _ADSShoulderThrowPitch = Config.Bind(
            AdvancedADSSettings,
            "Shoulder Throw Pitch",
            -3f,
            new ConfigDescription("Pitch rotation during throw phase (degrees). Positive = rotate up. Applied to both ADS and stance transitions.",
            new AcceptableValueRange<float>(-15f, 15f),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = -3 }));

        _ADSShoulderThrowRoll = Config.Bind(
            AdvancedADSSettings,
            "Shoulder Throw Roll",
            -1.5f,
            new ConfigDescription("Roll rotation during throw phase (degrees). Positive = tilt right. Applied to both ADS and stance transitions.",
            new AcceptableValueRange<float>(-15f, 15f),
            new ConfigurationManagerAttributes { IsAdvanced = true, Order = -4 }));

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
            1f,
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
            0f,
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
            0f,
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

        // FOV Settings
        _FOVExpandEnabled = Config.Bind(
            FOVSettings,
            "Enable Expanded FOV Range",
            false,
            new ConfigDescription("Allows extending the FOV slider beyond the default 50-75 range",
            null,
            new ConfigurationManagerAttributes { Order = 3 }));

        _FOVMinRange = Config.Bind(
            FOVSettings,
            "Minimum FOV",
            20,
            new ConfigDescription("Minimum FOV value. Default game minimum is 50",
            new AcceptableValueRange<int>(1, 50),
            new ConfigurationManagerAttributes { Order = 2 }));

        _FOVMaxRange = Config.Bind(
            FOVSettings,
            "Maximum FOV",
            150,
            new ConfigDescription("Maximum FOV value. Default game maximum is 75",
            new AcceptableValueRange<int>(75, 170),
            new ConfigurationManagerAttributes { Order = 1 }));

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