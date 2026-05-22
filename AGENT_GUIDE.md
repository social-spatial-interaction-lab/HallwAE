# HallwAE — AI Agent Orientation Guide

> **Provenance:** AI-generated (Claude Sonnet 4.6 via Claude Code)
> **Last revised:** 2026-05-20
> **Edit policy:** Future edits to this file should note the author/tool and date at the top of this block. Human edits are welcome; if AI-assisted, say so and with what model.

---

## What this project is

HallwAE is an **AR multiplayer app** for Meta Quest headsets that replicates the spontaneous hallway-conversation experience — but remotely. Two or more people wearing headsets see each other's avatars overlaid on their real environment, enabling lightweight social presence without a fully virtual world.

It is forked from Unity's official **VR Multiplayer Template** and adapted for AR (passthrough) and the specific social use case of the Social Spatial Interaction Lab.

---

## Unity environment

| Item | Value |
|---|---|
| **Required Unity version** | `6000.0.25f1` (Unity 6 LTS) |
| **Render pipeline** | Universal Render Pipeline (URP) 17.0.3 |
| **Target platform** | Android / Meta Quest (AR passthrough) |
| **Active branch** | `mark-v03` |

The project requires Unity 6. Opening it in the installed 2022 LTS editor will likely break packages.

---

## Repository layout

```
Assets/
  Editor/
    BuildScript.cs              ← headless APK build entry point (see below)
    EditorUtils/                ← mesh saver, text converter, build ID incrementer
    InEditorTutorials/          ← tutorial images and callbacks
  Scenes/
    BasicSceneAR.unity          ← PRIMARY scene (AR, multiplayer) — built into APK
    BasicScene.unity            ← VR version of the same scene
    SampleScene.unity           ← scratch / reference
  VRMPAssets/
    Scripts/
      Constants.cs              ← coordinator URL and other global constants
      Network/
        NetworkManagers/        ← core networking layer (see below)
        NetworkPlayer/          ← avatar IK, visuals, hand pose replication
        NetworkInteractions/    ← networked grab/physics/socket
        NetworkUtilityComponents/ ← client transform, triggers, networked UI
      Player/                   ← local player: avatar, appearance, HUD, name tag
      Gameplay/                 ← drawing (pen/trail), message board, object dispenser,
      |                           target practice, anti-gravity zone, room music
      MiniGames/                ← Climber, Slingshot, Whack-a-Pig mini-games
      Helpers/                  ← billboards, poolers, key-lock system, XR knob/lever,
                                  snap-to-height, tooltips, utils
      UI/                       ← lobby list, player list, offline menu, popout, tooltip
  Samples/                      ← Unity package samples (XR Hands, XRI Toolkit, etc.)
    XR Hands/1.5.0/             ← hand tracking visualizer
    XR Interaction Toolkit/
      3.0.6/                    ← hands demo, spatial keyboard, XR device simulator
      3.0.7/                    ← starter assets (controller animator, teleport, etc.)
Packages/manifest.json          ← all package dependencies (see key packages below)
ProjectSettings/                ← Unity project settings (do not hand-edit)
Progress.md                     ← informal dev log / known issues
README.md                       ← project overview (GitHub-facing)
RuntimeActionBindings.json      ← XR input action bindings
```

---

## Key source files

### Networking layer (`Assets/VRMPAssets/Scripts/Network/`)

| File | Role |
|---|---|
| `NetworkManagers/XRINetworkGameManager.cs` | Top-level singleton managing connection state machine (`None → Authenticating → Authenticated → Connecting → Connected`). Max 20 players. |
| `NetworkManagers/LobbyManager.cs` | Bridges Unity Lobby, Relay, and UnityTransport. Creates/joins allocations, issues heartbeats, stores join codes in lobby data. Talks to the coordinator API at `hallwae-coordinator.netlify.app/api/lobbies`. |
| `NetworkManagers/AuthenticationManager.cs` | Handles Unity Authentication sign-in before lobbies can be created. |
| `NetworkManagers/VoiceChatManager.cs` | Vivox voice chat integration. |
| `NetworkManagers/NetworkManagerVRMultiplayer.cs` | NGO NetworkManager subclass. |
| `NetworkPlayer/XRAvatarIK.cs` | Drives remote avatar head/hand transforms from network data. |
| `NetworkPlayer/XRAvatarVisuals.cs` | Updates avatar mesh/material from network-replicated state. |
| `NetworkPlayer/XRHandPoseReplicator.cs` | Replicates XR hand skeletal poses across the network. |
| `NetworkPlayer/XRINetworkPlayer.cs` | Per-player networked state (name, color, connection). |

### Constants (`Assets/VRMPAssets/Scripts/Constants.cs`)

```csharp
k_CoordinatorUrl = "https://hallwae-coordinator.netlify.app/api"
// A local dev server alternative is commented out: http://localhost:3000/api
```

### Player / UI

| File | Role |
|---|---|
| `Player/PlayerAppearanceMenu.cs` | In-headset menu for choosing avatar color/name. |
| `Player/PlayerNameTag.cs` | Floating name tag above each remote avatar. |
| `UI/LobbyList/LobbyUI.cs` | Lists available lobbies and handles create/join. |
| `UI/OfflineMenu.cs` | Shown when not connected to a session. |
| `MenuButton.cs` (Assets root) | Top-level menu button MonoBehaviour. |

---

## Build script (headless / agentic APK builds)

`Assets/Editor/BuildScript.cs` is the entry point for **CI and agentic headless builds**. It is not a game script — it lives in `Assets/Editor/` and is only compiled into the Unity Editor.

```csharp
BuildScript.BuildAndroid()
```

- Builds the scene `Assets/Scenes/BasicSceneAR.unity` for Android.
- Outputs to `/tmp/hallwae-build/HallwAE.apk`.
- Exits with code `1` on failure (suitable for CI pipelines).

**Invoking from the command line (headless):**

```bash
/path/to/Unity \
  -batchmode -nographics -quit \
  -projectPath /home/mark/work/hallwae \
  -executeMethod BuildScript.BuildAndroid \
  -logFile /tmp/hallwae-build.log
```

Unity 6000.0.25f1 must be used. Find the binary at:
`~/Unity/Hub/Editor/6000.0.25f1/Editor/Unity` (install via Unity Hub if not present; the installed 2022 editor at `~/Unity/Hub/Editor/2022.3.22f1/` is the wrong version).

The `BuildIncrementer` (`Editor/EditorUtils/BuildIncrementer/BuildIdIncrementer.cs`) auto-increments the build ID on each build.

---

## Key packages

| Package | Version | Purpose |
|---|---|---|
| `com.meta.xr.sdk.interaction` | 71.0.0 | Meta XR interaction framework |
| `com.meta.xr.mrutilitykit` | 71.0.0 | Mixed Reality scene understanding |
| `com.unity.netcode.gameobjects` | 1.10.0 | NGO — core multiplayer sync |
| `com.unity.services.lobby` | 1.2.2 | Unity Lobby service |
| `com.unity.services.relay` | 1.1.1 | Unity Relay (NAT traversal) |
| `com.unity.services.vivox` | 16.5.3 | Voice chat |
| `com.unity.xr.hands` | 1.5.0 | Hand tracking |
| `com.unity.xr.interaction.toolkit` | 3.0.7 | XR Interaction Toolkit |
| `com.unity.xr.openxr` | 1.13.1 | OpenXR backend |
| `com.unity.render-pipelines.universal` | 17.0.3 | URP rendering |
| `com.unity.transport` | 2.4.0 | Low-level network transport |

---

## Networking model

```
Local player
  └─ XRINetworkGameManager (singleton)
       ├─ AuthenticationManager  (Unity Auth sign-in)
       ├─ LobbyManager           (lobby list, create, join, heartbeat)
       │    └─ UnityTransport    (Relay-backed UDP transport)
       └─ NetworkManagerVRMultiplayer (NGO host/client)
            └─ XRINetworkPlayer (per-client: avatar state, name, color)
                 ├─ XRAvatarIK
                 ├─ XRAvatarVisuals
                 └─ XRHandPoseReplicator
```

Session flow:
1. `AuthenticationManager` signs in anonymously via Unity Authentication.
2. `LobbyManager` fetches/creates a lobby; coordinator API at Netlify is consulted.
3. Host creates a Relay allocation → gets join code → stores in lobby data.
4. Clients pull join code from lobby → join Relay allocation → NGO `StartClient()`.
5. Each player spawns a `XRINetworkPlayer` prefab; avatar sync runs per-frame.

---

## Known issues / watch points

- **Monkey mask culling bug:** Remove the "Mirror" layer from `cullingMask` on `centerEyeAnchor`. See `Progress.md`.
- **NGO 1.9.1 byte serialization bug:** Worked around with `#if USE_FORCED_BYTE_SERIALIZATION` guard in `XRINetworkGameManager.cs`. Remove when fixed upstream.
- **Coordinator URL:** Production points to Netlify (`hallwae-coordinator.netlify.app`). A local dev server alternative is commented out in `Constants.cs`.
- **Git LFS:** Binary assets use LFS. Always clone with `git lfs clone` or pull LFS objects separately; a plain `git clone` will leave placeholder files.
