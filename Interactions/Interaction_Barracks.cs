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

        //
        // var list = new List<MissionButton>();
        // for (int i = 0; i < 5; i++)
        // {
        //     GameObject go = GameObject.Instantiate(followerSelectMenu._missionInfoCardController._currentCard.MissionButtons[0].gameObject);
        //     list.Add((MissionButton)go);
        // }
        // followerSelectMenu._missionInfoCardController._currentCard._missionButtons = list.ToArray();

        //check length of buttons, and assign classes based on the current
        var length = followerSelectMenu._missionInfoCardController._currentCard.MissionButtons.Length;
        var button = followerSelectMenu._missionInfoCardController._currentCard.MissionButtons[0];
        MissionButton mb = Object.Instantiate(button, followerSelectMenu._missionInfoCardController._currentCard.transform.parent);
        MissionButton mb2 = Object.Instantiate(button, followerSelectMenu._missionInfoCardController._currentCard.transform.parent);
        MissionButton mb3 = Object.Instantiate(button, followerSelectMenu._missionInfoCardController._currentCard.transform.parent);
        MissionButton mb4 = Object.Instantiate(button, followerSelectMenu._missionInfoCardController._currentCard.transform.parent);
        MissionButton mb5 = Object.Instantiate(button, followerSelectMenu._missionInfoCardController._currentCard.transform.parent);
        List<MissionButton> list = [mb, mb2, mb3, mb4, mb5];
        //clear buttons
        followerSelectMenu._missionInfoCardController._currentCard._missionButtons = list.ToArray();

        //warrior
        mb._titleText.text = "Warrior: Bonus Attack Damage and slightly slower Attack Speed";
        mb._amountText.text = "1";
        mb._type = Plugin.warrior;
        // mb._icon = Plugin.warrior;
        mb.OnMissionSelected += (InventoryItem.ITEM_TYPE a) => {
            Plugin.Log.LogInfo("selected " + a);
        };
        mb.Configure(followerInfo);
        mb.Start();
        
        //prayer
        mb2._titleText.text = "Prayer: Bonus Health and slightly slower Attack Speed";
        mb2._amountText.text = "1";
        mb2._type = Plugin.prayer;
        // mb2._icon = Plugin.prayer;
        mb2.OnMissionSelected += (InventoryItem.ITEM_TYPE a) => {
            Plugin.Log.LogInfo("selected " + a);
        };
        mb2.Configure(followerInfo);
        mb2.Start();

        //holiday
        mb3._titleText.text = "Holiday: Bonus Health and slightly slower Attack Speed";
        mb3._amountText.text = "1";
        mb3._type = Plugin.holiday;
        // mb3._icon = Plugin.holiday;
        mb3.OnMissionSelected += (InventoryItem.ITEM_TYPE a) => {
            Plugin.Log.LogInfo("selected " + a);
        };
        mb3.Configure(followerInfo);
        mb3.Start();

        //missionary
        mb4._titleText.text = "Missionary: Bonus Health and slightly slower Attack Speed";
        mb4._amountText.text = "1";
        mb4._type = Plugin.missionary;
        // mb4._icon = Plugin.missionary;
        mb4.OnMissionSelected += (InventoryItem.ITEM_TYPE a) => {
            Plugin.Log.LogInfo("selected " + a);
        };
        mb4.Configure(followerInfo);
        mb4.Start();

        //undertaker
        mb5._titleText.text = "Undertaker: Bonus Health and slightly slower Attack Speed";
        mb5._amountText.text = "1";
        mb5._type = Plugin.undertaker;
        // mb5._icon = Plugin.undertaker;
        mb5.OnMissionSelected += (InventoryItem.ITEM_TYPE a) => {
            Plugin.Log.LogInfo("selected " + a);
        };
        mb5.Configure(followerInfo);
        mb5.Start();

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
        followerSelectMenu._missionInfoCardController._currentCard.MissionButtons[0].gameObject.SetActive(true);
        followerSelectMenu._missionInfoCardController._currentCard.MissionButtons[0]._titleText.text = "Give until next level (" + (followerInfo.FollowerLevel + 1) + " prestiges)";
        followerSelectMenu._missionInfoCardController._currentCard.MissionButtons[0]._amountText.text = "" + followerInfo.FollowerLevel + 1;
        followerSelectMenu._missionInfoCardController._currentCard.MissionButtons[0]._type = Plugin.prestige;
        followerSelectMenu._missionInfoCardController._currentCard.MissionButtons[0].OnMissionSelected += (InventoryItem.ITEM_TYPE a) => {
            Plugin.Log.LogInfo("selected " + a);
        };

        //reset button
        followerSelectMenu._missionInfoCardController._currentCard.MissionButtons[1].gameObject.SetActive(true);
        followerSelectMenu._missionInfoCardController._currentCard.MissionButtons[1]._titleText.text = "Reset Prestige (Returns " + (followerInfo.FollowerLevel + 1) + " prestiges)";
        followerSelectMenu._missionInfoCardController._currentCard.MissionButtons[1]._amountText.text = "" + followerInfo.FollowerLevel + 1;
        followerSelectMenu._missionInfoCardController._currentCard.MissionButtons[1]._type = InventoryItem.ITEM_TYPE.BLACK_GOLD;
        followerSelectMenu._missionInfoCardController._currentCard.MissionButtons[1].OnMissionSelected += (InventoryItem.ITEM_TYPE a) => {
            Plugin.Log.LogInfo("selected return " + a);
        };
        
        followerSelectMenu.OnMissionaryChosen += new System.Action<FollowerInfo, InventoryItem.ITEM_TYPE>((FollowerInfo fi, InventoryItem.ITEM_TYPE itemtype) => {
            Plugin.Log.LogInfo("missionary chosen" + itemtype);
            if (itemtype == InventoryItem.ITEM_TYPE.BLACK_GOLD) {
                //remove all prestige from the follower
                int totalRemoved = 0;
                foreach (InventoryItem item in fi.Inventory)
                {
                    if (item.type == (int)Plugin.prestige)
                    {   
                        totalRemoved += item.quantity;
                        fi.Inventory.Remove(item);
                    }
                }
                Inventory.AddItem(Plugin.prestige, totalRemoved);
            }
            else if (itemtype == Plugin.prestige) {
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
