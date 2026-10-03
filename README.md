# SmartCraft-Storage Smelter Filter

A small add-on for **SmartCraft-Storage** (tested with 0.10.0) that lets you choose items its auto-feed should **never** pull from chests into smelter-type stations.

## Why

The game treats the windmill, spinning wheel, smelter, blast furnace and eitr refinery as the same kind of station. With `SmelterAutoRefuel` on, SmartCraft-Storage auto-feeds all of them, including the windmill. Since the Deep North update the windmill also grinds Oat Seeds into Oats and Oats into Oat Flour. Both Oats and Oat Flour are food ingredients, but the windmill pulls your Oats out of nearby chests and grinds them all into flour. Moving chests doesn't help, because the windmill puts its output into a nearby chest and then pulls it straight back in.

This add-on keeps auto-feeding everything else and only skips the items you list.

## Install

Requires BepInEx and SmartCraft-Storage. Download `SmartCraftStorage.SmelterFilter-<version>.zip` from the [Releases](../../releases) page.

- **Vortex:** drag the zip into Vortex and enable it.
- **Manual:** extract the zip into your Valheim folder, so the DLL ends up in
  `Valheim/BepInEx/plugins/SmartCraftStorage.SmelterFilter/`.

## Configure

The settings file `BepInEx/config/local.smartcraftstorage.smelterfilter.cfg` is created on first launch. You can also change the setting live in Configuration Manager (F1).

```
SmelterAutoRefuelBlacklist = Oat
```

- The list is comma-separated and capitals don't matter. Example: `Oat, OatSeeds, Barley`
- You can write the in-game name (`Oats`), the internal name (`Oat`) or the item code (`$item_oat`).
- The default blocks Oats only. Oat Seeds are still ground into Oats automatically. Add `OatSeeds` to stop that too.
- You can still put blocked items into a station by hand.

To look up an item's name, check `BepInEx/LogOutput.log`. The first time each station is fed, the add-on logs what it accepts:

```
windmill accepts: prefab 'Barley', token '$item_barley', shown as 'Barley'
windmill accepts: prefab 'Oat', token '$item_oat', shown as 'Oats' -> BLOCKED
windmill accepts: prefab 'OatSeeds', token '$item_oatseeds', shown as 'Oat Seeds'
```

## Notes

- This is an unofficial add-on. It doesn't change any SmartCraft-Storage files. It hooks the method that picks which item to pull (`SmelterPatches.RefuelPatch.FindCookableItem`).
- If a SmartCraft-Storage update renames that method, this add-on switches itself off and logs `Smelter filter is NOT active` instead of breaking anything.
- Feel free to build a similar option into SmartCraft-Storage itself.

## Building from source

Requires the .NET SDK (8 or newer). The project references the game's and BepInEx's DLLs from your Valheim install:

```
dotnet build -c Release -p:ValheimDir="C:\Program Files (x86)\Steam\steamapps\common\Valheim"
```

The DLL ends up in `bin/Release/net48/`. Leave out `-p:ValheimDir=...` if Valheim is in the default Steam location.

## License

MIT, see [LICENSE](LICENSE).
