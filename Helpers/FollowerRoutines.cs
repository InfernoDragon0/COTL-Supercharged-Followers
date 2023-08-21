using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperchargedFollowers.Helpers;
public class FollowerRoutines : MonoBehaviour
{
    EnemyFollower follower;
    //start method
    public void Start()
    {
        //get the follower component
        this.follower = GetComponent<EnemyFollower>();
        Plugin.Log.LogInfo("follower is " + follower);
    }

    public IEnumerator<object> WaitForEnemy()
    {
        Plugin.Log.LogInfo("got wait for enemy follower is " + follower);
        follower.Spine.Initialize(false);
        follower.state.CURRENT_STATE = StateMachine.State.Idle;
        follower.Spine.AnimationState.SetAnimation(0, "Conversations/idle-nice", true);
        Plugin.Log.LogInfo("waiting for room active");

        while (!GameManager.RoomActive)
        {
            yield return null;
        }

        Plugin.Log.LogInfo("checkif target null");

        while (follower.TargetObject == null)
        {
            Plugin.Log.LogInfo("target null, get closest target");

            var nextTarget = follower.GetClosestTarget(true);
            Plugin.Log.LogInfo("target found of " + nextTarget);
            if (nextTarget != null)
            {
                follower.TargetObject = nextTarget.gameObject;
                Plugin.Log.LogInfo("target found of " + nextTarget.team);
            }
            else
            {
                Plugin.Log.LogInfo("pathing to player");
                try
                {
                    follower.givePath(PlayerFarming.Instance.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle * 2f);
                    follower.followerTimestamp = Time.time + 0.25f;
                }
                catch (Exception e)
                {
                    Plugin.Log.LogInfo("error pathing to player, try again later: " + e.Message);
                }

                yield return new WaitForSeconds(2f);

            }

        }
        Plugin.Log.LogInfo("check in range");

        bool InRange = false;
        while (!InRange)
        {
            Plugin.Log.LogInfo("checking in range");

            if (follower.TargetObject == null || follower.TargetObject.GetComponent<Health>().team == Health.Team.PlayerTeam)
            {
                Plugin.Log.LogInfo("target lost or was targeting player, trying again");
                follower.StartCoroutine(follower.WaitForTarget());
                yield return null;
            }
            Plugin.Log.LogInfo("checking in range 2");
            if ((double)Vector3.Distance(follower.TargetObject.transform.position, follower.transform.position) <= follower.VisionRange)
                InRange = true;
        }
        Plugin.Log.LogInfo("Chasing");
        follower.StartCoroutine(this.ChaseEnemy());
        yield return null;
    }

    public IEnumerator<object> ChaseEnemy()
    {
        follower.MyState = EnemyFollower.State.WaitAndTaunt;
        follower.state.CURRENT_STATE = StateMachine.State.Idle;

        //ATTACK SPEED BALANCING TODO: change values based on follower level
        follower.AttackDelay = UnityEngine.Random.Range(follower.AttackDelayRandomRange.x, follower.AttackDelayRandomRange.y);
        if (follower.health.HasShield)
            follower.AttackDelay = 2.5f;
        follower.MaxAttackDelay = UnityEngine.Random.Range(follower.MaxAttackDelayRandomRange.x, follower.MaxAttackDelayRandomRange.y);

        bool Loop = true;

        while (Loop)
        {
            if (follower.TargetObject == null || follower.TargetObject.GetComponent<Health>().team == Health.Team.PlayerTeam)
            {
                Plugin.Log.LogInfo("was null, so find new target");
                yield return new WaitForSeconds(0.35f);
                if (follower.GetClosestTarget(true) != null)
                {
                    follower.TargetObject = follower.GetClosestTarget(true).gameObject;
                    Plugin.Log.LogInfo("was null, so found new target of " + follower.GetClosestTarget(true).team);
                }
                else
                {
                    Plugin.Log.LogInfo("target closest was null, so found new target of playerfarming");
                    follower.givePath(PlayerFarming.Instance.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle * 2f);
                    follower.followerTimestamp = Time.time + 0.25f;
                    yield return null;
                    continue;

                }
            }
            if (follower.damageColliderEvents != null)
                follower.damageColliderEvents.SetActive(false);

            follower.TeleportDelay -= Time.deltaTime;
            follower.AttackDelay -= Time.deltaTime;
            follower.MaxAttackDelay -= Time.deltaTime;

            if (follower.MyState == EnemyFollower.State.WaitAndTaunt)
            {
                Plugin.Log.LogInfo("wait and taunting");

                if (follower.Spine.AnimationName != "roll-stop" && follower.state.CURRENT_STATE == StateMachine.State.Moving && follower.Spine.AnimationName != "run")
                    follower.Spine.AnimationState.SetAnimation(1, "run", true);

                follower.state.LookAngle = Utils.GetAngle(follower.transform.position, follower.TargetObject.transform.position);
                follower.Spine.skeleton.ScaleX = (double)follower.state.LookAngle <= 90.0 || (double)follower.state.LookAngle >= 270.0 ? -1f : 1f;
                if (follower.state.CURRENT_STATE == StateMachine.State.Idle && (double)(follower.RepathTimer -= Time.deltaTime) < 0.0)
                {
                    Plugin.Log.LogInfo("time to attack");
                    if (follower.CustomAttackLogic())
                        break;
                    if (follower.MaxAttackDelay < 0.0 || (double)Vector3.Distance(follower.transform.position, follower.TargetObject.transform.position) < (double)follower.AttackWithinRange)
                    {
                        if (follower.ChargeAndAttack && (follower.MaxAttackDelay < 0.0 || follower.AttackDelay < 0.0))
                        {
                            Plugin.Log.LogInfo("Attacking now");
                            follower.health.invincible = false;
                            follower.StopAllCoroutines();
                            yield return follower.StartCoroutine(this.FightPlayer(1.5f));
                            Plugin.Log.LogInfo("Attacking complete");
                            /*Loop = false; //todo: if false?*/
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
            follower.Seperate(0.5f);
            yield return null;
        }
    }

    public IEnumerator<object> FightPlayer(float attackDistance = 1.5f)
    {
        follower.MyState = EnemyFollower.State.Attacking;
        follower.UsePathing = true;
        follower.givePath(follower.TargetObject.transform.position);
        follower.Spine.AnimationState.SetAnimation(1, "run", true);
        follower.RepathTimer = 0.0f;
        int NumAttacks = follower.DoubleAttack ? 2 : 1;
        int AttackCount = 1;
        float MaxAttackSpeed = 15f;
        float AttackSpeed = MaxAttackSpeed;
        bool Loop = true;
        float SignPostDelay = 0.5f;
        while (Loop)
        {
            follower.Seperate(0.5f);
            if (follower.TargetObject == null)
            {
                yield return new WaitForSeconds(0.3f);
                follower.StartCoroutine(follower.WaitForTarget());
                yield break;
            }
            else
            {
                if (follower.state.CURRENT_STATE == StateMachine.State.Idle)
                    follower.state.CURRENT_STATE = StateMachine.State.Moving;
                switch (follower.state.CURRENT_STATE)
                {
                    case StateMachine.State.Moving:
                        follower.state.LookAngle = Utils.GetAngle(follower.transform.position, follower.TargetObject.transform.position);
                        follower.Spine.skeleton.ScaleX = (double)follower.state.LookAngle <= 90.0 || (double)follower.state.LookAngle >= 270.0 ? -1f : 1f;
                        follower.state.LookAngle = follower.state.facingAngle = Utils.GetAngle(follower.transform.position, follower.TargetObject.transform.position);
                        if ((double)Vector2.Distance((Vector2)follower.transform.position, (Vector2)follower.TargetObject.transform.position) < (double)attackDistance)
                        {
                            follower.state.CURRENT_STATE = StateMachine.State.SignPostAttack;
                            follower.variant = UnityEngine.Random.Range(0, 2);
                            string animationName = follower.variant == 0 ? "attack-charge" : "attack-charge2";
                            follower.Spine.AnimationState.SetAnimation(1, animationName, false);
                        }
                        else
                        {
                            if ((double)(follower.RepathTimer += Time.deltaTime) > 0.20000000298023224)
                            {
                                follower.RepathTimer = 0.0f;
                                follower.givePath(follower.TargetObject.transform.position);
                            }
                            if ((UnityEngine.Object)follower.damageColliderEvents != (UnityEngine.Object)null)
                            {
                                if ((double)follower.state.Timer < 0.20000000298023224 && !follower.health.WasJustParried)
                                    follower.damageColliderEvents.SetActive(true);
                                else
                                    follower.damageColliderEvents.SetActive(false);
                            }
                        }
                        if ((UnityEngine.Object)follower.damageColliderEvents != (UnityEngine.Object)null)
                        {
                            follower.damageColliderEvents.SetActive(false);
                            break;
                        }
                        break;
                    case StateMachine.State.SignPostAttack:
                        if ((UnityEngine.Object)follower.damageColliderEvents != (UnityEngine.Object)null)
                            follower.damageColliderEvents.SetActive(false);
                        follower.SimpleSpineFlash.FlashWhite(follower.state.Timer / SignPostDelay);
                        follower.state.Timer += Time.deltaTime;
                        if ((double)follower.state.Timer >= (double)SignPostDelay - (double)EnemyFollower.signPostParryWindow)
                            follower.canBeParried = true;
                        if (follower.health.team == Health.Team.PlayerTeam && (UnityEngine.Object)follower.TargetObject != (UnityEngine.Object)null)
                        {
                            follower.state.LookAngle = Utils.GetAngle(follower.transform.position, follower.TargetObject.transform.position);
                            follower.Spine.skeleton.ScaleX = (double)follower.state.LookAngle <= 90.0 || (double)follower.state.LookAngle >= 270.0 ? -1f : 1f;
                            follower.state.LookAngle = follower.state.facingAngle = Utils.GetAngle(follower.transform.position, follower.TargetObject.transform.position);
                        }
                        if ((double)follower.state.Timer >= (double)SignPostDelay)
                        {
                            follower.SimpleSpineFlash.FlashWhite(false);
                            CameraManager.shakeCamera(0.4f, follower.state.LookAngle);
                            follower.state.CURRENT_STATE = StateMachine.State.RecoverFromAttack;
                            // follower.speed = AttackSpeed * 0.0166666675f;
                            string animationName = follower.variant == 0 ? "attack-impact" : "attack-impact2";
                            follower.Spine.AnimationState.SetAnimation(1, animationName, false);
                            follower.canBeParried = true;
                            follower.StartCoroutine(follower.EnableDamageCollider(0.0f));
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
                        follower.SimpleSpineFlash.FlashWhite(false);
                        follower.canBeParried = (double)follower.state.Timer <= (double)EnemyFollower.attackParryWindow;
                        if ((double)(follower.state.Timer += Time.deltaTime) >= (AttackCount + 1 <= NumAttacks ? 0.5 : 1.0))
                        {
                            if (++AttackCount <= NumAttacks)
                            {
                                AttackSpeed = MaxAttackSpeed + (float)((3 - NumAttacks) * 2);
                                follower.state.CURRENT_STATE = StateMachine.State.SignPostAttack;
                                follower.variant = UnityEngine.Random.Range(0, 2);
                                string animationName = "attack-charge";
                                follower.Spine.AnimationState.SetAnimation(1, animationName, false);
                                SignPostDelay = 0.3f;
                                break;
                            }
                            Loop = false;
                            follower.SimpleSpineFlash.FlashWhite(false);
                            break;
                        }
                        break;
                }
                yield return (object)null;
            }
        }
        follower.StartCoroutine(follower.ChasePlayer());
    }
}
