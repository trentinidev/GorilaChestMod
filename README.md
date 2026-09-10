# CraftFromChests

A client side **Valheim** mod that lets you craft, upgrade and build using the
items inside nearby chests, without hauling anything into your inventory first.

The recipe list, the have/need numbers and the craft button all count the
contents of the chests around you. When you craft, the items in your own
inventory are spent first, then the chests, closest one first.

## Game version this was built against

| Item | Value |
| --- | --- |
| Game version | 1.0.7 |
| Steam build | 25185596 |
| Engine | Unity 6000.0.75f1, Mono backend |
| Game code | `valheim_Data/Managed/assembly_valheim.dll` |

Because the backend is Mono and not IL2CPP, the usual modding path still applies:
BepInEx 5 plus Harmony, patching in memory. No game file is modified.

## Manual install

1. Install **BepInEx 5** for Valheim in the game folder, the
   `denikson-BepInExPack_Valheim` package. Launch the game once so it creates its
   folders.
2. Copy `CraftFromChests.dll` into `Valheim/BepInEx/plugins/CraftFromChests/`, or
   extract the `-nexus.zip` release over the game folder.
3. Start the game. `Valheim/BepInEx/LogOutput.log` should contain a line reading
   `CraftFromChests 1.0.0 loaded`.

`dotnet build` already copies the dll into `BepInEx/plugins/CraftFromChests/`
when that folder exists.

## Installing through r2modman

r2modman and Thunderstore Mod Manager keep BepInEx inside their own profile, so
**do not mix them with a manual install in the game root**. If you have one,
delete `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version`, the
`doorstop_libs` folder and the `BepInEx` folder from `Valheim/` before you
migrate. Two BepInEx installs in one game is the classic cause of a mod loading
twice or not loading at all.

Three ways to manage this mod there, quickest first:

| Way | How | What you get, what you give up |
| --- | --- | --- |
| Loose dll | `Import local mod`, pick `CraftFromChests.dll` | Works right away. You type the name and version by hand, no icon and no automatic BepInEx dependency. |
| Local zip | `Import local mod`, pick `dist/CraftFromChests-<version>-thunderstore.zip` | Icon, version, description and BepInEx pulled in as a dependency. Stays private, nothing is published. |
| Thunderstore | Publish that same zip on thunderstore.io | Install and update straight from the r2modman browser, for you and for anyone else. |

Nexus Mods is not part of that list. r2modman does not install mods from Nexus,
and the Nexus manager is Vortex. Publishing on both is common, but a Nexus
download is installed manually or through Vortex.

Under r2modman the config file lives inside the profile, not in
`Valheim/BepInEx/config`. The manager has its own config editor.

## Configuration

`BepInEx/config/dev.trentini.craftfromchests.cfg` is written on first launch and
can be edited with the game closed.

| Option | Default | What it does |
| --- | --- | --- |
| `Enabled` | `true` | Master switch, turns the mod off without removing it. |
| `Range` | `20` | Radius in meters that chests are pulled from. |
| `UseForBuilding` | `true` | Also pay hammer, hoe and cultivator costs from chests. |
| `IncludeVehicleContainers` | `true` | Include containers on carts and ships. |
| `Verbose` | `false` | Log every chest withdrawal and every redirected call. |

## Which chests count

A chest is only used if it passes the same rules the game applies when you try
to open it:

- it is inside the configured radius;
- privacy is `Public`, or `Private` with you as the creator of the piece, so
  group chests stay out exactly like in vanilla;
- if the chest checks for a guard stone, you need access to that ward;
- nobody else has it open, a chest in use by another player is skipped.

Before taking anything the mod claims network ownership of the chest through
`ZNetView.ClaimOwnership`, which is what authorises writing the inventory back
into the ZDO. That is the same mechanism the game uses for "take all", so this
works on a dedicated server with no server side mod. It is a **client side only**
mod: players without it are unaffected.

## How it works

The game methods read the player inventory straight from the `m_inventory`
field, with no extension point anywhere. Rather than injecting fake items into
your backpack, the mod uses transpilers to rewrite only the inventory calls
inside seven game methods. An instance call already carries `this` as its first
stack argument, so a static method whose first parameter is the `Inventory` is a
drop-in replacement and the rest of the method body is left untouched.

Calls redirected into `InventoryBridge`:

| Game method | Call swapped | Why |
| --- | --- | --- |
| `Player.HaveRequirementItems` | `CountItems` | unlocks the recipe and the craft button |
| `Player.GetFirstRequiredItem` | `CountItems`, `GetItem` | single ingredient recipes such as meads and feasts |
| `Player.ConsumeResources` | `RemoveItem` | pays for a craft and for a placed piece |
| `Player.HaveRequirements(Piece)` | `HaveItem`, `CountItems` | unlocks the building piece |
| `InventoryGui.DoCrafting` | `RemoveItem` | pays the single ingredient |
| `InventoryGui.SetupRequirement` | `CountItems` | the have/need numbers in the UI |

Three support patches on top of that: `Container.Awake` registers every chest
that spawns, so no `FindObjectsOfType` sweep is ever needed, while
`Player.UpdatePlacement` and `Hud.SetupPieceInfo` mark the building context so
the `UseForBuilding` option can be honoured.

If a future game update removes one of those call sites, the transpiler writes a
`LogError` into the BepInEx log naming the call it could not find, instead of
failing silently.

## Build

```
dotnet build -c Release
```

If Valheim lives somewhere else:

```
dotnet build -c Release -p:ValheimDir="D:\Steam\steamapps\common\Valheim"
```

Game references come straight from `valheim_Data/Managed`, and BepInEx plus
Harmony come from the BepInEx NuGet feed, already configured in `nuget.config`.

## Packaging and publishing

```
powershell -ExecutionPolicy Bypass -File packaging\build-package.ps1
```

Writes both release zips into `dist/`:

| Zip | Contents | Where it goes |
| --- | --- | --- |
| `-thunderstore.zip` | `manifest.json`, `icon.png`, readme, changelog and the dll at the root | Thunderstore, or `Import local mod` in r2modman |
| `-nexus.zip` | `BepInEx/plugins/CraftFromChests/CraftFromChests.dll` plus readme and changelog | Nexus Mods, the player extracts it over the game folder |

The script validates what usually gets an upload rejected before it builds the
zip: an `x.y.z` version, an icon that is exactly 256 by 256, a manifest name made
only of letters, digits and underscores, and a description within the 250
character limit. It also checks that the compiled dll carries the same version as
the csproj.

The version lives **only** in `<Version>` in `CraftFromChests.csproj`. The
`GenerateBuildInfo` target turns it into the constant the `BepInPlugin` attribute
uses, and the packaging script reads the same property to fill in
`manifest.json`. To ship a new version: bump the csproj, write the changelog, run
the script.

The icon comes from `packaging/make-icon.ps1`, which draws the PNG in code.
Change the palette at the top of that file and run it again.

`docs/nexus-description.bbcode` holds the Nexus page description in BBCode,
ready to paste.

## Compatibility check after a game update

`tools/PatchCheck` opens `assembly_valheim.dll` with Mono.Cecil and asserts that
the nine target methods still exist with the same signature, that every inventory
call the transpilers look for is still there, and that the `InventoryBridge`
signatures still line up with the game's.

```
cd tools/PatchCheck
dotnet run
```

Expected output: `ALL CHECKS PASSED`. Run it first whenever Valheim updates,
before launching the game.

## Known limitations

- Repairing costs no materials in Valheim, so there is nothing to do there.
- Smelters, kilns and other stations that take ore or wood through direct
  interaction are not fed from chests. The mod covers crafting, upgrading and
  building.
- A chest another player has open at that moment is skipped until they close it.
- Ownership of a chest is claimed without asking the current owner over RPC, the
  way the game does when you open one. The local copy is refreshed once a second
  by the game's own `CheckForChanges`, so there is a short window in which two
  players touching the same chest at the same instant can have the last writer
  win.
