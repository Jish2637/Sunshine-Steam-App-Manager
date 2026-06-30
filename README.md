# Sunshine Steam App Manager

Based on https://github.com/CommonMugger/Sunshine-App-Automation

Sunshine Steam App Manager is a Windows app for adding installed Steam games to Sunshine by updating Sunshine's `apps.json` app list.

The app scans Steam library manifests, reads Steam AppIDs and game names, optionally downloads cover art from SteamGridDB, and writes clean Sunshine application entries that launch games with `steam://rungameid/APPID`.

It is recommended to run as admin if you are using default steam/sunshine installation folders otherwise the app will not be able to edit required files.

## What It Does

- Detects installed Steam games from `libraryfolders.vdf` and `appmanifest_*.acf` files.
- Adds missing Steam games to Sunshine `apps.json`.
- Repairs generated or clearly matched Steam URI entries.
- Avoids duplicate Sunshine entries.
- Preserves unrelated user-created Sunshine apps and unknown JSON fields.
- Creates a timestamped backup before every write.
- Tracks generated apps in `%LocalAppData%\SunshineSteamAppManager\managed-apps.json`.
- Logs operations to `%LocalAppData%\SunshineSteamAppManager\logs\app.log`.

## Simple Help

1. Open the app. It starts on Home.
2. Click Auto-detect Steam.
3. Click Check setup.
4. Set up cover pictures if you want artwork for your games.
5. Click Scan Steam games.
6. Tick the games you want to add or fix.
7. Click Download covers if you set up a SteamGridDB key.
8. Click Preview changes to check what the app will do.
9. Click Apply selected when you are ready.

The app makes a backup before it changes your Sunshine app list. You can restore a backup from Advanced Mode under Backups / Logs.

Restart Sunshine after applying changes is on by default. The app will ask before it restarts Sunshine.

## Getting Cover Pictures

Covers are recommended but optional. To use them:

1. Create or sign in to a free SteamGridDB account.
2. In the app, click Open API page in the cover pictures step.
3. Or open this page yourself: https://www.steamgriddb.com/profile/preferences/api
4. Generate an API key on SteamGridDB.
5. Copy the API key and paste it into the app.
6. Click Save key, then Test key.
7. After scanning games, click Download covers.

## Selecting Sunshine apps.json

Home handles the normal setup flow. If you need to choose the Sunshine app list manually, open Advanced Mode, then Setup, and set the Sunshine apps file path. The default is:

```text
C:\Program Files\Sunshine\config\apps.json
```

The generated JSON root is kept in this shape:

```json
{
  "apps": [],
  "env": {}
}
```

`env` is always written as an object, never as an empty string.

## Scanning Steam Games

On Home, click Auto-detect Steam. If you need to choose the Steam library file manually, open Advanced Mode, then Setup, and select:

```text
C:\Program Files (x86)\Steam\steamapps\libraryfolders.vdf
```

Then return to Home and click Scan Steam games. Broken manifests are logged and skipped without stopping the scan.

## Downloading Covers

Cover pictures use SteamGridDB. On Home, use the cover pictures step to open the SteamGridDB API page, generate an API key, paste it into the app, save it, and test it.

Covers are saved as:

```text
steam_APPID.png
```

If a cover cannot be found or downloaded, the game can still be added with an empty `image-path`.

## Previewing And Applying

Before writing anything, Preview changes opens a review window showing:

- Apps to add
- Apps to update
- Apps to repair
- Covers to download
- Stale managed apps found
- The backup path that will be created

Apply selected creates a timestamped backup and then updates `apps.json`.

## Backups And Restore

Every write creates a backup next to `apps.json`:

```text
apps.json.backup-YYYYMMDD-HHMMSS
```

Use the Backups / Logs tab to create a manual backup, restore a selected backup, open the log folder, or inspect the live operation log.

## Generated Sunshine Entry

Generated Steam apps use the Steam URI as the main Sunshine command:

```json
{
  "auto-detach": true,
  "cmd": "steam://rungameid/3321460",
  "elevated": false,
  "exclude-global-prep-cmd": false,
  "exit-timeout": 5,
  "image-path": "C:\\Program Files\\Sunshine\\config\\covers\\steam_3321460.png",
  "name": "Crimson Desert",
  "output": "",
  "wait-all": true
}
```

Steam games are placed in Command, not Detached Commands, because Sunshine should launch the Steam URI as the application command. Empty `detached`, empty `prep-cmd`, and unused `working-dir` fields are not generated.

## Manual Test Checklist

1. App opens to Home tab.
2. Steam auto-detect works.
3. Steam scan finds installed appmanifest files.
4. Broken manifests are logged but do not crash the app.
5. Existing Sunshine apps load from `apps.json` in Advanced Mode.
6. Preview shows changes before writing.
7. Applying changes creates a timestamped `apps.json` backup.
8. Generated Steam app has `cmd = steam://rungameid/APPID`.
9. Generated Steam app does not include `detached` when unused.
10. Generated Steam app does not include `prep-cmd` when unused.
11. `auto-detach` is `true`.
12. `wait-all` is `true`.
13. `exit-timeout` is number `5`, not string `"5"`.
14. `image-path` points to a local PNG when available.
15. Existing Steam apps are not duplicated.
16. Old detached Steam URI entries can be repaired.
17. User-created non-Steam apps are preserved.
18. `sunshine.conf` is not modified.
