using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using BepInEx.Configuration;
using System.Collections.Generic;

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
        public static List<FollowerInfo> summonList;
        public static FollowerInfo commander;

        public static bool summoned = false;

        private void Awake()
        {
            Plugin.Log = base.Logger;

            PluginPath = Path.GetDirectoryName(Info.Location);

            //SETUP: Config
            /*ammoConfig = Config.Bind("SuperchargedTarots", "Ammo", 66f, "Fervor Use Discount (higher the lesser, but dont go over 100)");*/
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