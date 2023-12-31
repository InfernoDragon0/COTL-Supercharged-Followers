using System.Collections.Generic;
using System.Linq;

namespace SuperchargedFollowers.Helpers;
public class Bonuses
{
    public static BuffStats GetClassBonuses(FollowerInfo followerInfo) {
        
        BuffStats buffStats = new();

        foreach (InventoryItem inventoryItem in followerInfo.Inventory) {
            if (inventoryItem.type == (int)Plugin.holiday) {
                buffStats.HealthBonus = 4f;
                buffStats.ClassName = "Holiday";
                break;
            }
            if (inventoryItem.type == (int)Plugin.warrior) {
                buffStats.AttackBonus = 2f;
                buffStats.HealthBonus = 2f;
                buffStats.ClassName = "Warrior";
                break;
            }
            if (inventoryItem.type == (int)Plugin.missionary) {
                buffStats.HealthBonus = 1f;
                buffStats.MovementSpeedBonus = 0.02f;
                buffStats.ClassName = "Missionary";
                break;
            }
            if (inventoryItem.type == (int)Plugin.undertaker) {
                buffStats.RegenBonus = 0.5f;
                buffStats.ClassName = "Undertaker";
                break;
            }
            if (inventoryItem.type == (int)Plugin.prayer) {
                buffStats.AttackBonus = 4f;
                buffStats.ClassName = "Prayer";
                break;
            }
        }
        
        return buffStats;
    }

    public static BuffStats GetNecklaceBonuses(FollowerInfo followerInfo) {

        BuffStats buffStats = new();

        switch (followerInfo.Necklace) {
            case InventoryItem.ITEM_TYPE.Necklace_1: //add atk spdd
                buffStats.DelayBonus = 0.5f;
                break;
            case InventoryItem.ITEM_TYPE.Necklace_2: //add speed
                buffStats.MovementSpeedBonus = 0.02f;
                break;
            case InventoryItem.ITEM_TYPE.Necklace_3: //add damage
                buffStats.AttackBonus = 1;
                break;
            case InventoryItem.ITEM_TYPE.Necklace_4: //add health
                buffStats.HealthBonus = 1;
                break;
            case InventoryItem.ITEM_TYPE.Necklace_5: //add regen
                buffStats.RegenBonus = 0.25f;
                break;
        }

        return buffStats;
    }
    public static BuffStats GetCommanderBonuses(FollowerInfo followerInfo) {
        BuffStats buffStats = new();

        if (Plugin.commander == followerInfo) {
            buffStats.AttackBonus = 3;
            buffStats.HealthBonus = 10;
            buffStats.DelayBonus = 1;
            buffStats.MovementSpeedBonus = 0.09f;
            buffStats.RegenBonus = 1.5f;
            buffStats.SizeBonus = 1;
        }

        return buffStats;
    }

    public static BuffStats GetPrestigeBonuses(FollowerInfo followerInfo) {
        BuffStats buffStats = new();

        int prestigeTotal = followerInfo.Inventory.Count(x => x.type == (int)Plugin.prestige);

        if (prestigeTotal >= 100) { //Level 10
            buffStats.CritChance = 0.1f;
            buffStats.Level = 10;
        }
        if (prestigeTotal >= 80) { //Level 9
            buffStats.DropPrestigeChance = 0.1f;
            buffStats.Level = 9;
        }
        if (prestigeTotal >= 60) { //Level 8
            buffStats.CurseRegenBonus = 1f;
            buffStats.Level = 8;
        }
        if (prestigeTotal >= 40) { //Level 7
            buffStats.BlueHealthChance = 0.2f;
            buffStats.Level = 7;
        }
        if (prestigeTotal >= 20) { //Level 6
            buffStats.RegenBonus = 0.5f;
            buffStats.Level = 6;
        }

        if (prestigeTotal >= 15) { //Level 5
            buffStats.HealthBonus = +0.5f;
            buffStats.DelayBonus = +0.5f;
            buffStats.Level = 5;
        }
        if (prestigeTotal >= 12) { //Level 4
            buffStats.HealthBonus += 0.5f;
            buffStats.AttackBonus += 0.5f;
            buffStats.Level = 4;
        }
        if (prestigeTotal >= 9) { //Level 3
            buffStats.HealthBonus += 1f;
            buffStats.MovementSpeedBonus += 0.03f;
            buffStats.Level = 3;
        }
        if (prestigeTotal >= 6) { //Level 2
            buffStats.HealthBonus += 0.5f;
            buffStats.AttackBonus += 0.25f;
            buffStats.Level = 2;
        }
        if (prestigeTotal >= 3) { //Level 1
            buffStats.HealthBonus += 0.5f;
            buffStats.AttackBonus += 0.5f;
            buffStats.Level = 1;
        }

        return buffStats;
    }

    public static int PrestigeToNextLevel(FollowerInfo followerInfo) {
        int prestigeTotal = followerInfo.Inventory.Count(x => x.type == (int)Plugin.prestige);
        List<int> prestigeValues = new List<int>(){
            3, 6, 9, 12, 15, 20, 40, 60, 80, 100
        };
        int nextLevel = prestigeValues.FirstOrDefault(x => x > prestigeTotal);
        return nextLevel - prestigeTotal;
    }
}
