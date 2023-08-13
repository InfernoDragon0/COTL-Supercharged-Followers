using System;
using System.IO;
using COTL_API.CustomStructures;
using COTL_API.Helpers;
using Lamb.UI.BuildMenu;
using SuperchargedFollowers;
using SuperchargedFollowers.Interactions;
using UnityEngine;


namespace SuperchargedFollowers.Structures;
public class RallyStructure : CustomStructure
{
    public override string InternalName => "RALLY_STRUCTURE";
    public override Type Interaction => typeof(Interaction_Rally); //TODO: add interaction
    public override Sprite Sprite => TextureHelper.CreateSpriteFromPath(Path.Combine(Plugin.PluginPath, "Assets/Rally.png"));
    public override int BuildDurationMinutes => 30;
    public override Vector2Int Bounds => new(2, 2);
    public override string GetLocalizedName() => "Rally Flag";
    public override string GetLocalizedDescription() => "[Supercharged Series] Rally your followers to bring them to battle.";

    public override string GetLocalizedLore() => "[Supercharged Series]";
    public override FollowerCategory.Category Category => FollowerCategory.Category.Misc;
    public override Categories StructureCategories => Categories.FAITH;



}
