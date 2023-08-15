using System.Collections.Generic;
using Lamb.UI;
using Lamb.UI.FollowerSelect;
using src.Extensions;
using SuperchargedFollowers.Structures;
using UnityEngine;

namespace SuperchargedFollowers.Interactions;
public class Interaction_Rally : Interaction
{
    public Structure Structure;
    private bool Activating = false;
    private GameObject Player;
    private float Delay = 0.04f;
    public float DistanceToTriggerDeposits = 5f;

    public StructuresData StructureInfo => this.Structure.Structure_Info;
    public RallyStructure RallyStructure => this.Structure.Brain as RallyStructure;
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
        Plugin.summoned = false;
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
                    fib.FollowerRole.text = "Currently Rallied! |  Health: " + (0.5 + fib._followerInfo.FollowerLevel * 1)  + " | Attack: " + (fib._followerInfo.FollowerLevel * 0.5);
                    fib.FollowerSpine.AnimationState.SetAnimation(0, "attack-impact-multi", true);

                }
                else {
                    fib.FollowerRole.text = "Rally | Health: " + (0.5 + fib._followerInfo.FollowerLevel * 1) + " | Attack: " + (fib._followerInfo.FollowerLevel * 0.5);
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
                    fib.FollowerRole.text = "Current Commander | Base Health: " + (1 + 3)  + " | Attack: " + (1 + 2);
                }
                else {
                    fib.FollowerRole.text = "Select As Commander | Base Health: " + (1 + 3) + " | Attack: " + (1 + 2);
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
