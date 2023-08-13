using System.Collections.Generic;
using Lamb.UI;
using Lamb.UI.FollowerSelect;
using src.Extensions;
using src.UI.Menus;
using SuperchargedFollowers.Structures;
using UnityEngine;

namespace SuperchargedFollowers.Interactions;
public class Interaction_Barracks : Interaction
{
    public Structure Structure;
    private bool Activating = false;
    private GameObject Player;
    private float Delay = 0.04f;
    public float DistanceToTriggerDeposits = 5f;

    public StructuresData StructureInfo => this.Structure.Structure_Info;
    public BarracksStructure BarracksStructure => this.Structure.Brain as BarracksStructure;
    public override void GetLabel()
    {
        this.secondaryLabel = "Follower Prestige Levels";
        this.label = "Change Follower Class";
    }

    private void Start()
    {
        this.HasSecondaryInteraction = true;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        Plugin.Log.LogInfo("Barracks OnEnable");
        Structure = GetComponentInParent<Transform>().GetComponent<Structure>();
    }

    public override void OnInteract(StateMachine state)
    {
        if (this.Activating) return;
        base.OnInteract(state);
        this.Activating = true;

        //open the ui for rallying followers
        GameManager.GetInstance().OnConversationNew();

        //Time.timeScale = 0.0f; //set timescale if want to pause

        HUD_Manager.Instance.Hide(false, 0);
        UIFollowerSelectMenuController followerSelectMenu = MonoSingleton<UIManager>.Instance.FollowerSelectMenuTemplate.Instantiate();
        List<FollowerInfo> blackList = new();

        foreach (FollowerInfo follower in DataManager.Instance.Followers)
        {
            if (follower.CursedState == Thought.OldAge)
                blackList.Add(follower);
        }

        followerSelectMenu.Show(DataManager.Instance.Followers, blackList, false, UpgradeSystem.Type.Count, true, true, true);

        followerSelectMenu.OnFollowerSelected += new System.Action<FollowerInfo>(this.OnFollowerChosen);
        followerSelectMenu.OnShown += new System.Action(() =>
        {
            foreach (FollowerInformationBox fib in followerSelectMenu._followerInfoBoxes)
            {
                fib.FollowerRole.text = "Warrior | Prestige 0 |  Health: " + (0.5 + fib._followerInfo.FollowerLevel * 1)  + " | Attack: " + (fib._followerInfo.FollowerLevel * 0.5);
            }
        });
        followerSelectMenu.OnHidden += () =>
        {
            followerSelectMenu = null;
            this.OnHidden();
        };

        followerSelectMenu.OnCancel += () =>
        {
            followerSelectMenu = null;
            this.OnHidden();
        };
    }

    public override void OnSecondaryInteract(StateMachine state)
    {
        if (this.Activating) return;
        base.OnSecondaryInteract(state);
        this.Activating = true;

        //open the ui to select a commander. commander can only be selected if they are in the folloewr list
        GameManager.GetInstance().OnConversationNew();

        //Time.timeScale = 0.0f; //set timescale if want to pause

        HUD_Manager.Instance.Hide(false, 0);
        UIFollowerSelectMenuController followerSelectMenu = MonoSingleton<UIManager>.Instance.FollowerSelectMenuTemplate.Instantiate();
        List<FollowerInfo> blackList = new();

        foreach (FollowerInfo follower in DataManager.Instance.Followers)
        {
            if (!Plugin.summonList.Contains(follower)) {
                blackList.Add(follower);
            }
        }

        followerSelectMenu.Show(DataManager.Instance.Followers, blackList, false, UpgradeSystem.Type.Count, true, true, true);

        followerSelectMenu.OnFollowerSelected += new System.Action<FollowerInfo>(this.OnPrestigeChosen);
        followerSelectMenu.OnShown += new System.Action(() =>
        {
            foreach (FollowerInformationBox fib in followerSelectMenu._followerInfoBoxes)
            {
                fib.FollowerRole.text = "Warrior | Prestige 0 |  Health: " + (0.5 + fib._followerInfo.FollowerLevel * 1)  + " | Attack: " + (fib._followerInfo.FollowerLevel * 0.5);
            }
        });
        followerSelectMenu.OnHidden += () =>
        {
            followerSelectMenu = null;
            this.OnHidden();
        };

        followerSelectMenu.OnCancel += () =>
        {
            followerSelectMenu = null;
            this.OnHidden();
        };

        // this.Activating = false;
    }

    public void OnHidden() {
        this.Activating = false;
        HUD_Manager.Instance.Show();
        GameManager.GetInstance().OnConversationEnd();
    }

    public void OnFollowerChosen(FollowerInfo followerInfo) {
        GameManager.GetInstance().OnConversationNew();
        HUD_Manager.Instance.Hide(false, 0);

        //open a new menu to select the class
        UIMissionaryMenuController followerSelectMenu = MonoSingleton<UIManager>.Instance.MissionaryMenuTemplate.Instantiate();
        followerSelectMenu.FollowerSelected(followerInfo);

        followerSelectMenu.OnMissionaryChosen += new System.Action<FollowerInfo, InventoryItem.ITEM_TYPE>(this.OnClassChosen);
        followerSelectMenu.OnHidden += () =>
        {
            followerSelectMenu = null;
            this.OnHidden();
        };

        followerSelectMenu.OnCancel += () =>
        {
            followerSelectMenu = null;
            this.OnHidden();
        };
        followerSelectMenu.Show();
    }

    public void OnPrestigeChosen(FollowerInfo followerInfo) {
        //open a menu to determine how many prestige to give
        GameManager.GetInstance().OnConversationNew();
        HUD_Manager.Instance.Hide(false, 0);

        //open a new menu to select the class
        UIMissionaryMenuController followerSelectMenu = MonoSingleton<UIManager>.Instance.MissionaryMenuTemplate.Instantiate();
        followerSelectMenu.FollowerSelected(followerInfo);
        Plugin.Log.LogInfo("we have x missions here");
        Plugin.Log.LogInfo(followerSelectMenu._missionInfoCardController._currentCard.MissionButtons.Length);

        //put a button to show "Give until next level (3 prestiges)"
        
        
        followerSelectMenu.OnMissionaryChosen += new System.Action<FollowerInfo, InventoryItem.ITEM_TYPE>(this.OnClassChosen);
        followerSelectMenu.OnHidden += () =>
        {
            followerSelectMenu = null;
            this.OnHidden();
        };

        followerSelectMenu.OnCancel += () =>
        {
            followerSelectMenu = null;
            this.OnHidden();
        };
        followerSelectMenu.Show();
    }

    public void OnClassChosen(FollowerInfo followerInfo, InventoryItem.ITEM_TYPE classType) {
        //change the class of the follower
        Plugin.Log.LogInfo("class chosen " + classType);

        //if follower inventory contains any other proxy items, remove them
        foreach (InventoryItem item in followerInfo.Inventory)
        {
            if (Plugin.allJobs.Contains(item.type))
            {
                followerInfo.Inventory.Remove(item);
            }
        }
        //then, add the new proxy item
        followerInfo.Inventory.Add(new InventoryItem(classType, 1));

        //check if the proxy items might be dumped out to the chest over time
    }
}
