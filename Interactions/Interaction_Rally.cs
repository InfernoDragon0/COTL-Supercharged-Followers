using System.Collections.Generic;
using Lamb.UI;
using Lamb.UI.FollowerSelect;
using src.Extensions;
using SuperchargedFollowers;
using UnityEngine;

namespace Namespace;
public class Interaction_Rally : Interaction
{
    public Structure Structure;
    private bool Activating = false;
    private GameObject Player;
    private float Delay = 0.04f;
    public float DistanceToTriggerDeposits = 5f;

    public StructuresData StructureInfo => this.Structure.Structure_Info;
    public RallyStructure AIOFarmer => this.Structure.Brain as RallyStructure;
    public override void GetLabel()
    {
        this.secondaryLabel = "Select Commander";
        this.label = "Rally Followers";
    }

    private void Start()
    {
        this.HasSecondaryInteraction = true;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        Plugin.Log.LogInfo("Rally OnEnable");
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
                if (Plugin.summonList.Contains(fib._followerInfo)) {
                    fib.FollowerRole.text = "Currently Rallied! |  Health: " + (50 + fib._followerInfo.FollowerLevel * 20)  + " | Attack: " + (fib._followerInfo.FollowerLevel * 2);
                }
                fib.FollowerRole.text = "Rally | Health: " + (50 + fib._followerInfo.FollowerLevel * 20) + " | Attack: " + (fib._followerInfo.FollowerLevel * 2);
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

        followerSelectMenu.OnFollowerSelected += new System.Action<FollowerInfo>(this.OnCommanderChosen);
        followerSelectMenu.OnShown += new System.Action(() =>
        {
            foreach (FollowerInformationBox fib in followerSelectMenu._followerInfoBoxes)
            {
                if (Plugin.commander == fib._followerInfo) {
                    fib.FollowerRole.text = "Current Commander |  Health: " + (150 + fib._followerInfo.FollowerLevel * 40)  + " | Attack: " + (fib._followerInfo.FollowerLevel * 4);
                }
                fib.FollowerRole.text = "Select As Commander | Health: " + (150 + fib._followerInfo.FollowerLevel * 40) + " | Attack: " + (fib._followerInfo.FollowerLevel * 4);
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

        this.Activating = false;
    }

    public void OnHidden() {
        this.Activating = false;
        HUD_Manager.Instance.Show();
        GameManager.GetInstance().OnConversationEnd();
    }

    public void OnFollowerChosen(FollowerInfo followerInfo) {
        if (Plugin.summonList.Contains(followerInfo)) {
            Plugin.summonList.Remove(followerInfo);
        } else {
            Plugin.summonList.Add(followerInfo);
        }
    }

    public void OnCommanderChosen(FollowerInfo followerInfo) {
        Plugin.commander = followerInfo;
    }
}
