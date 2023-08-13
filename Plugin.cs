using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using BepInEx.Configuration;
using System.Collections.Generic;
using COTL_API.CustomInventory;
using SuperchargedFollowers.ProxyItems;

namespace SuperchargedFollowers
{
    [BepInPlugin(PluginGuid, PluginName, PluginVer)]
    [BepInDependency("io.github.xhayper.COTL_API")]
    [HarmonyPatch]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "InfernoDragon0.cotl.SuperchargedFollowers";
        public const string PluginName = "SuperchargedFollowers";
        public const string PluginVer = "1.0.0";

        internal static ManualLogSource Log;
        internal readonly static Harmony Harmony = new(PluginGuid);

        internal static string PluginPath;

        //Config for Tarots
        internal static ConfigEntry<float> ammoConfig;

        //Summoning list for Rally Banner
        public static List<FollowerInfo> summonList;
        public static FollowerInfo commander;

        public static bool summoned = false;

        //Temp List to store summoned follower to track alive or dead
        public static List<EnemyFollower> tempSummoned;

        //ITEMS
        public static InventoryItem.ITEM_TYPE holiday;
        public static InventoryItem.ITEM_TYPE prayer;
        public static InventoryItem.ITEM_TYPE missionary;
        public static InventoryItem.ITEM_TYPE warrior;
        public static InventoryItem.ITEM_TYPE undertaker;

        public static InventoryItem.ITEM_TYPE prestige;

        public static List<int> allJobs;

        private void Awake()
        {
            Plugin.Log = base.Logger;

            PluginPath = Path.GetDirectoryName(Info.Location);

            //SETUP: Config
            /*ammoConfig = Config.Bind("SuperchargedTarots", "Ammo", 66f, "Fervor Use Discount (higher the lesser, but dont go over 100)");*/

            //ADD: ITEMS
            holiday = CustomItemManager.Add(new Holiday());
            prayer = CustomItemManager.Add(new Prayer());
            missionary = CustomItemManager.Add(new Missionary());
            warrior = CustomItemManager.Add(new Warrior());
            undertaker = CustomItemManager.Add(new Undertaker());

            prestige = CustomItemManager.Add(new Prestige());

            allJobs = [
                (int)holiday,
                (int)prayer,
                (int)missionary,
                (int)warrior,
                (int)undertaker
            ];

            //ADD: STRUCTURES

        }

        private void OnEnable()
        {
            Harmony.PatchAll();
            Logger.LogInfo($"Loaded {PluginName}!");
        }

        private void OnDisable()
        {
            Harmony.UnpatchSelf();
            Logger.LogInfo($"Unloaded {PluginName}!");
        }
    }
}