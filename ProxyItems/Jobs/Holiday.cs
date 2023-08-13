using System.IO;
using COTL_API.CustomInventory;
using COTL_API.Helpers;
using SuperchargedFollowers;
using UnityEngine;


namespace SuperchargedFollowers.ProxyItems;
public class Holiday : CustomInventoryItem
{
    public override string InternalName => "Holiday_Proxy";
    public override string LocalizedName() { return "Holiday"; }
    public override string LocalizedDescription() { return "A Proxy Item for Supercharged Followers."; }
    public override Sprite Sprite => TextureHelper.CreateSpriteFromPath(Path.Combine(Plugin.PluginPath, "Assets/proxy.png"));
    public override Sprite InventoryIcon { get; } = TextureHelper.CreateSpriteFromPath(Path.Combine(Plugin.PluginPath, "Assets/proxy.png"));
}
