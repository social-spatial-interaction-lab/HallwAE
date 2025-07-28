# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

HallwAE is a Unity-based VR multiplayer application that enables spontaneous hallway conversations in AR/VR. Built on Unity's official VR Multiplayer Template, it uses Unity Netcode for GameObjects and supports Meta Quest devices.

## Architecture

### Core Technologies
- Unity 6000.0.31f1
- Unity Netcode for GameObjects 1.10.0
- XR Interaction Toolkit 3.0.7
- Unity Lobby & Relay Services
- Vivox voice chat
- Universal Render Pipeline (URP)

### Key Systems

1. **Networking Architecture**
   - `NetworkManagerVRMultiplayer` - Custom network manager handling connection lifecycle
   - `XRINetworkPlayer` - Network player prefab with VR avatar representation
   - Unity Services integration (Authentication, Lobby, Relay, Vivox)

2. **Player Systems**
   - XR avatar with IK systems for body tracking
   - Hand pose replication via OpenXR
   - Player appearance customization
   - Name tags and voice chat indicators

3. **Interaction Systems**
   - Network-aware XR interactables
   - Object dispensers and spawners
   - Mini-games framework (Climbing, Slingshot, Whack-A-Pig)
   - Drawing/pen system
   - Message board system

4. **Main Code Locations**
   - Scripts: `Assets/VRMPAssets/Scripts/`
   - Network managers: `Assets/VRMPAssets/Scripts/Network/NetworkManagers/`
   - Player code: `Assets/VRMPAssets/Scripts/Network/NetworkPlayer/`
   - Gameplay features: `Assets/VRMPAssets/Scripts/Gameplay/`
   - UI systems: `Assets/VRMPAssets/Scripts/UI/`

## Common Development Commands

### Building
- Unity build through File > Build Settings
- Target platform: Android (Quest 3)
- Build profile location: `Assets/Settings/Build Profiles/Quest 3 Build.asset`

### Testing
- No automated test framework currently configured
- Manual testing required in Unity Editor or on device
- Use ParrelSync for local multiplayer testing (if installed)

### Running
- In Editor: Play button with XR Device Simulator
- On Device: Build and deploy APK to Quest headset
- Multiplayer testing: Use Unity's Multiplayer Play Mode or ParrelSync

## Important Notes

- Git LFS is required - use `git lfs clone` for initial setup
- The project uses assembly definitions (VRMP.asmdef)
- Network objects must have NetworkObject component
- All networked interactions require proper RPC setup
- Voice chat requires Vivox service configuration
- Unity 6 deprecations: Use `FindFirstObjectByType` instead of `FindObjectOfType`

## Quest 3 Specific Setup

### XR Configuration
- Uses OVRCameraRig (tagged as "PlayerRig") instead of XROrigin
- Hand tracking requires XRInputModalityManager component on OVRCameraRig for automatic hand/controller switching
- Meta XR SDK v71.0.0 includes Quest 3 controller models at `Library/PackageCache/com.meta.xr.sdk.core/Prefabs/`

### Hand/Controller Switching
- Quest3HandControllerManager handles visual switching between hands and controllers
- Prevents controller models from getting stuck on hands during mode transitions
- Configure with Meta XR SDK prefabs:
  - Hand models: OVRHandPrefab or OVRCustomHandPrefab_L/R
  - Controller models: OVRControllerPrefab (runtime instantiation)

### App Exit Detection
- SessionStateFeature (custom OpenXR feature) detects app quit on Quest 3
- Quest 3 uses XR_SESSION_STATE_STOPPING instead of EXITING state
- Triggers cleanup in XRINetworkGameManager.ShutDown() for proper room exit notification

### Known Issues
- Controller models may stick to hands when switching modes - use Quest3HandControllerManager to fix
- Manual hand tracking detection needed if XRInputModalityManager is not present
- Quest 3 doesn't call OnApplicationPause/Focus reliably for app exit detection