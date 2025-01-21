# HallwAE

*What if you could have those spontaneous hallway conversations with colleagues or friends- but remotely?*

AR can do this. It has an outside shot of being a socially acceptable use of AR within settings where people do this.

*Based on the official sample for VR Multiplarer in Unity*

You can checkout the tutorial in the project, or the quick start guide to know more about this sample:
[VR Multiplayer Template Quick Start Guide | VR Multiplayer | 2.0.4](https://docs.unity3d.com/Packages/com.unity.template.vr-multiplayer@2.0/manual/index.html) 

## Git LFS
Since a game project will have many binary files, using git lfs is a common solution but require different steps to clone this repo.

First, make sure to install git lfs on your computer: https://git-lfs.com

Second, Use the command `git lfs clone <repository-url>` instead of the standard `git clone <repository-url>`. The git lfs clone command is optimized for repositories using LFS and can be significantly faster

The file types for lfs is specified in .gitattributes, if you modify this file, you need to re-add to apply this update:
```
git rm --cached -r .
git add -A
```

## References
- Here is the guide to initialize a unity + git project: https://github.com/orgs/community/discussions/56071
- Unity Lobby Service: https://docs.unity3d.com/Packages/com.unity.services.lobby@1.2/api/Unity.Services.Lobbies.Models.Lobby.html
