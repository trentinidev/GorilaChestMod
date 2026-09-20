# Download

Built packages, committed here so they can be grabbed straight from the file list
without building the project or opening the releases page.

| File | Use it for |
| --- | --- |
| `GorilaChestMod-2.4.2-thunderstore.zip` | Thunderstore upload, and `Import local mod` in r2modman or Thunderstore Mod Manager. The dll sits at the root. |
| `GorilaChestMod-2.4.2-nexus.zip` | **Dedicated server and manual installs.** It already has the `BepInEx/plugins/GorilaChestMod/` layout, so it extracts straight over the game or server folder. |

Both hold the same dll, only the folder layout differs.

The mod is also on Thunderstore at
[trentinidev/GorilaChestMod](https://thunderstore.io/c/valheim/p/trentinidev/GorilaChestMod/),
and every version is attached to its
[GitHub release](https://github.com/trentinidev/GorilaChestMod/releases).

## Server

A dedicated server needs BepInEx and this mod, and every player needs the same
version. Step by step, Windows, Linux and rented panels:
[../docs/server-install.md](../docs/server-install.md).

r2modman cannot deploy to a remote server. It manages a local profile folder, so
files still have to be uploaded to the server by FTP, SSH or the host's panel.
