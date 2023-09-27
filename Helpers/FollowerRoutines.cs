using System;
using System.Collections.Generic;
using MMBiomeGeneration;
using UnityEngine;

namespace SuperchargedFollowers.Helpers;
public class FollowerRoutines : MonoBehaviour
{
    EnemyFollower follower;
    SuperchargedFollowersAIState myState = SuperchargedFollowersAIState.Idle;
    
    //start method
    public void Start()
    {
        //get the follower component
        this.follower = GetComponent<EnemyFollower>();
        Plugin.Log.LogInfo("follower is " + follower);
    }


    public Health GetRandomEnemy() {
        Plugin.Log.LogInfo("Attempting to find random enemy");
        List<Health> enemies = Health.team2;
        int maxAttempts = 10;
        int attempts = 0;
        if (enemies.Count > 0) {
            int random = UnityEngine.Random.Range(0, enemies.Count);
            while (enemies[random].HP <= 0 && attempts < maxAttempts) {
                Plugin.Log.LogInfo("Attempting to find random enemy again as targeted a dead enemy");
                random = UnityEngine.Random.Range(0, enemies.Count);
                attempts++;
            }
            return enemies[random];
        }
        else {
            //target player if no mor enemy
            Plugin.Log.LogInfo("Returning none");
            return null;
        }
    }

    public IEnumerator<object> RunFollowerAI() {

        //WAIT FOR ROOM TO BE READY
        while (!GameManager.RoomActive)
        {
            yield return null;
        }

        //INITIALIZE FOLLOWER ANIMATION
        follower.Spine.Initialize(false);
        follower.state.CURRENT_STATE = StateMachine.State.Idle;
        follower.Spine.AnimationState.SetAnimation(0, "Conversations/idle-nice", true);
        Follower followerData = follower.GetComponentInParent<Follower>();

        //SET MYSTATE TO SEARCH
        this.myState = SuperchargedFollowersAIState.Searching;
        Plugin.Log.LogInfo("[STATE: " + this.myState + "]" + followerData.Brain.Info.Name + " is ready for battle");

        //SWITCH CASE FOR ALL STATES SuperchargedFollowersAIState
        bool Loop = true;
        while(Loop) {
            //WAIT FOR ROOM TO BE READY
            while (!GameManager.RoomActive)
            {
                yield return null;
            }

            //WAIT IF ROOM IS CLEARED
            while (Health.team2.Count == 0) {
                Plugin.Log.LogInfo("Room is cleared, waiting for next room");
                follower.Spine.AnimationState.SetAnimation(1, "Conversations/idle-nice", true);
                this.myState = SuperchargedFollowersAIState.Idle;
                yield return new WaitForSeconds(1f);
            }

            //DO STATE ACTIONS
            switch(this.myState) {
                case SuperchargedFollowersAIState.Searching:
                    Plugin.Log.LogInfo("[STATE: " + this.myState + "]" + followerData.Brain.Info.Name + " is searching for enemies");
                    var nextTarget = this.GetRandomEnemy();
                    if (nextTarget != null && nextTarget.team != Health.Team.PlayerTeam)
                    {
                        Plugin.Log.LogInfo("[STATE: " + this.myState + "]" + followerData.Brain.Info.Name + " finds " + nextTarget);
                        follower.TargetObject = nextTarget.gameObject;
                        this.myState = SuperchargedFollowersAIState.Chasing;
                    }
                    else {
                        this.myState = SuperchargedFollowersAIState.Idle;
                        yield return new WaitForSeconds(0.05f);
                    }
                    yield return null;
                    break;
                
                case SuperchargedFollowersAIState.Chasing:
                    Plugin.Log.LogInfo("[STATE: " + this.myState + "]" + followerData.Brain.Info.Name + " is chasing enemies");
                    follower.MyState = EnemyFollower.State.WaitAndTaunt;
                    follower.state.CURRENT_STATE = StateMachine.State.Idle;
                    follower.Spine.AnimationState.SetAnimation(0, "Conversations/idle-nice", true);

                    //ATTACK SPEED
                    follower.AttackDelay = UnityEngine.Random.Range(follower.AttackDelayRandomRange.x, follower.AttackDelayRandomRange.y);
                    if (follower.health.HasShield)
                        follower.AttackDelay = 2.5f;
                    follower.MaxAttackDelay = UnityEngine.Random.Range(follower.MaxAttackDelayRandomRange.x, follower.MaxAttackDelayRandomRange.y);

                    bool chasing = true;

                    while(chasing) {
                        if (follower.TargetObject == null || follower.TargetObject.GetComponent<Health>().team == Health.Team.PlayerTeam)
                        {
                            Plugin.Log.LogInfo("[STATE: " + this.myState + "]" + followerData.Brain.Info.Name + " was chasing, now null");
                            yield return new WaitForSeconds(0.20f);
                            this.myState = SuperchargedFollowersAIState.Searching;
                            chasing = false;
                            break;
                        }

                        if (follower.damageColliderEvents != null)
                            follower.damageColliderEvents.SetActive(false);

                        follower.TeleportDelay -= Time.deltaTime;
                        follower.AttackDelay -= Time.deltaTime;
                        follower.MaxAttackDelay -= Time.deltaTime;

                        if (follower.MyState == EnemyFollower.State.WaitAndTaunt) {
                            yield return new WaitForSeconds(0.02f);
                            if (follower.TargetObject == null) {
                                Plugin.Log.LogInfo("[STATE: " + this.myState + "]" + followerData.Brain.Info.Name + " was chasing, now null after waiting");
                                this.myState = SuperchargedFollowersAIState.Searching;
                                chasing = false;
                                break;
                            }

                            if (follower.Spine.AnimationName != "roll-stop" && follower.state.CURRENT_STATE == StateMachine.State.Moving && follower.Spine.AnimationName != "run")
                                follower.Spine.AnimationState.SetAnimation(1, "run", true);

                            follower.state.LookAngle = Utils.GetAngle(follower.transform.position, follower.TargetObject.transform.position);
                            follower.Spine.skeleton.ScaleX = follower.state.LookAngle <= 90.0 || follower.state.LookAngle >= 270.0 ? -1f : 1f;
                            if (follower.state.CURRENT_STATE == StateMachine.State.Idle && (double)(follower.RepathTimer -= Time.deltaTime) < 0.0)
                            {
                                Plugin.Log.LogInfo("[STATE: " + this.myState + "]" + followerData.Brain.Info.Name + " is starting to attack");
                                if (follower.CustomAttackLogic())
                                    break;
                                
                                if (follower.MaxAttackDelay < 0.0 || (double)Vector3.Distance(follower.transform.position, follower.TargetObject.transform.position) < (double)follower.AttackWithinRange)
                                {
                                    if (follower.ChargeAndAttack && (follower.MaxAttackDelay < 0.0 || follower.AttackDelay < 0.0))
                                    {
                                        try {
                                            var health = follower.TargetObject.GetComponent<Health>();
                                            Plugin.Log.LogInfo("[STATE: " + this.myState + "]" + followerData.Brain.Info.Name + " attacked " + follower.TargetObject.name + " health at " + health.HP);

                                            if (health.HP <= 0) {
                                                Plugin.Log.LogInfo("[STATE: " + this.myState + "]" + followerData.Brain.Info.Name + " attacked at 0 hp, so find new target");
                                                this.myState = SuperchargedFollowersAIState.Searching;
                                                chasing = false;                                                
                                                break;
                                            }
                                            else {
                                                follower.health.invincible = false;
                                                this.myState = SuperchargedFollowersAIState.Attacking;
                                                chasing = false;    
                                                break;
                                            }
                                        }
                                        catch (Exception e) {
                                            Plugin.Log.LogInfo("[STATE: " + this.myState + "]" + followerData.Brain.Info.Name + " attacked a null enemy, reset to Searching");
                                            Plugin.Log.LogInfo("Attacking an null enemy " + e.Message);
                                            this.myState = SuperchargedFollowersAIState.Searching;
                                            chasing = false;     
                                            break;
                                        }
                                    }
                                    else if (!follower.health.HasShield)
                                    {
                                        follower.Angle = (float)(((double)Utils.GetAngle(follower.TargetObject.transform.position, follower.transform.position) + (double)UnityEngine.Random.Range(-20, 20)) * (Math.PI / 180.0));
                                        follower.TargetPosition = follower.TargetObject.transform.position + new Vector3(follower.MaintainTargetDistance * Mathf.Cos(follower.Angle), follower.MaintainTargetDistance * Mathf.Sin(follower.Angle));
                                        follower.FindPath(follower.TargetPosition);
                                    }
                                }
                                else if (Vector3.Distance(follower.transform.position, follower.TargetObject.transform.position) > (double)follower.MoveCloserDistance + (follower.health.HasShield ? 0.0 : 1.0))
                                {
                                    follower.Angle = (float)(((double)Utils.GetAngle(follower.TargetObject.transform.position, follower.transform.position) + (double)UnityEngine.Random.Range(-20, 20)) * (Math.PI / 180.0));
                                    follower.TargetPosition = follower.TargetObject.transform.position + new Vector3(follower.MaintainTargetDistance * Mathf.Cos(follower.Angle), follower.MaintainTargetDistance * Mathf.Sin(follower.Angle));
                                    follower.FindPath(follower.TargetPosition);
                                }
                            }
                        }
                        yield return null;
                    }
                    yield return null;
                    break;
                
                case SuperchargedFollowersAIState.Attacking:
                    Plugin.Log.LogInfo("[STATE: " + this.myState + "]" + followerData.Brain.Info.Name + " is attacking enemies");

                    follower.MyState = EnemyFollower.State.Attacking;
                    follower.UsePathing = true;
                    follower.givePath(follower.TargetObject.transform.position);
                    follower.Spine.AnimationState.SetAnimation(1, "run", true);
                    follower.RepathTimer = 0.0f;

                    int NumAttacks = follower.DoubleAttack ? 2 : 1;
                    int AttackCount = 1;
                    float attackDistance = 1.5f;
                    float MaxAttackSpeed = 15f;
                    float AttackSpeed = MaxAttackSpeed;
                    bool attacking = true;
                    float SignPostDelay = 0.5f;

                    while (attacking)
                    {
                        follower.Seperate(0.5f);
                        if (follower.TargetObject == null)
                        {
                            yield return new WaitForSeconds(0.2f);
                            Plugin.Log.LogInfo("[STATE: " + this.myState + "]" + followerData.Brain.Info.Name + " lost enemy, reset to search");
                            this.myState = SuperchargedFollowersAIState.Searching;
                            attacking = false;
                            break;
                        }
                        
                        if (follower.state.CURRENT_STATE == StateMachine.State.Idle)
                            follower.state.CURRENT_STATE = StateMachine.State.Moving;
                        
                        switch (follower.state.CURRENT_STATE)
                        {
                            case StateMachine.State.Moving:
                                follower.state.LookAngle = Utils.GetAngle(follower.transform.position, follower.TargetObject.transform.position);
                                follower.Spine.skeleton.ScaleX = follower.state.LookAngle <= 90.0 || follower.state.LookAngle >= 270.0 ? -1f : 1f;
                                follower.state.LookAngle = follower.state.facingAngle = Utils.GetAngle(follower.transform.position, follower.TargetObject.transform.position);
                                
                                if ((double)Vector2.Distance((Vector2)follower.transform.position, (Vector2)follower.TargetObject.transform.position) < (double)attackDistance)
                                {
                                    follower.state.CURRENT_STATE = StateMachine.State.SignPostAttack;
                                    string animationName = "attack-charge";
                                    follower.Spine.AnimationState.SetAnimation(1, animationName, false);
                                }

                                else
                                {
                                    if ((double)(follower.RepathTimer += Time.deltaTime) > 0.20000000298023224)
                                    {
                                        follower.RepathTimer = 0.0f;
                                        follower.givePath(follower.TargetObject.transform.position);
                                    }
                                    if (follower.damageColliderEvents != null)
                                    {
                                        if ((double)follower.state.Timer < 0.20000000298023224 && !follower.health.WasJustParried)
                                            follower.damageColliderEvents.SetActive(true);
                                        else
                                            follower.damageColliderEvents.SetActive(false);
                                    }
                                }
                                if (follower.damageColliderEvents != null)
                                {
                                    follower.damageColliderEvents.SetActive(false);
                                    break;
                                }
                                break;

                            case StateMachine.State.SignPostAttack:
                                if (follower.damageColliderEvents != null)
                                    follower.damageColliderEvents.SetActive(false);

                                follower.state.Timer += Time.deltaTime;
                                if ((double)follower.state.Timer >= (double)SignPostDelay - (double)EnemyFollower.signPostParryWindow)
                                    follower.canBeParried = true;
                                if (follower.health.team == Health.Team.PlayerTeam && follower.TargetObject != null)
                                {
                                    follower.state.LookAngle = Utils.GetAngle(follower.transform.position, follower.TargetObject.transform.position);
                                    follower.Spine.skeleton.ScaleX = follower.state.LookAngle <= 90.0 || follower.state.LookAngle >= 270.0 ? -1f : 1f;
                                    follower.state.LookAngle = follower.state.facingAngle = Utils.GetAngle(follower.transform.position, follower.TargetObject.transform.position);
                                }
                                if ((double)follower.state.Timer >= (double)SignPostDelay)
                                {

                                    follower.state.CURRENT_STATE = StateMachine.State.RecoverFromAttack;
                                    // follower.speed = AttackSpeed * 0.0166666675f;
                                    string animationName = follower.variant == 0 ? "attack-impact" : "attack-impact-multi";
                                    follower.Spine.AnimationState.SetAnimation(1, animationName, false);
                                    follower.canBeParried = true;
                                    follower.StartCoroutine(follower.EnableDamageCollider(0.0f));
                                    if (follower.variant == 1) {
                                        follower.StartCoroutine(follower.EnableDamageCollider(0.7f));
                                        follower.StartCoroutine(follower.EnableDamageCollider(1.4f));
                                    }
                                    if (!string.IsNullOrEmpty(follower.attackSoundPath))
                                    {
                                        AudioManager.Instance.PlayOneShot(follower.attackSoundPath, follower.transform.position);
                                        break;
                                    }
                                    break;
                                }
                                break;
                            case StateMachine.State.RecoverFromAttack:
                                if ((double)AttackSpeed > 0.0)
                                    AttackSpeed -= 1f * GameManager.DeltaTime;
                                // follower.speed = AttackSpeed * Time.deltaTime;
                                // follower.SimpleSpineFlash.FlashWhite(false);
                                follower.Spine.AnimationState.SetAnimation(0, "Conversations/idle-nice", true);
                                follower.canBeParried = follower.state.Timer <= EnemyFollower.attackParryWindow;
                                if ((double)(follower.state.Timer += Time.deltaTime) >= 0.6)
                                {
                                    if (++AttackCount <= NumAttacks)
                                    {
                                        AttackSpeed = MaxAttackSpeed + (3 - NumAttacks) * 2;
                                        follower.state.CURRENT_STATE = StateMachine.State.SignPostAttack;
                                        follower.variant = UnityEngine.Random.Range(0, 2);
                                        string animationName = "attack-charge";
                                        follower.Spine.AnimationState.SetAnimation(1, animationName, false);
                                        SignPostDelay = 0.3f;
                                        break;
                                    }
                                    attacking = false;
                                    break;
                                }
                                break;
                        }
                        yield return null;
                    }
                    
                    Plugin.Log.LogInfo("[STATE: " + this.myState + "]" + followerData.Brain.Info.Name + " Attack complete, Chase enemy again.");
                    this.myState = SuperchargedFollowersAIState.Chasing;
                    yield return null;
                    break;
                
                case SuperchargedFollowersAIState.Idle:
                    Plugin.Log.LogInfo("[STATE: " + this.myState + "]" + followerData.Brain.Info.Name + " was idle, will search now");
                    this.myState = SuperchargedFollowersAIState.Searching;
                    yield return null;
                    break;
                
                default:
                    Plugin.Log.LogInfo("[STATE: " + this.myState + "]" + followerData.Brain.Info.Name + " is fallback to default state, will search again");
                    this.myState = SuperchargedFollowersAIState.Searching;
                    yield return null;
                    break;
            }
            yield return null;
        }
        yield return null;
    }

    public IEnumerator<object> RepositionSelfIfOutside() {
        yield return new WaitForSeconds(2f);
        if (BiomeGenerator.Instance.CurrentRoom.Completed) {
            Plugin.Log.LogInfo("Follower reposition to player");
            follower.transform.position = PlayerFarming.Instance.transform.position;
        }
    }

}
