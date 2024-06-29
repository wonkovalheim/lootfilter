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
        internal const string ModVersion = "1.0.1";
        internal const string Author = "wonkotron";
        private const string ModGUID = Author + "." + ModName;

        private Harmony _harmony;

        public static ConfigEntry<bool> filterEnabled;
        private static ConfigEntry<string> blacklistString;
        public static string[] blacklist {
            get;
            private set;
        }

        private static LootFilter _instance;

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
                    if (LootFilter.blacklist.Contains(item.m_dropPrefab.name)) {
                        __result = false;
                    }
                }
            }
        }
    }
}
