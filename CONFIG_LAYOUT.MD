# Config Layout Reference

Use this to reorganize config sections and order. Edit and send back.

---

## Section: "Positions"

| Order | Name | Default | Advanced | Type |
|-------|------|---------|----------|------|
| 1 | Enable Camera Position | true | NO | bool |
| 2 | Forward/Backward Offset | 0f | NO | float (-0.5 to 0.5) |
| 3 | Up/Down Offset | 0.02f | NO | float (-0.5 to 0.5) |
| 4 | Sideways Offset | 0f | NO | float (-0.5 to 0.5) |

---

## Section: "Settings"

| Order | Name | Default | Advanced | Type |
|-------|------|---------|----------|------|
| 5 | Enable Stance 1 in Cycle | true | NO | bool |
| 6 | Enable Stance 2 in Cycle | true | NO | bool |
| 7 | Enable Stance 3 in Cycle | true | NO | bool |
| 8 | Stance Toggle Hotkey | V | NO | KeyCode |
| 9 | Enable Mouse Wheel Stance Cycle | false | NO | bool |
| 10 | Mouse Wheel Modifier Key | LeftAlt | NO | KeyCode |
| 11 | Use Only Stances | true | NO | bool |
| 12 | Stance Transition Speed | 1f | NO | float (0.5-20) |
| 13 | ADS Transition Speed | 1f | NO | float (0.5-5) |

---

## Section: "Advanced ADS Transitions"

| Order | Name | Default | Advanced | Type |
|-------|------|---------|----------|------|
| 14 | Advanced ADS Transitions | false | NO | bool |
| 15 | Affect Stance Transition Too | true | YES | bool |
| 16 | Stance Change Sound Volume | 1f | NO | float (0-2) |
| 17 | ADS Shoulder Throw Intensity | 1f | YES | float (0-2) |
| 18 | Stance Shoulder Throw Intensity | 0.75f | YES | float (0-2) |
| 19 | Scale by Weapon Stats | true | YES | bool |
| 20 | Advanced ADS Transition Stat Intensity | 1f | YES | float (0-2) |
| 21 | Advanced Stance Transition Stat Intensity | 1f | YES | float (0.01-2) |
| 22 | Shoulder Throw Forward Amount | 0.02f | YES | float (0-0.3) |
| 23 | Shoulder Throw Up Amount | -0.015f | YES | float (-0.15 to 0.15) |
| 24 | Shoulder Throw Yaw | 6f | YES | float (-15 to 15) |
| 25 | Shoulder Throw Pitch | -3f | YES | float (-15 to 15) |
| 26 | Shoulder Throw Roll | -1.5f | YES | float (-15 to 15) |
| 27 | Shoulder Throw Speed | 2f | YES | float (0.5-5) |
| 28 | Shoulder Settle Speed | 1.5f | YES | float (0.5-5) |
| 29 | Shoulder Throw Duration | 0.15f | YES | float (0.01-0.5) |

---

## Section: "ADS Default Values (Advanced)"

| Order | Name | Default | Advanced | Type |
|-------|------|---------|----------|------|
| 30 | Reset Positions When Aiming | true | YES | bool |
| 31 | ADS Hands Pitch Rotation | 0f | YES | float (-45 to 45) |
| 32 | ADS Hands Yaw Rotation | 0f | YES | float (-45 to 45) |
| 33 | ADS Hands Roll Rotation | 0f | YES | float (-45 to 45) |
| 34 | ADS Hands Forward/Backward Offset | 0f | YES | float (-0.5 to 0.5) |
| 35 | ADS Hands Up/Down Offset | 0f | YES | float (-0.5 to 0.5) |
| 36 | ADS Hands Sideways Offset | 0f | YES | float (-0.5 to 0.5) |

---

## Section: "Default Hands/Arms Positions (Advanced)"

| Order | Name | Default | Advanced | Type |
|-------|------|---------|----------|------|
| 37 | Enable Default Hands/Arms Position | false | YES | bool |
| 38 | Default Hands Forward/Backward Offset | 0f | YES | float (-0.5 to 0.5) |
| 39 | Default Hands Up/Down Offset | 0f | YES | float (-0.5 to 0.5) |
| 40 | Default Hands Sideways Offset | 0f | YES | float (-0.5 to 0.5) |

---

## Section: "Stance 1"

| Order | Name | Default | Advanced | Type |
|-------|------|---------|----------|------|
| 41 | Enable Stance 1 Sprint Animation | true | NO | bool |
| 42 | Stance 1 Hands Pitch (X-Axis) | -15f | NO | float (-45 to 45) |
| 43 | Stance 1 Hands Yaw (Y-Axis) | -15f | NO | float (-45 to 45) |
| 44 | Stance 1 Hands Roll (Z-Axis) | 0f | NO | float (-45 to 45) |
| 45 | Stance 1 Hands Forward/Backward Offset | -0.15f | NO | float (-0.5 to 0.5) |
| 46 | Stance 1 Hands Up/Down Offset | 0f | NO | float (-0.5 to 0.5) |
| 47 | Stance 1 Hands Sideways Offset | 0f | NO | float (-0.5 to 0.5) |

---

## Section: "Tac Sprint Settings (Advanced)"

| Order | Name | Default | Advanced | Type |
|-------|------|---------|----------|------|
| (number after stances) | Tac Sprint Weight Limit | 5.1f | YES | float (1-15) |
|  (number after stances)  +1  | Tac Sprint Weight Limit (Bullpup) | 5.75f | YES | float (1-15) |
|   (number after stances)  +1   | Tac Sprint Length Limit | 6 | YES | int (1-10) |
|   (number after stances)  +1   | Tac Sprint Ergo Limit | 35f | YES | float (0-100) |

---

## Section: "Field of View"

| Order | Name | Default | Advanced | Type |
|-------|------|---------|----------|------|
| +1 | Enable Expanded FOV Range | false | NO | bool |
| +1 | Minimum FOV | 20 | NO | int (1-50) |
| +1 | Maximum FOV | 150 | NO | int (75-170) |

---

## Notes
