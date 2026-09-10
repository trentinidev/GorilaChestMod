# Download

The built package, committed here so it can be grabbed straight from the file
list without building the project or opening the releases page.

| File | Use it for |
| --- | --- |
| `GorilaChestMod-2.2.2-thunderstore.zip` | Thunderstore upload, and `Import local mod` in r2modman or Thunderstore Mod Manager |

Same file as the asset on the [v2.2.2 release](https://github.com/trentinidev/GorilaChestMod/releases/tag/v2.2.2).

For a manual or dedicated server install, the dll inside goes to
`BepInEx/plugins/GorilaChestMod/`. See [../docs/server-install.md](../docs/server-install.md).

`packaging/build-package.ps1` writes every build into `dist/`, which stays out of
the repository. Only the file above is committed, and it is refreshed when a
version ships.
