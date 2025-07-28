# SessionStateFeature Setup Guide

## How to Enable SessionStateFeature in Unity

1. **Open XR Plug-in Management Settings**
   - Go to `Edit > Project Settings > XR Plug-in Management`
   - Make sure OpenXR is enabled for Android platform
   - Click on the OpenXR settings (gear icon)

2. **Add the SessionStateFeature**
   - In the OpenXR settings, you'll see a list of OpenXR Features
   - Look for "XR Multiplayer Session State Handler" in the feature list
   - Check the checkbox to enable it
   - If you don't see it, click the "+" button and add it from the available features

3. **Verify Meta Quest Support**
   - Ensure "Meta Quest Support" is also enabled in the OpenXR features list
   - This is required for Quest 3 compatibility

4. **Build Settings**
   - Platform: Android
   - Texture Compression: ASTC
   - Target Architecture: ARM64

## How It Works

The SessionStateFeature automatically:
- Overrides built-in OpenXR lifecycle methods
- `OnSessionExiting()` - Called when user exits the app
- `OnSessionLossPending()` - Called when system is reclaiming resources
- Calls `XRINetworkGameManager.ShutDown()` for proper cleanup
- Logs state transitions for debugging

## Testing

1. Build and deploy to Quest 3
2. Join a multiplayer session
3. Press the Oculus button and select "Quit" from the system menu
4. Check logs to verify proper cleanup:
   ```
   OpenXR Session State Changed: FOCUSED -> EXITING
   OpenXR Session is exiting - user requested app closure (Session: [id])
   ```

## Troubleshooting

- If the feature doesn't appear, ensure you have the latest OpenXR package
- Check that the SessionStateFeature.cs file has proper Unity metadata
- Verify the feature ID matches: `com.xrmultiplayer.openxr.feature.sessionstate`