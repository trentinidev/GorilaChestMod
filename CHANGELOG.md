# Changelog

## 2.3.0

- **New: favorites.** Hold Alt and left click a stack in your inventory to mark
  it. A marked stack is left alone by quick stack and by the chest's own stack
  button, so your food, arrows and tools stop being swept into storage. Click
  again to unmark.
- A small gold star sits on a marked slot, and a line in the corner confirms each
  change. Both can be turned off, and the modifier key is configurable.
- The mark lives in the item's own saved data, so it survives logging out,
  moving the stack, and splitting it. Merging two stacks keeps the mark if
  either side had it.

## 2.2.2

- The button clears the open chest panel: it sits further right, and its own
  canvas now sorts high enough that nothing draws over it.
- The caption is broken one word per line and centred, so "Stack to Chests"
  reads as three stacked lines inside the square.

## 2.2.1

- New icon: the chest artwork, with the mod name across the top band and the
  author across the bottom one. Composed by packaging/make-icon.ps1 from
  packaging/icon-source.png, so re-running it picks up either change. The old
  drawn badge is still there as make-icon-badge.ps1.
- The hotkey defaults to **J**. G turned out to open the hotbar radial, which the
  key binding screen does not list.
- The button is back under the weight readout where it belongs. Positioning it
  by world coordinates put it in the top corner instead.
- It carries its own canvas at a higher sorting order now, which is what keeps
  an open chest from drawing over it, rather than relying on hierarchy order.

## 2.2.0

- The quick stack hotkey defaults to **G**. V, the old suggestion, is taken by
  the game for auto pickup.
- Chest slots default to **100000** per stack, up from 1000.
- The button became a compact square with the caption wrapped inside it, and it
  moved further out to the side.
- It is no longer buried behind an open chest. It is parented to the root that
  holds both panels and drawn last, so nothing covers it, and it stays glued to
  the weight readout.

## 2.1.1

- The slot label fix from 2.1.0 never ran. It looked up InventoryElement as a
  nested type of InventoryGrid, and that class is top level, so the lookup came
  back empty and the postfix bailed out on its first line. It now references the
  type directly, which the compiler checks.
- A large limit is also written in short form now, so a chest slot reads
  650/100k instead of 650/100000, and the font shrinks from there. That keeps
  the number readable rather than merely small.
- The quick stack button sits lower, fully clear of the inventory panel.

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
