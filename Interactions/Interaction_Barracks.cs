using System.Collections.Generic;
using Lamb.UI;
using Lamb.UI.FollowerSelect;
using src.Extensions;
using src.UI.Menus;
using src.UINavigator;
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
        this.label = "Change Follower Class";
    }

    public override void GetSecondaryLabel()
    {
        this.secondaryLabel = "Follower Prestige Levels";
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
        UIMissionaryMenuController followerSelectMenu = MonoSingleton<UIManager>.Instance.MissionaryMenuTemplate.Instantiate();
        List<FollowerInfo> blackList = new();

        foreach (FollowerInfo follower in DataManager.Instance.Followers)
        {
            if (follower.CursedState == Thought.OldAge)
                blackList.Add(follower);
        }

        followerSelectMenu.Show(DataManager.Instance.Followers, blackList, false, UpgradeSystem.Type.Count);
        // followerSelectMenu._missionInfoCardController.ShowCardWithParam(DataManager.Instance.Followers[0]);
        // followerSelectMenu._missionInfoCardController._currentCard.Configure(followerInfo);

        Plugin.Log.LogInfo("on chosen 6");
        followerSelectMenu._missionInfoCardController.OnInfoCardShown += (MissionInfoCard mic) =>
        {
            Plugin.Log.LogInfo("SHOWN");
            var button = mic.MissionButtons[0];
            
            MissionButton mb = Object.Instantiate(button, button.transform.parent);
            MissionButton mb2 = Object.Instantiate(button, button.transform.parent);
            MissionButton mb3 = Object.Instantiate(button, button.transform.parent);
            MissionButton mb4 = Object.Instantiate(button, button.transform.parent);
            MissionButton mb5 = Object.Instantiate(button, button.transform.parent);
            List<MissionButton> list = new List<MissionButton>(){mb, mb2, mb3, mb4, mb5};

            //warrior
            mb.Configure(mic._followerInfo);
            mb2.Configure(mic._followerInfo);
            mb3.Configure(mic._followerInfo);
            mb4.Configure(mic._followerInfo);
            mb5.Configure(mic._followerInfo);

            mb._titleText.text = "Warrior: <sprite name=\"icon_GoodTrait\"> " + "Attack Damage, <sprite name=\"icon_BadTrait\"> Attack Speed";
            mb._amountText.text = "1";
            mb._type = Plugin.warrior;
            // mb._icon = Plugin.warrior;
            mb.OnMissionSelected += (InventoryItem.ITEM_TYPE a) => {
                Plugin.Log.LogInfo("selected " + a);
                this.OnClassChosen(mic._followerInfo, a, followerSelectMenu);
            };
            mb.Start();
            Plugin.Log.LogInfo("on chosen 7");
            
            //prayer
            mb2._titleText.text = "Prayer: Bonus Health and slightly slower Attack Speed";
            mb2._amountText.text = "1";
            mb2._type = Plugin.prayer;
            // mb2._icon = Plugin.prayer;
            mb2.OnMissionSelected += (InventoryItem.ITEM_TYPE a) => {
                Plugin.Log.LogInfo("selected " + a);
                this.OnClassChosen(mic._followerInfo, a, followerSelectMenu);
            };
            mb2.Start();

            //holiday
            mb3._titleText.text = "Holiday: Bonus Health and slightly slower Attack Speed";
            mb3._amountText.text = "1";
            mb3._type = Plugin.holiday;
            // mb3._icon = Plugin.holiday;
            mb3.OnMissionSelected += (InventoryItem.ITEM_TYPE a) => {
                Plugin.Log.LogInfo("selected " + a);
                this.OnClassChosen(mic._followerInfo, a, followerSelectMenu);
            };
            mb3.Start();

            //missionary
            mb4._titleText.text = "Missionary: Bonus Health and slightly slower Attack Speed";
            mb4._amountText.text = "1";
            mb4._type = Plugin.missionary;
            // mb4._icon = Plugin.missionary;
            mb4.OnMissionSelected += (InventoryItem.ITEM_TYPE a) => {
                this.OnClassChosen(mic._followerInfo, a, followerSelectMenu);
                Plugin.Log.LogInfo("selected " + a);
            };
            mb4.Start();

            //undertaker
            mb5._titleText.text = "Undertaker: Bonus Health and slightly slower Attack Speed";
            mb5._amountText.text = "1";
            mb5._type = Plugin.undertaker;
            // mb5._icon = Plugin.undertaker;
            mb5.OnMissionSelected += (InventoryItem.ITEM_TYPE a) => {
                this.OnClassChosen(mic._followerInfo, a, followerSelectMenu);
                Plugin.Log.LogInfo("selected " + a);
            };
            mb5.Start();

            //clear buttons
            foreach (MissionButton mbx in mic._missionButtons) {
                mbx.gameObject.SetActive(false);
            }
            mic._missionButtons = list.ToArray();
        };

        followerSelectMenu.OnShown += new System.Action(() =>
        {
            foreach (FollowerInformationBox fib in followerSelectMenu._followerInfoBoxes)
            {
                var prestige = Helpers.Bonuses.GetPrestigeBonuses(fib.FollowerInfo);
                fib.FollowerRole.text = "Warrior | Prestige " + prestige.Level + " |  Health: " + (0.5 + fib._followerInfo.FollowerLevel * 1)  + " | Attack: " + (fib._followerInfo.FollowerLevel * 0.5);
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
                var prestige = Helpers.Bonuses.GetPrestigeBonuses(fib.FollowerInfo);
                var nextLevel = Helpers.Bonuses.PrestigeToNextLevel(fib.FollowerInfo);
                fib.FollowerRole.text = "Prestige " + prestige.Level + "| " + nextLevel + " More to next Prestige | Bonuses: " + prestige.HealthBonus + "HP, " + prestige.AttackBonus + "ATK, " + prestige.MovementSpeedBonus + "MOV, "
                    + prestige.DelayBonus + "SPD";
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

    public void OnPrestigeChosen(FollowerInfo followerInfo) {
 
        //add prestige until next level
        int toNextLevel = Helpers.Bonuses.PrestigeToNextLevel(followerInfo);
        //check if inventory has enough
        int quantity = Inventory.GetItemQuantity(Plugin.prestige);
        if (quantity >= toNextLevel) {
            //remove the prestige from inventory
            Inventory.ChangeItemQuantity(Plugin.prestige, -toNextLevel);
            //add the prestige to the follower
            for (int i = 0; i < toNextLevel; i++) {
                followerInfo.Inventory.Add(new InventoryItem(Plugin.prestige, 1));
            }
            NotificationCentreScreen.Play("Prestige level increased for " + followerInfo.Name);
        }
        else {
            NotificationCentreScreen.Play("Not enough Prestige to level, need " + toNextLevel + " Prestige");
        }

        this.OnHidden();
    }

    public void OnClassChosen(FollowerInfo followerInfo, InventoryItem.ITEM_TYPE classType, UIMissionaryMenuController followerSelectMenu) {
        //change the class of the follower
        Plugin.Log.LogInfo("class chosen " + classType);

        //if follower inventory contains any other proxy items, remove them
        followerInfo.Inventory.RemoveAll(item => Plugin.allJobs.Contains(item.type));
        
        //then, add the new proxy item
        followerInfo.Inventory.Add(new InventoryItem(classType, 1));
        
        followerSelectMenu.Hide();
        this.OnHidden();

        //check if the proxy items might be dumped out to the chest over time
    }


}
