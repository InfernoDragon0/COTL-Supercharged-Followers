using System.IO;
using COTL_API.CustomInventory;
using COTL_API.Helpers;
using SuperchargedFollowers;
using UnityEngine;


namespace Namespace;
public class Prayer : CustomInventoryItem
{
    public override string InternalName => "Prayer_Proxy";
    public override string LocalizedName() { return "Prayer"; }
    public override string LocalizedDescription() { return "A Proxy Item for Supercharged Followers."; }
    public override Sprite Sprite => TextureHelper.CreateSpriteFromPath(Path.Combine(Plugin.PluginPath, "Assets/proxy.png"));
    public override Sprite InventoryIcon { get; } = TextureHelper.CreateSpriteFromPath(Path.Combine(Plugin.PluginPath, "Assets/proxy.png"));
}
