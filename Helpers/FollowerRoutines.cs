using System;
using System.Collections.Generic;
using MMBiomeGeneration;
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

    public IEnumerator<object> WaitForEnemy()
    {
        follower.Spine.Initialize(false);
        follower.state.CURRENT_STATE = StateMachine.State.Idle;
        follower.Spine.AnimationState.SetAnimation(0, "Conversations/idle-nice", true);

        while (!GameManager.RoomActive)
        {
            yield return null;
        }

        while (BiomeGenerator.Instance.CurrentRoom.Completed) {
            Plugin.Log.LogInfo("Room is cleared, waiting for next room");
            follower.Spine.AnimationState.SetAnimation(1, "Conversations/idle-nice", true);
            yield return new WaitForSeconds(1f);
        }

        Plugin.Log.LogInfo("room active, waiting for target ");

        if (follower.TargetObject != null) {
            Plugin.Log.LogInfo("target was not null, so reset target and try again");
            var nextTarget = this.GetRandomEnemy();
            if (nextTarget != null)
            {
                Plugin.Log.LogInfo("target found of " + nextTarget);
                follower.TargetObject = nextTarget.gameObject;
                Plugin.Log.LogInfo("Chasing");
                follower.StopAllCoroutines();
                follower.StartCoroutine(this.ChaseEnemy());
                yield break;
            }
        }


        while (follower.TargetObject == null || follower.TargetObject.GetComponent<Health>().team == Health.Team.PlayerTeam)
        {
            Plugin.Log.LogInfo("target null, waiting for target");

            try {
                var nextTarget = this.GetRandomEnemy();
                if (nextTarget != null)
                {
                    Plugin.Log.LogInfo("target found of " + nextTarget);
                    follower.TargetObject = nextTarget.gameObject;
                    Plugin.Log.LogInfo("Chasing");
                    follower.StopAllCoroutines();
                    follower.StartCoroutine(this.ChaseEnemy());
                    yield break;
                }
                
            }
            catch (Exception e) {
                Plugin.Log.LogInfo("error getting target, try again later");

            }
            yield return new WaitForSeconds(0.5f);
        }
        
        yield return null;
    }

    public IEnumerator<object> ChaseEnemy()
    {
        follower.MyState = EnemyFollower.State.WaitAndTaunt;
        follower.state.CURRENT_STATE = StateMachine.State.Idle;
        follower.Spine.AnimationState.SetAnimation(0, "Conversations/idle-nice", true);

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
                Plugin.Log.LogInfo("was null, so find new target, stop chase");
                yield return new WaitForSeconds(0.25f);
                follower.StartCoroutine(this.WaitForEnemy());
                yield break;
            }
            if (follower.damageColliderEvents != null)
                follower.damageColliderEvents.SetActive(false);

            follower.TeleportDelay -= Time.deltaTime;
            follower.AttackDelay -= Time.deltaTime;
            follower.MaxAttackDelay -= Time.deltaTime;

            if (follower.MyState == EnemyFollower.State.WaitAndTaunt)
            {
                yield return new WaitForSeconds(0.02f);
                if (follower.TargetObject == null) {
                    Plugin.Log.LogInfo("target is gone, need to find new target");
                    follower.StartCoroutine(this.WaitForEnemy());
                    yield break;
                }

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
                            try {
                                var health = follower.TargetObject.GetComponent<Health>();
                                Plugin.Log.LogInfo("Attacking now, i am attacking " + follower.TargetObject.name + " health at " + health.HP);

                                if (health.HP <= 0) {
                                    Plugin.Log.LogInfo("Attacking now, i am attacking an enemy at 0 hp, so find new target");
                                    follower.StartCoroutine(this.WaitForEnemy());
                                    yield break;
                                }
                                else {
                                    follower.health.invincible = false;
                                    follower.StopAllCoroutines();
                                    follower.StartCoroutine(this.FightPlayer(1.5f));
                                    yield break;
                                }
                            }
                            catch (Exception e) {
                                Plugin.Log.LogInfo("Attacking an null enemy " + e.Message);
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
            follower.Seperate(0.5f);
            yield return null;
        }
    }

    public IEnumerator<object> RepositionSelfIfOutside() {
        yield return new WaitForSeconds(2f);
        if (BiomeGenerator.Instance.CurrentRoom.Completed) {
            Plugin.Log.LogInfo("Follower reposition to player");
            follower.transform.position = PlayerFarming.Instance.transform.position;
        }

        // foreach (RaycastHit2D raycastHit2D in Physics2D.RaycastAll((Vector2) this.transform.position, (Vector2) (follower.transform.position - this.transform.position).normalized, Vector3.Distance(follower.transform.position, this.transform.position)))
        // {
        //     CompositeCollider2D component2 = raycastHit2D.collider.GetComponent<CompositeCollider2D>();
        //     RoomLockController component3 = raycastHit2D.collider.GetComponent<RoomLockController>();
        //     if (component2 != null || component3 != null)
        //     {
        //         break;
        //     }
        // }
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
                Plugin.Log.LogInfo("was null in fightplayer");
                follower.StartCoroutine(this.WaitForEnemy());
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
                        // follower.SimpleSpineFlash.FlashWhite(follower.state.Timer / SignPostDelay);
                        follower.state.Timer += Time.deltaTime;
                        if ((double)follower.state.Timer >= (double)SignPostDelay - (double)EnemyFollower.signPostParryWindow)
                            follower.canBeParried = true;
                        if (follower.health.team == Health.Team.PlayerTeam && follower.TargetObject != null)
                        {
                            follower.state.LookAngle = Utils.GetAngle(follower.transform.position, follower.TargetObject.transform.position);
                            follower.Spine.skeleton.ScaleX = (double)follower.state.LookAngle <= 90.0 || (double)follower.state.LookAngle >= 270.0 ? -1f : 1f;
                            follower.state.LookAngle = follower.state.facingAngle = Utils.GetAngle(follower.transform.position, follower.TargetObject.transform.position);
                        }
                        if ((double)follower.state.Timer >= (double)SignPostDelay)
                        {
                            // follower.SimpleSpineFlash.FlashWhite(false);
                            // CameraManager.shakeCamera(0.4f, follower.state.LookAngle);
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
                            Loop = false;
                            // follower.SimpleSpineFlash.FlashWhite(false);
                            break;
                        }
                        break;
                }
                yield return null;
            }
            yield return null;
        }
        Plugin.Log.LogInfo("starting to chase again");
        follower.StartCoroutine(this.ChaseEnemy());
    }
}
