# GorilaChestMod

Chest quality of life for **Valheim**, in three parts:

1. **Craft from chests.** Craft, upgrade and build using the items inside the
   chests around you, without hauling anything into your inventory first.
2. **Oversized chest stacks.** A chest slot holds far more than the vanilla
   limit, 1000 by default, so a chest full of wood is one slot instead of twenty.
   Your own inventory keeps the vanilla limits.
3. **Quick stack.** One button, or a hotkey, that pushes matching items from your
   inventory into the chests in range.

Runs on the client and on a dedicated server.

## Game version this was built against

| Item | Value |
| --- | --- |
| Game version | 1.0.7 |
| Steam build | 25185596 |
| Engine | Unity 6000.0.75f1, Mono backend |
| Game code | `valheim_Data/Managed/assembly_valheim.dll` |

Because the backend is Mono and not IL2CPP, the usual modding path still applies:
BepInEx 5 plus Harmony, patching in memory. No game file is modified.

## Install

1. Install **BepInEx 5** for Valheim, the `denikson-BepInExPack_Valheim` package.
   Launch the game once so it creates its folders.
2. Copy `GorilaChestMod.dll` into `Valheim/BepInEx/plugins/GorilaChestMod/`, or
   extract the `-nexus.zip` release over the game folder.
3. Start the game. `BepInEx/LogOutput.log` should contain a line reading
   `GorilaChestMod 2.1.0 loaded`.

With r2modman or Thunderstore Mod Manager, use `Import local mod` and pick the
`-thunderstore.zip` release. Do not mix a mod manager with a manual BepInEx
install in the game root, that is the classic cause of a mod loading twice or
not loading at all.

### On a dedicated server

Full step by step for whoever runs the server, Windows, Linux and rented panels:
**[docs/server-install.md](docs/server-install.md)**.

The short version is the same as above, into the server's `BepInEx/plugins`, and
**every player needs it as well**. The server half exists for two reasons:

- A server that loads a chest into memory decides how large its stacks may be. A
  vanilla server would clamp an oversized stack back to the vanilla limit and
  destroy the excess the next time it wrote that chest out.
- The server hands its chest stack settings to every client as they connect, so
  the whole session agrees on the limit.

Craft from chests and quick stack are decided entirely on the player's own
machine, so those two still work fine against a vanilla server.

## Configuration

`BepInEx/config/dev.trentini.gorilachestmod.cfg` is written on first launch and
can be edited with the game closed.

| Section | Option | Default | What it does |
| --- | --- | --- | --- |
| Craft from chests | `Enabled` | `true` | Count nearby chests when crafting, upgrading and building. |
| Craft from chests | `Range` | `20` | Radius in meters. Quick stack uses the same radius. |
| Craft from chests | `UseForBuilding` | `true` | Also pay hammer, hoe and cultivator costs from chests. |
| Craft from chests | `IncludeVehicleContainers` | `true` | Include containers on carts and ships. |
| Chest stacks | `Enabled` | `true` | Allow oversized stacks inside chests. |
| Chest stacks | `ChestStackSize` | `1000` | How much one chest slot may hold. The server's value wins. |
| Chest stacks | `ShrinkStackText` | `true` | Shrink a slot label when the numbers get long, so 100000/100000 fits. |
| Quick stack | `Enabled` | `true` | Enable the button and the hotkey. |
| Quick stack | `SkipHotbar` | `true` | Leave the hotbar row alone. |
| Quick stack | `ShowButton` | `true` | Show the button in the inventory screen. |
| Quick stack | `ButtonOffset` | `0, 0` | Nudge the button in pixels from its spot under the weight readout. |
| Quick stack | `ButtonLabel` | `Stack to nearby chests` | Caption on the button. |
| Quick stack | `Hotkey` | `None` | Hotkey for quick stacking, inventory open or closed. |
| Quick stack | `HotkeyNeedsInventory` | `false` | Require the inventory to be open for the hotkey. |
| Debug | `Verbose` | `false` | Log every withdrawal, move and patched call. |

When you are connected to a server that has the mod, that server's
`Enabled` and `ChestStackSize` under Chest stacks replace your local values for
as long as you are connected. Everything else stays personal.

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
into the ZDO. That is the same mechanism the game uses for "take all".

## How it works

### Craft from chests

The game methods read the player inventory straight from the `m_inventory`
field, with no extension point anywhere. Rather than injecting fake items into
your backpack, the mod uses transpilers to rewrite only the inventory calls
inside these methods. An instance call already carries `this` as its first stack
argument, so a static method whose first parameter is the `Inventory` is a
drop-in replacement and the rest of the method body is left untouched.

| Game method | Call swapped | Why |
| --- | --- | --- |
| `Player.HaveRequirementItems` | `CountItems` | unlocks the recipe and the craft button |
| `Player.GetFirstRequiredItem` | `CountItems`, `GetItem` | single ingredient recipes such as meads and feasts |
| `Player.ConsumeResources` | `RemoveItem` | pays for a craft and for a placed piece |
| `Player.HaveRequirements(Piece)` | `HaveItem`, `CountItems` | unlocks the building piece |
| `InventoryGui.DoCrafting` | `RemoveItem` | pays the single ingredient |
| `InventoryGui.SetupRequirement` | `CountItems` | the have/need numbers in the UI |

`Container.Awake` registers every chest that spawns, so no `FindObjectsOfType`
sweep is ever needed, and `Player.UpdatePlacement` and `Hud.SetupPieceInfo` mark
the building context so `UseForBuilding` can be honoured.

### Oversized chest stacks

The stack limit is the plain field `ItemData.m_shared.m_maxStackSize`, read in a
handful of places inside `Inventory` and `InventoryGrid`. A transpiler turns each
of those field reads into a call that also receives the inventory being worked
on, so the same item type answers 1000 in a chest and the vanilla 50 in a
backpack. The list of methods is discovered from the game's own IL at startup
rather than hard coded, and written to the log.

That single change covers saving and loading too: the overload of
`Inventory.AddItem` that rebuilds an inventory from its saved bytes clamps by the
same field, which is why a vanilla server or a vanilla client would otherwise
trim the excess away.

Three guards keep oversized stacks from leaking out of chests:

- `Inventory.AddItem(ItemData)` splits a stack that is too large for its
  destination across several slots, so shift clicking 1000 wood out of a chest
  gives you twenty vanilla stacks.
- The slot targeted `Inventory.AddItem` moves only a vanilla sized portion and
  reports the move as partial, so the rest stays in the chest.
- `ItemDrop.DropItem` splits oversized stacks into several drops, so a destroyed
  chest scatters normal stacks instead of one impossible pile.

### Quick stack

`InventoryGui.Awake` clones the game's own take all button, so it inherits the
vanilla look, font and sounds, and anchors it under the weight readout, outside
the item grid. It is shown while the inventory is open and there is a chest in
range. The hotkey works with the inventory closed too, and holds off while a
menu, the console, the chat or a text field has focus. Items move only into chests that already
hold that item, equipped and quest items are skipped, and the hotbar is skipped
unless you say otherwise.

### Server side

The plugin loads in `valheim_server.exe` as well as `valheim.exe`. On a server
there is no local player, so craft from chests and quick stack simply never fire,
while the stack limit patches do their job on every chest the server touches.

Config sync is two routed RPCs of the game's own network layer: a client asks
once it is connected, the server answers with its chest stack settings, and a
server never takes those values from a client.

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
The build copies the dll into the r2modman profile when that folder exists, and
otherwise into the game's own plugins folder.

## Packaging and publishing

```
powershell -ExecutionPolicy Bypass -File packaging\build-package.ps1
```

Writes both release zips into `dist/`:

| Zip | Contents | Where it goes |
| --- | --- | --- |
| `-thunderstore.zip` | `manifest.json`, `icon.png`, readme, changelog and the dll at the root | Thunderstore, or `Import local mod` in r2modman |
| `-nexus.zip` | `BepInEx/plugins/GorilaChestMod/GorilaChestMod.dll` plus readme and changelog | Nexus Mods, extracted over the game folder |

The script validates what usually gets an upload rejected: an `x.y.z` version, an
icon that is exactly 256 by 256, a manifest name made only of letters, digits and
underscores, and a description within the 250 character limit. It also checks
that the compiled dll carries the same version as the csproj.

The version lives **only** in `<Version>` in `GorilaChestMod.csproj`. The
`GenerateBuildInfo` target turns it into the constant the `BepInPlugin` attribute
uses, and the packaging script reads the same property for `manifest.json`.

The icon comes from `packaging/make-icon.ps1`, which draws the PNG in code.
`docs/nexus-description.bbcode` holds the Nexus page description, ready to paste.

## Compatibility check after a game update

`tools/PatchCheck` opens `assembly_valheim.dll` with Mono.Cecil and asserts that
every method the mod patches still exists with the same signature, that every
call site the transpilers rewrite is still there, that the stack limit is still
read where it needs to be, and that the mod side signatures still line up.

```
cd tools/PatchCheck
dotnet run
```

Expected output: `ALL CHECKS PASSED`. Run it first whenever Valheim updates,
before launching the game.

## Known limitations

- **Removing the mod leaves oversized stacks behind.** Vanilla clamps a stack
  back to its normal limit when it loads a chest, and the excess is gone. Empty
  your chests, or lower `ChestStackSize` and let the chests settle, before you
  uninstall. The same applies to a player without the mod opening a chest that
  holds oversized stacks.
- Repairing costs no materials in Valheim, so there is nothing to do there.
- Smelters, kilns and other stations that take ore or wood through direct
  interaction are not fed from chests. Crafting, upgrading and building are.
- The quick stack button is mouse driven, there is no gamepad binding for it yet.
- A chest another player has open at that moment is skipped until they close it.
