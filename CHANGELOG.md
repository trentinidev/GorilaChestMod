# Changelog

## 1.0.0

First release, built against Valheim 1.0.7.

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
- Client side. Works on a dedicated server with no server side mod.
