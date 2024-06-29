using System;
using System.Collections.Generic;
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
        public static ConfigEntry<string> filterString;

        private static LootFilter _instance;

        [UsedImplicitly]
        private void Awake() {
            _instance = this;
            
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModGUID);

            filterEnabled = Config.Bind("Filter", "Filter Enabled", true, "Enable Item Filter");
            filterString = Config.Bind("Filter", "Item Blacklist", "", "Comma separated list of PrefabID strings, NO SPACES");
        }

        private void OnDestroy() {
            _harmony?.UnpatchSelf();
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.CanAddItem), new Type[] {typeof(ItemDrop.ItemData), typeof(int)})]
    public static class Inventory_CanAddItem_Patch {
        public static void Postfix(ItemDrop.ItemData item, int stack, ref bool __result) {
            if (__result) {  // only fire when CanAddItem was going to return true
                if (LootFilter.filterEnabled.Value) {
                    string[] itemIds = LootFilter.filterString.Value.Split(',');

                    if (itemIds.Contains(item.m_dropPrefab.name)) {
                        __result = false;
                    }
                }
            }
        }
    }
}
