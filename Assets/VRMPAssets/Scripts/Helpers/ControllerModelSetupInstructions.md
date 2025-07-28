# Controller Model Setup Instructions

## Quick Setup

1. **Find your XR Origin GameObject** in the scene hierarchy

2. **Add the XRControllerSetup component** to the XR Origin

3. **Assign the controller prefabs:**
   - In the Inspector, look for the "Controller References" section
   - For **Left Controller Model**, drag and drop:
     `Assets/Samples/XR Interaction Toolkit/3.0.7/Starter Assets/Prefabs/Controllers/XR Controller Left.prefab`
   - For **Right Controller Model**, drag and drop:
     `Assets/Samples/XR Interaction Toolkit/3.0.7/Starter Assets/Prefabs/Controllers/XR Controller Right.prefab`

4. **Play the scene** - The controllers should now appear with ray interactors!

## What You Get

The XR Interaction Toolkit controller prefabs include:
- Generic controller model (UniversalController.fbx)
- Pre-configured ray interactor
- Line renderer for the ray
- Proper material setup
- Haptic feedback support

## Troubleshooting

If controllers don't appear:
1. Make sure the prefabs are assigned in the Inspector
2. Check that controller tracking is working (move your Quest controllers)
3. Verify the XR Origin is set up correctly
4. Ensure XR Plugin Management is configured for Android/Quest

## Note
These are generic controller models that work with all VR devices, including Quest 3. While they don't look exactly like Quest 3 controllers, they provide full functionality for ray interaction and button inputs.