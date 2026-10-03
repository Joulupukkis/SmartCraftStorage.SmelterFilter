using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace SmartCraftStorageSmelterFilter;

// Companion to SmartCraft-Storage: lets you stop its smelter auto-feed (which also drives
// the windmill, spinning wheel, blast furnace and eitr refinery) from pulling chosen items.
[BepInPlugin(Guid, "SmartCraft-Storage Smelter Filter", "1.0.0")]
[BepInDependency(TargetGuid)]
public class Plugin : BaseUnityPlugin
{
    public const string Guid = "local.smartcraftstorage.smelterfilter";
    private const string TargetGuid = "com.zellds.smartcraftstorage";

    internal static ConfigEntry<string> Blacklist;
    internal static BepInEx.Logging.ManualLogSource Log;
    private static HashSet<string> _blacklist = new(StringComparer.OrdinalIgnoreCase);
    private static Func<Inventory, string, bool, ItemDrop.ItemData> _findUnlocked;

    private void Awake()
    {
        Log = Logger;
        Blacklist = Config.Bind("Stations", "SmelterAutoRefuelBlacklist", "Oat",
            "Comma-separated items that SmartCraft-Storage must never auto-pull from chests into a smelter-type " +
            "station (windmill, spinning wheel, smelter, blast furnace, eitr refinery). Use the name shown in game " +
            "(Oats, Barley), the prefab name (Oat) or the item token. Not case-sensitive. LogOutput.log lists what " +
            "each station accepts. You can still feed blocked items by hand.");
        Blacklist.SettingChanged += (_, _) => { ParseBlacklist(); _loggedStations.Clear(); };
        ParseBlacklist();

        var target = AccessTools.TypeByName("SmartCraftStorage.Stations.SmelterPatches+RefuelPatch");
        var original = target == null ? null : AccessTools.Method(target, "FindCookableItem");
        var unlocked = AccessTools.TypeByName("SmartCraftStorage.Shared.UnlockedInventory");
        var findItem = unlocked == null ? null : AccessTools.Method(unlocked, "FindItem");
        if (original == null || findItem == null)
        {
            Logger.LogError("SmartCraft-Storage internals not found (mod updated?). Smelter filter is NOT active.");
            return;
        }
        _findUnlocked = (Func<Inventory, string, bool, ItemDrop.ItemData>)
            Delegate.CreateDelegate(typeof(Func<Inventory, string, bool, ItemDrop.ItemData>), findItem);

        new Harmony(Guid).Patch(original, prefix: new HarmonyMethod(typeof(Plugin), nameof(FindCookableItemPrefix)));
        Logger.LogInfo($"Smelter filter active. Blacklist: {Blacklist.Value}");
    }

    private static void ParseBlacklist()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in Blacklist.Value.Split(','))
        {
            var name = part.Trim();
            if (name.Length > 0) set.Add(name);
        }
        _blacklist = set;
    }

    // Matches the prefab name (e.g. "Oat"), the item token ("$item_oat" or "item_oat"),
    // or the name shown in game in the current language (e.g. "Oats").
    private static bool IsBlacklisted(ItemDrop from)
    {
        if (_blacklist.Count == 0) return false;
        string token = from.m_itemData.m_shared.m_name;
        return _blacklist.Contains(from.gameObject.name)
            || _blacklist.Contains(token)
            || _blacklist.Contains(token.TrimStart('$'))
            || _blacklist.Contains(DisplayName(token));
    }

    private static string DisplayName(string token) =>
        Localization.instance != null ? Localization.instance.Localize(token) : token;

    private static readonly HashSet<string> _loggedStations = new();

    // Logs each station type's accepted inputs once, so the right names for the blacklist are easy to find.
    private static void LogConversionsOnce(Smelter smelter)
    {
        string station = Utils.GetPrefabName(smelter.gameObject);
        if (!_loggedStations.Add(station)) return;
        foreach (var c in smelter.m_conversion)
        {
            if (c.m_from == null) continue;
            string token = c.m_from.m_itemData.m_shared.m_name;
            Log.LogInfo($"{station} accepts: prefab '{c.m_from.gameObject.name}', token '{token}', " +
                        $"shown as '{DisplayName(token)}'{(IsBlacklisted(c.m_from) ? " -> BLOCKED" : "")}");
        }
    }

    // Same search as the original, but skips blacklisted inputs instead of returning them.
    private static bool FindCookableItemPrefix(Smelter smelter, Inventory inventory, ref ItemDrop.ItemData __result)
    {
        LogConversionsOnce(smelter);
        if (_blacklist.Count == 0) return true;
        __result = null;
        foreach (var conversion in smelter.m_conversion)
        {
            if (conversion.m_from == null || IsBlacklisted(conversion.m_from)) continue;
            var item = _findUnlocked(inventory, conversion.m_from.m_itemData.m_shared.m_name, true);
            if (item != null)
            {
                __result = item;
                break;
            }
        }
        return false;
    }
}
