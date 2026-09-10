# Installing GorilaChestMod on a dedicated server

This page is for whoever runs the server. Hand them this file and the release zip.

## Why the server needs the mod at all

Two of the three features are decided entirely on a player's own machine, so they
work against a vanilla server: crafting from nearby chests, and the quick stack
button.

The third one does not. A chest inventory is stored inside its ZDO, and whoever
holds that chest in memory decides how large its stacks may be. A vanilla server
loading a chest that contains an oversized stack clamps it back to the normal
limit and destroys the excess the next time it writes that chest out. That is why
the mod loads in `valheim_server.exe` as well.

The server also hands its chest stack settings to every client as it connects, so
the whole session agrees on one limit.

**Every player still needs the mod installed too.** Installing it only on the
server gives nobody the features.

## What you need

- The server files, Steam app `896660`, Valheim dedicated server.
- [BepInExPack_Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
  by denikson, the same pack the players use.
- `GorilaChestMod-<version>-nexus.zip` from the releases of this repository. It is
  laid out as `BepInEx/plugins/GorilaChestMod/GorilaChestMod.dll`, so it extracts
  straight over the server folder.

Server and clients must run the same mod version.

## Windows

1. Stop the server.
2. From the BepInEx pack, copy these into the folder that holds
   `valheim_server.exe`: the `BepInEx` folder, the `doorstop_libs` folder,
   `winhttp.dll`, `doorstop_config.ini` and `.doorstop_version`.
3. Extract the mod zip over that same folder.
4. Start the server the way you normally do, through your
   `start_headless_server.bat`. No extra launch argument is needed, injection
   happens through `winhttp.dll` sitting next to the executable.

## Linux

1. Stop the server.
2. Copy the same files from the pack into the folder that holds
   `valheim_server.x86_64`. On Linux the injection comes from the `.so` files in
   `doorstop_libs`, `winhttp.dll` is ignored.
3. Extract the mod zip over that same folder.
4. Start the server with `start_server_bepinex.sh` from the pack instead of your
   old start script, editing its last line with your server name, port, world and
   password. It exports `DOORSTOP_ENABLED`, points `DOORSTOP_TARGET_ASSEMBLY` at
   `./BepInEx/core/BepInEx.Preloader.dll` and sets `LD_PRELOAD`.
5. Make it executable first: `chmod +x start_server_bepinex.sh`.

## Rented host with a control panel

Most providers, GPortal and Nitrado among them, have a BepInEx switch in the
panel. Turn that on, let it install, then upload only the dll over FTP to
`BepInEx/plugins/GorilaChestMod/GorilaChestMod.dll`. Some panels reset the server
root on game updates, so check the mod is still there after one.

## Configuring it

The first boot writes `BepInEx/config/dev.trentini.gorilachestmod.cfg`. Stop the
server, edit it, start it again.

Only these two are pushed to clients:

| Section | Option | Meaning |
| --- | --- | --- |
| `2 - Chest stacks` | `Enabled` | Whether oversized chest stacks exist at all on this server. |
| `2 - Chest stacks` | `ChestStackSize` | How much one chest slot may hold, 1000 by default. |

Everything else in that file only affects a machine where somebody is actually
playing, so on a headless server it does nothing.

## Checking that it worked

In `BepInEx/LogOutput.log` on the server:

- `GorilaChestMod <version> loaded` right after the chainloader lines.
- `Sent chest stack settings to peer <id>` each time a player joins.

On a player's machine, the log answers with the value the server sent.

## Before you remove the mod

Vanilla trims a stack back to its normal limit when it loads a chest, and the
excess is gone for good. Before uninstalling, either empty the chests, or lower
`ChestStackSize`, let players visit those chests so they get rewritten, and only
then remove it.
