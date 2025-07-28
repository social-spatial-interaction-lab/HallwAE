# Quest 3 Controller and Ray Interactor Setup Guide

## Overview
This guide explains how to properly set up Quest 3 controllers with ray interactors for both hand tracking and controller modes in Unity's VR Multiplayer Template.

## Quick Fix Steps

### 1. Add XRControllerSetup Component
1. Find your XR Origin GameObject in the scene
2. Add the `XRControllerSetup` component to it
3. The component will automatically configure:
   - Ray interactors for both controllers
   - Line visuals for the rays
   - UI interaction capabilities

### 2. Add UIInteractionSetup Component
1. Create an empty GameObject named "UI Interaction Manager"
2. Add the `UIInteractionSetup` component to it
3. Enable "Setup All Canvases On Start" option
4. This will ensure all UI elements can be interacted with

### 3. Configure XR Input Modality Manager
1. On your XR Origin GameObject, ensure you have:
   - `XRInputModalityManager` component
   - References set for:
     - Left Hand Controller
     - Right Hand Controller
     - Left Hand (for hand tracking)
     - Right Hand (for hand tracking)

### 4. Controller Model Setup (Optional)
If you want to show Quest 3 controller models:
1. Import Meta XR SDK controller models
2. Assign them to the `XRControllerSetup` component:
   - Left Controller Model
   - Right Controller Model

## Manual Setup (if automatic setup doesn't work)

### For Each Controller (Left/Right):

1. **Find Controller GameObject**
   - XR Origin > Camera Offset > Left Controller (or Right Controller)

2. **Add Ray Interactor**
   ```
   - Right-click on controller GameObject
   - XR > Ray Interactor (Action-based)
   - Name it "Left Ray Interactor" or "Right Ray Interactor"
   ```

3. **Configure Ray Interactor**
   - Enable UI Interaction: ✓
   - Max Raycast Distance: 10
   - Line Type: Straight Line
   - Interaction Layers: Default, UI

4. **Add Line Visual**
   - Add `XRInteractorLineVisual` component
   - Configure colors for valid/invalid/blocked states

5. **Add Line Renderer**
   - Add `LineRenderer` component
   - Set width: 0.01
   - Assign material

## Troubleshooting

### Ray not showing:
- Check if `XRInteractorLineVisual` is enabled
- Verify `LineRenderer` has a material assigned
- Ensure controller tracking is working

### Can't click UI:
- Verify canvas has `TrackedDeviceGraphicRaycaster`
- Check EventSystem has `XRUIInputModule`
- Ensure UI elements are on "UI" layer
- World space canvases need colliders

### Controller model not showing:
- Import Meta XR SDK for Quest controller models
- Assign model prefabs to controller GameObjects
- Check if models are active in hierarchy

### Hand tracking issues:
- Enable Hand Tracking Subsystem in OpenXR settings
- Add Meta Quest Support in XR Plug-in Management
- Ensure `XRInputModalityManager` is configured

## Code Integration

The setup scripts automatically handle:
- Creating ray interactors if missing
- Configuring line visuals
- Setting up UI interaction
- Managing hand/controller switching

To use in your code:
```csharp
// The XRControllerSetup will run automatically on Start
// To manually trigger setup:
XRControllerSetup setup = GetComponent<XRControllerSetup>();
setup.SetupControllers();

// For UI setup:
UIInteractionSetup.ConfigureCanvasForXR(yourCanvas);
```

## Notes
- Quest 3 uses OpenXR, not the legacy OVR system
- Controller models may need to be imported separately
- Hand tracking and controllers cannot be used simultaneously
- The XRInputModalityManager handles switching between modes