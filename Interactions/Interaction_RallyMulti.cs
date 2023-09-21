using System.Collections.Generic;
using Lamb.UI;
using Lamb.UI.FollowerSelect;
using src.Extensions;
using SuperchargedFollowers.Structures;
using UnityEngine;

namespace SuperchargedFollowers.Interactions;
public class Interaction_RallyMulti : Interaction
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
        this.secondaryLabel = "Unrally All followers";
        this.label = "Rally All Followers";
    }

    private void Start()
    {
        this.HasSecondaryInteraction = true;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        Plugin.Log.LogInfo("RallyMulti OnEnable");
        Structure = GetComponentInParent<Transform>().GetComponent<Structure>();
    }

    public override void OnInteract(StateMachine state)
    {
        Plugin.summoned = false;
        if (this.Activating) return;
        base.OnInteract(state);
        this.Activating = true;

        List<FollowerInfo> blackList = new();

        foreach (FollowerInfo follower in DataManager.Instance.Followers)
        {
            if (follower.CursedState != Thought.OldAge)
                blackList.Add(follower);
        }

        Plugin.summonList.Clear();
        Plugin.summonList.AddRange(blackList);
        NotificationCentreScreen.Play("Rallied " + blackList.Count +  " followers!");
        this.Activating = false;
    }

    public override void OnSecondaryInteract(StateMachine state)
    {
        if (this.Activating) return;
        base.OnSecondaryInteract(state);
        this.Activating = true;

        Plugin.summonList.Clear();
        NotificationCentreScreen.Play("Unrallied all followers!");


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
}
