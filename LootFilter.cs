using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;

namespace lootfilter {

    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class LootFilter : BaseUnityPlugin {
        internal const string ModName = "LootFilter";
        internal const string ModVersion = "1.0.2";
        internal const string Author = "wonkotron";
        private const string ModGUID = Author + "." + ModName;

        private Harmony _harmony = null!;

        public static ConfigEntry<bool> filterEnabled = null!;
        private static ConfigEntry<string> blacklistString = null!;
        public static string[] blacklist {
            get;
            private set;
        } = null!;

        private static LootFilter _instance = null!;

        [UsedImplicitly]
        private void Awake() {
            _instance = this;
            
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModGUID);

            filterEnabled = Config.Bind("Filter", "Filter Enabled", true, "Enable Loot Filter");
            blacklistString = Config.Bind("Filter", "Item Blacklist", "", "Comma separated list (NO SPACES) of PrefabID strings (e.g. Resin,LeatherScraps)");

            // init blacklist
            blacklist = blacklistString.Value.Split(',');

            Config.SettingChanged += Config_SettingChanged;
        }

        private void Config_SettingChanged(object sender, EventArgs e) {
            Config.SettingChanged -= Config_SettingChanged;  // unsubscribe before executing

            blacklist = blacklistString.Value.Split(',');

            Config.SettingChanged += Config_SettingChanged; // resubscribe
        }

        [UsedImplicitly]
        private void OnDestroy() {
            Config.SettingChanged -= Config_SettingChanged;
            _harmony?.UnpatchSelf();
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.CanAddItem), new Type[] {typeof(ItemDrop.ItemData), typeof(int)})]
    public static class Inventory_CanAddItem_Patch {
        public static void Postfix(ItemDrop.ItemData item, int stack, ref bool __result) {
            if (__result) {  // only fire when CanAddItem was going to return true
                if (LootFilter.filterEnabled.Value) {
                    string? name = item?.m_dropPrefab?.name;

                    if (string.IsNullOrEmpty(name)) {
                        // TODO:  log error and explicitly return
                        return;
                    }
                    else {
                        if (LootFilter.blacklist.Contains(name)) {
                            __result = false;  // only change result of CanAddItem if a match is in the filter
                        }
                    }
                }
            }
        }
    }
}
