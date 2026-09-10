# Publishing to Thunderstore

Uploading runs under a Thunderstore account, so this is a manual step. The
package itself is already built and validated by
`packaging/build-package.ps1`.

## What to upload

`dist/GorilaChestMod-<version>-thunderstore.zip`, the same file attached to the
matching [GitHub release](https://github.com/trentinidev/GorilaChestMod/releases).
It holds `manifest.json`, `icon.png`, `README.md`, `CHANGELOG.md` and
`GorilaChestMod.dll` at the root, which is the layout Thunderstore expects.

## Metadata to use

| Field | Value |
| --- | --- |
| Community | Valheim |
| Team / namespace | `trentinidev` |
| Package name | `GorilaChestMod` |
| Categories | `Utility`, `Client-side`, `Server-side` |
| NSFW | no |

Both side categories are correct: the mod runs on the client and has to be on the
dedicated server as well, see [server-install.md](server-install.md).

The page description and the changelog come from the `README.md` and
`CHANGELOG.md` inside the zip, so there is nothing to paste by hand.

## Steps

1. Sign in at [thunderstore.io](https://thunderstore.io) and, the first time
   only, create the team `trentinidev` under Settings, Teams. The namespace has
   to match the team name.
2. Go to the Valheim community, Upload, or
   `https://thunderstore.io/c/valheim/create/`.
3. Pick the zip, select the team, tick the three categories, leave NSFW off.
4. Submit. The listing appears at
   `https://thunderstore.io/c/valheim/p/trentinidev/GorilaChestMod/`.

A version number can never be reused, so bump `<Version>` in the csproj and
re-run the packaging script before uploading again.

## Checks the upload runs, which the packaging script already covers

- `manifest.json` present, with `name`, `version_number`, `website_url`,
  `description` and `dependencies`.
- Name made only of letters, digits and underscores.
- Description at most 250 characters.
- `icon.png` exactly 256 by 256.
- `README.md` present.
- Every dependency exists, here `denikson-BepInExPack_Valheim-5.4.2350`.

## Publishing from the command line, later

The Thunderstore CLI can push a build without the website. It needs a service
account token, created under the team's settings, kept in the `TCLI_AUTH_TOKEN`
environment variable and never committed.

```
tcli publish --file dist/GorilaChestMod-<version>-thunderstore.zip
```

That requires a `thunderstore.toml` describing the package. It is not in this
repository yet, because nothing here has exercised the CLI path. The values it
needs are the same ones in the table above.
