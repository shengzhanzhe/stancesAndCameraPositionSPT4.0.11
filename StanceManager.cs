using UnityEngine;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;

namespace CameraRotationMod
{
    public enum Stance
    {
        Default,
        Stance1,
        Stance2,
        Stance3
    }

    /// <summary>
    /// Manages stance toggling between Default, Stance1, and Stance2 positions/rotations
    /// </summary>
    public static class StanceManager
    {
        public static Stance CurrentStance { get; private set; } = Stance.Default;
        public static bool IsInStance => CurrentStance != Stance.Default;
        
        private static ConfigEntry<KeyCode> _stanceToggleKeyConfig;
        private static ConfigEntry<bool> _enableMouseWheelCycleConfig;
        private static ConfigEntry<KeyCode> _mouseWheelModifierKeyConfig;
        private static bool _wasKeyPressed = false;
        private static float _lastScrollTime = 0f;
        private const float ScrollCooldown = 0.15f; // Prevent scroll spam

        // Tac Sprint variables - track state to avoid setting animator every frame
        private static bool _isTacSprintActive = false;
        private static bool _wasAiming = false;
        
        // Track GameWorld to detect raid changes and reset state
        private static GameWorld _lastGameWorld = null;

        public static void Initialize(ConfigEntry<KeyCode> stanceToggleKeyConfig)
        {
            _stanceToggleKeyConfig = stanceToggleKeyConfig;
            _enableMouseWheelCycleConfig = Plugin._EnableMouseWheelCycle;
            _mouseWheelModifierKeyConfig = Plugin._MouseWheelModifierKey;
        }

        public static void Update()
        {
            // Check if the stance toggle key is pressed (simple keycode check, works with other keys held)
            bool isKeyPressed = UnityEngine.Input.GetKeyDown(_stanceToggleKeyConfig.Value);
            
            // Cycle through stances on key press
            if (isKeyPressed && !_wasKeyPressed)
            {
                CurrentStance = GetNextStance(CurrentStance);
                Plugin.Logger.LogInfo($"Stance changed to: {CurrentStance}");
            }
            
            _wasKeyPressed = isKeyPressed;

            // Mouse wheel cycling
            if (_enableMouseWheelCycleConfig?.Value == true)
            {
                // Check if modifier key is held
                if (UnityEngine.Input.GetKey(_mouseWheelModifierKeyConfig.Value))
                {
                    float scrollDelta = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
                    
                    // Check cooldown to prevent rapid cycling
                    if (scrollDelta != 0 && Time.time - _lastScrollTime > ScrollCooldown)
                    {
                        if (scrollDelta > 0)
                        {
                            // Scroll up - cycle forward
                            CurrentStance = GetNextStance(CurrentStance);
                            Plugin.Logger.LogInfo($"Stance changed to: {CurrentStance} (scroll up)");
                        }
                        else
                        {
                            // Scroll down - cycle backward
                            CurrentStance = GetPreviousStance(CurrentStance);
                            Plugin.Logger.LogInfo($"Stance changed to: {CurrentStance} (scroll down)");
                        }
                        _lastScrollTime = Time.time;
                    }
                }
            }
        }

        /// <summary>
        /// Get the next enabled stance in the cycle, skipping disabled stances
        /// </summary>
        private static Stance GetNextStance(Stance current)
        {
            bool useOnlyStances = Plugin._UseOnlyStances?.Value ?? false;
            int maxAttempts = 5; // Prevent infinite loops
            int attempts = 0;
            
            Stance next = current;
            
            do
            {
                // Move to next stance in sequence
                next = next switch
                {
                    Stance.Default => Stance.Stance1,
                    Stance.Stance1 => Stance.Stance2,
                    Stance.Stance2 => Stance.Stance3,
                    Stance.Stance3 => Stance.Default,
                    _ => Stance.Default
                };
                
                attempts++;
                
                // Check if this stance should be included in cycle
                if (IsStanceEnabled(next, useOnlyStances))
                    return next;
                    
            } while (attempts < maxAttempts && next != current);
            
            // If we cycled through everything and nothing is enabled, return current
            return current;
        }

        /// <summary>
        /// Get the previous enabled stance in the cycle, skipping disabled stances (for scroll down)
        /// </summary>
        private static Stance GetPreviousStance(Stance current)
        {
            bool useOnlyStances = Plugin._UseOnlyStances?.Value ?? false;
            int maxAttempts = 5; // Prevent infinite loops
            int attempts = 0;
            
            Stance prev = current;
            
            do
            {
                // Move to previous stance in sequence (reverse order)
                prev = prev switch
                {
                    Stance.Default => Stance.Stance3,
                    Stance.Stance1 => Stance.Default,
                    Stance.Stance2 => Stance.Stance1,
                    Stance.Stance3 => Stance.Stance2,
                    _ => Stance.Default
                };
                
                attempts++;
                
                // Check if this stance should be included in cycle
                if (IsStanceEnabled(prev, useOnlyStances))
                    return prev;
                    
            } while (attempts < maxAttempts && prev != current);
            
            // If we cycled through everything and nothing is enabled, return current
            return current;
        }
        
        /// <summary>
        /// Check if a stance is enabled and should be included in the cycle
        /// </summary>
        private static bool IsStanceEnabled(Stance stance, bool useOnlyStances)
        {
            switch (stance)
            {
                case Stance.Default:
                    // Default is included unless "use only stances" is enabled
                    return !useOnlyStances;
                    
                case Stance.Stance1:
                    return Plugin._EnableStance1?.Value ?? true;
                    
                case Stance.Stance2:
                    return Plugin._EnableStance2?.Value ?? true;
                    
                case Stance.Stance3:
                    return Plugin._EnableStance3?.Value ?? true;
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Check if player is holding a firearm (not melee, empty hands, or using items)
        /// </summary>
        public static bool IsHoldingFirearm()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld?.MainPlayer == null)
                return false;
            
            // HandsController will be FirearmController when holding a gun
            // It will be different controller types for melee, meds, food, grenades, empty hands
            return gameWorld.MainPlayer.HandsController is Player.FirearmController;
        }

        /// <summary>
        /// Get the current target rotation based on stance state and ADS state
        /// </summary>
        public static Vector3 GetTargetRotation(bool isAiming)
        {
            // If ADS and reset is enabled, return ADS rotation
            if (isAiming && (Plugin._ResetOnADS?.Value ?? false))
            {
                return new Vector3(
                    Plugin._ADSHandsPitchRotation.Value,
                    Plugin._ADSHandsYawRotation.Value,
                    Plugin._ADSHandsRollRotation.Value
                );
            }

            // If in Stance1 mode, return stance1 rotation
            if (CurrentStance == Stance.Stance1)
            {
                return new Vector3(
                    Plugin._Stance1HandsPitchRotation.Value,
                    Plugin._Stance1HandsYawRotation.Value,
                    Plugin._Stance1HandsRollRotation.Value
                );
            }

            // If in Stance2 mode, return stance2 rotation
            if (CurrentStance == Stance.Stance2)
            {
                return new Vector3(
                    Plugin._Stance2HandsPitchRotation.Value,
                    Plugin._Stance2HandsYawRotation.Value,
                    Plugin._Stance2HandsRollRotation.Value
                );
            }

            // If in Stance3 mode, return stance3 rotation
            if (CurrentStance == Stance.Stance3)
            {
                return new Vector3(
                    Plugin._Stance3HandsPitchRotation.Value,
                    Plugin._Stance3HandsYawRotation.Value,
                    Plugin._Stance3HandsRollRotation.Value
                );
            }

            // Default rotation is always 0,0,0
            return Vector3.zero;
        }

        /// <summary>
        /// Get the current target position based on stance state and ADS state
        /// </summary>
        public static Vector3 GetTargetPosition(bool isAiming)
        {
            // If ADS and reset is enabled, return ADS position
            if (isAiming && (Plugin._ResetOnADS?.Value ?? false))
            {
                return new Vector3(
                    Plugin._ADSHandsSidewaysOffset.Value,
                    Plugin._ADSHandsUpDownOffset.Value,
                    Plugin._ADSHandsForwardBackwardOffset.Value
                );
            }

            // If in Stance1 mode, return stance1 position
            if (CurrentStance == Stance.Stance1)
            {
                return new Vector3(
                    Plugin._Stance1HandsSidewaysOffset.Value,
                    Plugin._Stance1HandsUpDownOffset.Value,
                    Plugin._Stance1HandsForwardBackwardOffset.Value
                );
            }

            // If in Stance2 mode, return stance2 position
            if (CurrentStance == Stance.Stance2)
            {
                return new Vector3(
                    Plugin._Stance2HandsSidewaysOffset.Value,
                    Plugin._Stance2HandsUpDownOffset.Value,
                    Plugin._Stance2HandsForwardBackwardOffset.Value
                );
            }

            // If in Stance3 mode, return stance3 position
            if (CurrentStance == Stance.Stance3)
            {
                return new Vector3(
                    Plugin._Stance3HandsSidewaysOffset.Value,
                    Plugin._Stance3HandsUpDownOffset.Value,
                    Plugin._Stance3HandsForwardBackwardOffset.Value
                );
            }

            // Return default position
            if (Plugin._DefaultHandsPositionEnabled.Value)
            {
                return new Vector3(
                    Plugin._DefaultHandsSidewaysOffset.Value,
                    Plugin._DefaultHandsUpDownOffset.Value,
                    Plugin._DefaultHandsForwardBackwardOffset.Value
                );
            }

            // If nothing enabled, return zero
            return Vector3.zero;
        }

        /// <summary>
        /// Resets all stance state - called when entering new raid or GameWorld changes
        /// </summary>
        public static void ResetState()
        {
            CurrentStance = Stance.Default;
            _isTacSprintActive = false;
            _wasAiming = false;
            _lastGameWorld = null;
            _wasKeyPressed = false;
            _lastScrollTime = 0f;
        }
        
        /// <summary>
        /// Updates the tac sprint animation state based on stance and sprint status
        /// Uses state tracking to avoid setting animator parameters every frame
        /// </summary>
        public static void UpdateTacSprint()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld?.MainPlayer == null)
            {
                // Reset state when no player
                ResetState();
                return;
            }
            
            // Detect GameWorld change (new raid) and reset state
            if (_lastGameWorld != gameWorld)
            {
                // Reset but don't change stance preference - user may want to keep their stance
                _isTacSprintActive = false;
                _wasAiming = false;
                _lastGameWorld = gameWorld;
            }

            var player = gameWorld.MainPlayer;
            
            // CRITICAL: If player switched away from firearm (e.g., consumables, melee), 
            // immediately disable tac sprint to prevent stuck animation glitch
            if (_isTacSprintActive && !IsHoldingFirearm())
            {
                DisableTacSprint(player);
                return;
            }
            
            // Check if player is aiming - CRITICAL for preventing stuck sprint
            bool isAiming = player.ProceduralWeaponAnimation?.IsAiming ?? false;
            
            // If we just entered ADS, immediately disable tac sprint
            if (isAiming && !_wasAiming && _isTacSprintActive)
            {
                DisableTacSprint(player);
                _wasAiming = isAiming;
                return;
            }
            
            _wasAiming = isAiming;
            
            // Don't allow tac sprint while aiming
            if (isAiming)
            {
                if (_isTacSprintActive)
                {
                    DisableTacSprint(player);
                }
                return;
            }

            // Check if sprint animation is enabled for current stance
            bool sprintEnabled = CurrentStance switch
            {
                Stance.Stance1 => Plugin._Stance1SprintAnimationEnabled?.Value ?? false,
                Stance.Stance2 => Plugin._Stance2SprintAnimationEnabled?.Value ?? false,
                Stance.Stance3 => Plugin._Stance3SprintAnimationEnabled?.Value ?? false,
                _ => false // Default stance - no special sprint animation
            };

            // If feature disabled for this stance, make sure tac sprint is off
            if (!sprintEnabled)
            {
                if (_isTacSprintActive)
                {
                    DisableTacSprint(player);
                }
                return;
            }

            // Check if we should enable tac sprint
            bool shouldEnableTacSprint = CanDoTacSprint(player);

            // State change: enable tac sprint
            if (shouldEnableTacSprint && !_isTacSprintActive)
            {
                EnableTacSprint(player);
            }
            // State change: disable tac sprint
            else if (!shouldEnableTacSprint && _isTacSprintActive)
            {
                DisableTacSprint(player);
            }
        }

        /// <summary>
        /// Enable tac sprint animation - only called once when entering state
        /// </summary>
        private static void EnableTacSprint(Player player)
        {
            if (player.HandsController is Player.FirearmController fc)
            {
                player.BodyAnimatorCommon.SetFloat(PlayerAnimator.WEAPON_SIZE_MODIFIER_PARAM_HASH, 2f);
                _isTacSprintActive = true;
            }
        }

        /// <summary>
        /// Disable tac sprint animation - reset to actual weapon size
        /// Handles both firearm and non-firearm states (consumables, empty hands, etc.)
        /// </summary>
        private static void DisableTacSprint(Player player)
        {
            // Always mark as inactive first
            _isTacSprintActive = false;
            
            // If holding a firearm, reset to actual weapon size
            if (player.HandsController is Player.FirearmController fc)
            {
                // Get the actual weapon width from its item dimensions
                float actualWeaponSize = (float)fc.Item.CalculateCellSize().X;
                player.BodyAnimatorCommon.SetFloat(PlayerAnimator.WEAPON_SIZE_MODIFIER_PARAM_HASH, actualWeaponSize);
            }
            else
            {
                // Not holding a firearm - reset to neutral (1 = default for non-weapons)
                // This prevents the stuck animation glitch when switching to consumables
                player.BodyAnimatorCommon.SetFloat(PlayerAnimator.WEAPON_SIZE_MODIFIER_PARAM_HASH, 1f);
            }
        }

        /// <summary>
        /// Check if player meets all conditions for tac sprint animation
        /// </summary>
        private static bool CanDoTacSprint(Player player)
        {
            // Must be in stance, sprinting, and holding a firearm
            if (!IsInStance || !player.IsSprintEnabled || !IsHoldingFirearm())
                return false;

            if (!(player.HandsController is Player.FirearmController fc))
                return false;

            var weapon = fc.Item;
            
            // Get weapon stats
            float weaponWeight = weapon.TotalWeight;
            int weaponLength = weapon.CalculateCellSize().X;
            float weaponErgo = weapon.ErgonomicsTotal;
            
            // Check if weapon is bullpup (template ID contains specific bullpup markers)
            // Common bullpup weapons: AUG, P90, MDR, RFB, etc.
            bool isBullpup = weapon.Template.Name.ToLower().Contains("bullpup") ||
                           weapon.Template.ShortNameLocalizationKey.ToString().ToLower().Contains("aug") ||
                           weapon.Template.ShortNameLocalizationKey.ToString().ToLower().Contains("p90") ||
                           weapon.Template.ShortNameLocalizationKey.ToString().ToLower().Contains("mdr") ||
                           weapon.Template.ShortNameLocalizationKey.ToString().ToLower().Contains("rfb");
            
            // Apply weight limit based on bullpup status
            float weightLimit = isBullpup ? Plugin._TacSprintWeightLimitBullpup.Value : Plugin._TacSprintWeightLimit.Value;
            
            // Check all conditions
            bool passesWeightCheck = weaponWeight <= weightLimit;
            bool passesLengthCheck = weaponLength <= Plugin._TacSprintLengthLimit.Value;
            bool passesErgoCheck = weaponErgo > Plugin._TacSprintErgoLimit.Value;
            
            return passesWeightCheck && passesLengthCheck && passesErgoCheck;
        }
    }
}
