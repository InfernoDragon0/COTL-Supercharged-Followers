using System.IO;
using COTL_API.CustomInventory;
using COTL_API.Helpers;
using SuperchargedFollowers;
using UnityEngine;


namespace Namespace;
public class Prestige : CustomInventoryItem
{
    public override string InternalName => "Prestige";
    public override string LocalizedName() { return "Prestige"; }
    public override string LocalizedDescription() { return "Give this to your followers via the Barracks to upgrade their battle stats."; }
    public override Sprite Sprite => TextureHelper.CreateSpriteFromPath(Path.Combine(Plugin.PluginPath, "Assets/prestige.png"));
    public override Sprite InventoryIcon { get; } = TextureHelper.CreateSpriteFromPath(Path.Combine(Plugin.PluginPath, "Assets/prestige.png"));
}
