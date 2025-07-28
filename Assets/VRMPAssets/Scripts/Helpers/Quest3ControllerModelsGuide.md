# Quest 3 Controller Models Setup Guide

## Component Relationships

- **XRControllerSetup**: Handles ray interactors and interaction setup
- **Quest3HandControllerManager**: Handles visual model switching and cleanup
- Both components work together - use both for complete functionality

## Problem
- Controller models getting stuck on hands when in hand tracking mode
- No controller models appearing when switching to controller mode
- Using generic XR Interaction Toolkit models instead of Quest 3 specific models

## Solution: Quest3HandControllerManager

### Setup Steps:

1. **Add Quest3HandControllerManager to your scene**
   - Add it to your OVRCameraRig or any persistent GameObject
   - It will automatically find the tracking space

2. **Assign Hand/Controller Visuals**
   - **Hand Visuals**: Assign any GameObjects that should only show in hand mode
   - **Controller Visuals**: Assign any GameObjects that should only show in controller mode

3. **Getting Quest 3 Controller Models**

   **Option A: Meta XR SDK (Recommended for Quest 3 models)**
   - You already have Meta XR SDK v71.0.0 installed!
   - Find controller prefabs at:
     - Main prefab: `Library/PackageCache/com.meta.xr.sdk.core/Prefabs/OVRControllerPrefab.prefab`
     - Runtime prefab: `Library/PackageCache/com.meta.xr.sdk.core/Prefabs/OVRRuntimeControllerPrefab.prefab`
   - To use in Unity:
     1. In Project window, show Package contents (click the eye icon)
     2. Navigate to: Packages > Meta XR Core SDK > Prefabs
     3. Drag OVRControllerPrefab into your scene or prefab slots
   - These automatically show the correct controller model:
     - Quest 3: Touch Plus controllers
     - Quest 2: Touch controllers
     - Quest Pro: Touch Pro controllers

   **Option B: Manual Import**
   - Download controller models from Meta's developer resources
   - Import the Touch Plus controller FBX/prefabs
   - Assign to the Left/Right Controller Model Prefab slots

   **Option C: Use XR Interaction Toolkit Models**
   - Leave the controller model slots empty
   - The system will use the existing XR controller setup

4. **Configure XRInputModalityManager** (if not already done)
   - Add XRInputModalityManager to OVRCameraRig
   - Assign Left/Right Controller references
   - Assign Left/Right Hand references

## How It Works

The Quest3HandControllerManager:
- Monitors hand/controller tracking state changes
- Properly hides/shows visuals based on current mode
- Cleans up any "stuck" controller models on hands
- Supports both XRInputModalityManager and manual detection

## Common Issues Fixed

1. **Controller models stuck on hands**: The script actively searches for and disables misplaced controller models
2. **No models in controller mode**: Ensures controller models are properly activated
3. **Switching failures**: Provides both automatic and manual mode switching

## Testing

1. Start with hands visible (no controllers)
2. Pick up controllers - hands should disappear, controllers should appear
3. Put down controllers - controllers should disappear, hands should appear
4. Check that no controller models are visible on hands

## Manual Override

If automatic switching fails, you can call:
```csharp
GetComponent<Quest3HandControllerManager>().ForceHandMode();
// or
GetComponent<Quest3HandControllerManager>().ForceControllerMode();
```