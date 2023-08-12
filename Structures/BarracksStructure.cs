using System;
using System.IO;
using COTL_API.CustomStructures;
using COTL_API.Helpers;
using Lamb.UI.BuildMenu;
using SuperchargedFollowers;
using UnityEngine;


namespace Namespace;
public class BarracksStructure: CustomStructure
{
    public override string InternalName => "BARRACKS_STRUCTURE";
    public override Type Interaction => typeof(Interaction_Barracks); //TODO: add interaction
    public override Sprite Sprite => TextureHelper.CreateSpriteFromPath(Path.Combine(Plugin.PluginPath, "Assets/Barracks.png"));
    public override int BuildDurationMinutes => 30;
    public override Vector2Int Bounds => new(2, 2);
    public override string GetLocalizedName() => "Barracks";
    public override string GetLocalizedDescription() => "[Supercharged Series] Upgrade your followers for battle here.";

    public override string GetLocalizedLore() => "[Supercharged Series]";
    public override FollowerCategory.Category Category => FollowerCategory.Category.Misc;
    public override Categories StructureCategories => Categories.FAITH;
}
