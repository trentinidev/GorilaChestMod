# Changelog

## 2.1.0

Quick stack and chest slot polish, from playing the 2.0.0 build.

- The quick stack button moved out of the item grid. It now sits under the weight
  readout, to the side of the inventory, where it cannot cover a slot, and it is
  wide enough for its caption.
- The button is called **Stack to nearby chests**.
- Amount labels shrink as the numbers grow, so a slot holding 100000 of something
  still reads cleanly instead of spilling over its neighbours. Turn it off with
  `ShrinkStackText`.
- The hotkey works with the inventory closed, so you can stand next to your
  chests and press it. `HotkeyNeedsInventory` brings the old behaviour back. It
  stays quiet while a menu, the console, the chat or a text field has focus.

## 2.0.0

Renamed from CraftFromChests to **GorilaChestMod**, and grew from one feature to
three. The plugin id changed with the name, so the old
`dev.trentini.craftfromchests.cfg` is not read any more, copy your settings over
to `dev.trentini.gorilachestmod.cfg`.

- **New: oversized chest stacks.** Items stack up to a configurable limit,
  1000 by default, while they sit in a chest, so a slot no longer caps at the
  vanilla 50. Player inventories keep the vanilla limits. Stacks are split back
  into vanilla sized ones the moment they leave a chest, and a destroyed chest
  drops normal stacks.
- **New: quick stack.** A button in the inventory screen, plus an optional
  hotkey, that pushes matching items from your inventory into the chests in
  range. An item only moves into a chest that already holds that item, and the
  hotbar is left alone by default.
- **New: runs on a dedicated server.** The plugin no longer restricts itself to
  the game client. A server holding a chest would otherwise clamp oversized
  stacks back to the vanilla limit, so it needs the mod too. The server also
  hands its chest stack settings to every client that connects.
- Craft from chests is unchanged, and still counts nearby chests for crafting,
  upgrading and building.
- The compatibility checker in `tools/PatchCheck` now covers all three features.

## 1.0.0

First release as CraftFromChests, built against Valheim 1.0.7.

- Craft and upgrade at a workbench using the items inside chests within the
  configured radius.
- Hammer, hoe and cultivator building costs are paid from those chests too, with
  an option to turn that part off.
- The recipe list, the have/need numbers and the craft button all count the
  contents of nearby chests.
- Items in your own inventory are spent first, then the chests, closest one
  first.
- Honours the game's own access rules: radius, chest privacy, guard stones and
  chests another player has open.
