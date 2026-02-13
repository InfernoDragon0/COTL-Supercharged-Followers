using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using BepInEx.Configuration;
using System.Collections.Generic;
using COTL_API.CustomInventory;
using SuperchargedFollowers.ProxyItems;
using COTL_API.CustomStructures;
using SuperchargedFollowers.Structures;

namespace SuperchargedFollowers
{
    [BepInPlugin(PluginGuid, PluginName, PluginVer)]
    [BepInDependency("io.github.xhayper.COTL_API")]
    [HarmonyPatch]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "InfernoDragon0.cotl.SuperchargedFollowers";
        public const string PluginName = "SuperchargedFollowers";
        public const string PluginVer = "1.0.5";

        internal static ManualLogSource Log;
        internal readonly static Harmony Harmony = new(PluginGuid);

        internal static string PluginPath;

        //Config for Tarots
        internal static ConfigEntry<float> followerTransparency;
        internal static ConfigEntry<bool> shouldCommanderTransparent;
        internal static ConfigEntry<bool> shouldBypassHitStop;

        //Summoning list for Rally Banner
        public static List<FollowerInfo> summonList = new();
        public static FollowerInfo commander;

        public static bool summoned = false;

        //Temp List to store summoned follower to track alive or dead
        public static List<Follower> tempSummoned = new();

        //ITEMS
        public static InventoryItem.ITEM_TYPE holiday;
        public static InventoryItem.ITEM_TYPE prayer;
        public static InventoryItem.ITEM_TYPE missionary;
        public static InventoryItem.ITEM_TYPE warrior;
        public static InventoryItem.ITEM_TYPE undertaker;

        public static InventoryItem.ITEM_TYPE prestige;

        //STRUCTURES
        public static StructureBrain.TYPES rally;
        public static StructureBrain.TYPES rallymulti;
        public static StructureBrain.TYPES barracks;

        public static List<int> allJobs;

        private void Awake()
        {
            Plugin.Log = base.Logger;

            PluginPath = Path.GetDirectoryName(Info.Location);

            //SETUP: Config
            followerTransparency = Config.Bind("SuperchargedFollowers", "FollowerTransparency", 0.5f, "Transparency of the followers, from 0 to 1");
            shouldCommanderTransparent = Config.Bind("SuperchargedFollowers", "CommanderTransparency", true, "Should the commander be transparent?");
            shouldBypassHitStop = Config.Bind("SuperchargedFollowers", "BypassHitStop", true, "Should attacks bypass hitstop feature? (recommended to TRUE for improved combat experience)");
            /*ammoConfig = Config.Bind("SuperchargedTarots", "Ammo", 66f, "Fervor Use Discount (higher the lesser, but dont go over 100)");*/

            //ADD: ITEMS
            holiday = CustomItemManager.Add(new Holiday());
            prayer = CustomItemManager.Add(new Prayer());
            missionary = CustomItemManager.Add(new Missionary());
            warrior = CustomItemManager.Add(new Warrior());
            undertaker = CustomItemManager.Add(new Undertaker());

            prestige = CustomItemManager.Add(new Prestige());

            allJobs = new List<int>(){
                (int)holiday,
                (int)prayer,
                (int)missionary,
                (int)warrior,
                (int)undertaker
            };
                

            //ADD: STRUCTURES
            rally = CustomStructureManager.Add(new RallyStructure());
            rallymulti = CustomStructureManager.Add(new RallyMultiStructure());
            barracks = CustomStructureManager.Add(new BarracksStructure());

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