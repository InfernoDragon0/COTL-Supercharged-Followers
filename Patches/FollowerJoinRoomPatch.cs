using DG.Tweening;
using HarmonyLib;
using MMBiomeGeneration;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperchargedFollowers.Patches
{
    [HarmonyPatch]
    internal class FollowerJoinRoomPatch
    {
        private static FollowerManager.SpawnedFollower SpawnAllyFollower(FollowerInfo followerInfo, Vector3 position)
        {
            FollowerManager.SpawnedFollower spawnedFollower = FollowerManager.SpawnCopyFollower(FollowerManager.CombatFollowerPrefab, followerInfo, position, PlayerFarming.Instance.transform.parent, BiomeGenerator.Instance.DungeonLocation);
            spawnedFollower.Follower.State.CURRENT_STATE = StateMachine.State.CustomAnimation;
            spawnedFollower.Follower.Spine.AnimationState.SetAnimation(1, "spawn-in", false);
            spawnedFollower.Follower.Spine.AnimationState.AddAnimation(1, "Reactions/react-worried1", true, 0.0f);
            // spawnedFollower.Follower.Spine.AnimationState.AddAnimation(1, "Conversations/idle-hate", true, 0.0f);
            spawnedFollower.Follower.GetComponent<EnemyFollower>().enabled = true;

            var modifiedFollower = spawnedFollower.Follower.GetComponent<EnemyFollower>();
            var prestigeBonus = Helpers.Bonuses.GetPrestigeBonuses(spawnedFollower.FollowerBrain._directInfoAccess);
            var classBonus = Helpers.Bonuses.GetClassBonuses(spawnedFollower.FollowerBrain._directInfoAccess);
            var commanderBonus = Helpers.Bonuses.GetCommanderBonuses(spawnedFollower.FollowerBrain._directInfoAccess);
            var necklaceBonus = Helpers.Bonuses.GetNecklaceBonuses(spawnedFollower.FollowerBrain._directInfoAccess);

            //SCALING
            if (followerInfo == Plugin.commander) {
                spawnedFollower.Follower.transform.DOScale(1f + commanderBonus.SizeBonus, 0.25f).SetEase(Ease.OutBounce);
            }
            
            // DAMAGE BALANCING TODO:check if commander TODO: Prestiges
            modifiedFollower.Damage = 0.5f + prestigeBonus.AttackBonus + classBonus.AttackBonus + commanderBonus.AttackBonus + necklaceBonus.AttackBonus;

            modifiedFollower.speed = 0.08f + prestigeBonus.MovementSpeedBonus + classBonus.MovementSpeedBonus + commanderBonus.MovementSpeedBonus + necklaceBonus.MovementSpeedBonus;
            modifiedFollower.maxSpeed = 0.08f + prestigeBonus.MovementSpeedBonus + classBonus.MovementSpeedBonus + commanderBonus.MovementSpeedBonus + necklaceBonus.MovementSpeedBonus;
            // HEALTH BALANCING TODO:check if commander
            Health component = spawnedFollower.Follower.GetComponent<Health>();
            component.totalHP = 10f + prestigeBonus.HealthBonus + classBonus.HealthBonus + commanderBonus.HealthBonus + necklaceBonus.HealthBonus;
            component.HP = component.totalHP;

            Plugin.tempSummoned.Add(spawnedFollower.Follower);

            //set to ally
            component.team = Health.Team.PlayerTeam;
            component.isPlayerAlly = true;
            return spawnedFollower;
        }

        private static List<FollowerInfo> RandomFollowers()
        {
            List<FollowerInfo> possibleFollowers = new List<FollowerInfo>();
            List<FollowerInfo> summonedFollowers = new List<FollowerInfo>();
            foreach (FollowerInfo follower in DataManager.Instance.Followers)
            {
                if (follower.CursedState == Thought.None && !FollowerManager.FollowerLocked(follower.ID))
                    possibleFollowers.Add(follower);
            }
            if (possibleFollowers.Count > 0)
            {
                for (int i = 0; i < 5 && possibleFollowers.Count != 0; ++i)
                {
                    FollowerInfo followerInfo = possibleFollowers[UnityEngine.Random.Range(0, possibleFollowers.Count)];
                    summonedFollowers.Add(followerInfo);
                    possibleFollowers.Remove(followerInfo);
                }
            }
            return summonedFollowers;
        }

        [HarmonyPatch(typeof(Door), nameof(Door.ChangeRoom))]
        [HarmonyPrefix]
        public static bool Door_ChangeRoom()
        {
            if (Plugin.summoned)
            {
                return true;
            }
            Plugin.summoned = true;
            // Plugin.summonList = RandomFollowers();
            Plugin.Log.LogInfo("Summoning followers");
            for (int i = 0; i <  Plugin.summonList.Count; i++)
            {
                SpawnAllyFollower(Plugin.summonList[i], PlayerFarming.Instance.transform.position);
            }

            RoomLockController.OnRoomCleared += OnRoomCleared;

            return true;
        }

        [HarmonyPatch(typeof(Door), nameof(Door.OnTriggerEnter2D))] //*** THIS IS TEMPORARY, change to Health.DealDamage
        [HarmonyPrefix]
        public static bool Door_OnTriggerEnter2D(Door __instance)
        {
            //check the roomtype
            Plugin.Log.LogInfo(__instance.ConnectionType);
            __instance.Used = false;
            return true;
        }

        [HarmonyPatch(typeof(Health), nameof(Health.DealDamage))] //*** THIS IS TEMPORARY, change to Health.DealDamage
        [HarmonyPrefix]
        public static bool Health_DealDamage(Health __instance, GameObject Attacker)
        {
            Health componentInParent = Attacker.GetComponent<Health>();
            if (componentInParent == null) { return true; }
            if (componentInParent.team == Health.Team.PlayerTeam && __instance.team == Health.Team.PlayerTeam)
            {
                Plugin.Log.LogInfo("No attack self teams");
                return false;
            }

            return true;
        }

        //next we patch the chaseplayer to chase enemies instead
        [HarmonyPatch(typeof(EnemyFollower), nameof(EnemyFollower.WaitForTarget))] //*** THIS IS TEMPORARY, change to Health.DealDamage
        [HarmonyPrefix]
        public static bool EnemyFollower_WaitForTarget(EnemyFollower __instance)
        {
            if (__instance.health.team == Health.Team.PlayerTeam && __instance.health.isPlayerAlly)
            {
                Plugin.Log.LogInfo("waiting for target");
                __instance.StartCoroutine(WaitForEnemy(__instance));

                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(EnemyFollower), nameof(EnemyFollower.ChasePlayer))] //*** THIS IS TEMPORARY, change to Health.DealDamage
        [HarmonyPrefix]
        public static bool EnemyFollower_ChasePlayer(EnemyFollower __instance)
        {
            Plugin.Log.LogInfo("start chasing");
            if (__instance.health.team == Health.Team.PlayerTeam && __instance.health.isPlayerAlly)
            {
                Plugin.Log.LogInfo("start chasing 2");

                __instance.StartCoroutine(ChaseEnemy(__instance));

                return false;
            }


            return true;
        }

        /*[HarmonyPatch(typeof(EnemyFollower), nameof(EnemyFollower.FightPlayer))] //*** THIS IS TEMPORARY, change to Health.DealDamage
        [HarmonyPostfix]
        public static void EnemyFollower_FightPlayer(EnemyFollower __instance)
        {
            Plugin.Log.LogInfo("restart chasing");
            if (__instance.health.team == Health.Team.PlayerTeam && __instance.health.isPlayerAlly)
            {
                Plugin.Log.LogInfo("restart chasing 2");

                __instance.StartCoroutine(WaitForEnemy(__instance));
            }
        }*/

        public static IEnumerator<System.Object> WaitForEnemy(EnemyFollower follower)
        {
            Plugin.Log.LogInfo("got wait for enemy follower is " + follower);
            follower.Spine.Initialize(false);
            follower.state.CURRENT_STATE = StateMachine.State.Idle;
            follower.Spine.AnimationState.SetAnimation(0, "Conversations/idle-nice", true);
            Plugin.Log.LogInfo("waiting for room active");

            while (!GameManager.RoomActive) { 
                yield return null;
            }

            Plugin.Log.LogInfo("checkif target null");

            while (follower.TargetObject == null)
            {
                Plugin.Log.LogInfo("target null, get closest target");

                var nextTarget = follower.GetClosestTarget();
                Plugin.Log.LogInfo("target found of " + nextTarget);
                if (nextTarget != null)
                {
                    follower.TargetObject = nextTarget.gameObject;
                    Plugin.Log.LogInfo("target found of " + nextTarget.team);
                }
                else
                {
                    Plugin.Log.LogInfo("pathing to player");
                    follower.givePath(PlayerFarming.Instance.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle * 2f);
                    follower.followerTimestamp = Time.time + 0.25f;
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
            follower.StartCoroutine(ChaseEnemy(follower));
            yield return null;
        }

        public static IEnumerator<System.Object> ChaseEnemy(EnemyFollower follower)
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
                    if (follower.GetClosestTarget() != null)
                    {
                        follower.TargetObject = follower.GetClosestTarget().gameObject;
                        Plugin.Log.LogInfo("was null, so found new target of " + follower.GetClosestTarget().team);
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
                                yield return follower.StartCoroutine(FightPlayer(follower, 1.5f));
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

        public static void OnRoomCleared() {
            Plugin.Log.LogInfo("Room wascleared");
            Plugin.Log.LogInfo("is this the boss room: " + BiomeGenerator.Instance.CurrentRoom.IsBoss);

            List<Follower> toResummon = new();
            foreach (Follower followerInfo in Plugin.tempSummoned) {
                Plugin.Log.LogInfo(followerInfo.Health.HP + " over " + followerInfo.Health.totalHP);
                if (followerInfo.Health.HP <= 0) {
                    //if wearing goldenskull
                    Plugin.Log.LogInfo("currently wearing " + followerInfo.Brain.Info.Necklace);
                    if (followerInfo.Brain.Info.Necklace == InventoryItem.ITEM_TYPE.Necklace_Gold_Skull) {
                        Plugin.Log.LogInfo("Golden Skull found, revive");
                        toResummon.AddItem(followerInfo);
                    }
                }
                
            }

            if (toResummon != null && !BiomeGenerator.Instance.CurrentRoom.IsBoss) {
                Plugin.Log.LogInfo("Resummon all necklace followers");
                Plugin.tempSummoned.RemoveAll(toResummon.Contains);
                foreach (Follower follower in toResummon) {
                    SpawnAllyFollower(follower.Brain._directInfoAccess, PlayerFarming.Instance.transform.position);
                }
            }

            if (BiomeGenerator.Instance.CurrentRoom.IsBoss) { //for each alive, give 1 prestige, max of 12
                Plugin.Log.LogInfo("Boss completed, give reward");
                int alive = 0;

                foreach (Follower followerInfo in Plugin.tempSummoned) {
                    if (followerInfo.Health.HP > 0) {
                        alive++;
                    }
                }
                //log total alive
                Plugin.Log.LogInfo("Total alive: " + alive);
                Inventory.AddItem(Plugin.prestige, Math.Min(alive, 12));

            }
        }

        public static IEnumerator<object> FightPlayer(EnemyFollower enemyFollower, float attackDistance = 1.5f) {
            enemyFollower.MyState = EnemyFollower.State.Attacking;
            enemyFollower.UsePathing = true;
            enemyFollower.givePath(enemyFollower.TargetObject.transform.position);
            enemyFollower.Spine.AnimationState.SetAnimation(1, "run", true);
            enemyFollower.RepathTimer = 0.0f;
            int NumAttacks = enemyFollower.DoubleAttack ? 2 : 1;
            int AttackCount = 1;
            float MaxAttackSpeed = 15f;
            float AttackSpeed = MaxAttackSpeed;
            bool Loop = true;
            float SignPostDelay = 0.5f;
            while (Loop)
            {
            enemyFollower.Seperate(0.5f);
            if (enemyFollower.TargetObject == null)
            {
                yield return new WaitForSeconds(0.3f);
                enemyFollower.StartCoroutine(enemyFollower.WaitForTarget());
                yield break;
            }
            else
            {
                if (enemyFollower.state.CURRENT_STATE == StateMachine.State.Idle)
                enemyFollower.state.CURRENT_STATE = StateMachine.State.Moving;
                switch (enemyFollower.state.CURRENT_STATE)
                {
                case StateMachine.State.Moving:
                    enemyFollower.state.LookAngle = Utils.GetAngle(enemyFollower.transform.position, enemyFollower.TargetObject.transform.position);
                    enemyFollower.Spine.skeleton.ScaleX = (double) enemyFollower.state.LookAngle <= 90.0 || (double) enemyFollower.state.LookAngle >= 270.0 ? -1f : 1f;
                    enemyFollower.state.LookAngle = enemyFollower.state.facingAngle = Utils.GetAngle(enemyFollower.transform.position, enemyFollower.TargetObject.transform.position);
                    if ((double) Vector2.Distance((Vector2) enemyFollower.transform.position, (Vector2) enemyFollower.TargetObject.transform.position) < (double) attackDistance)
                    {
                    enemyFollower.state.CURRENT_STATE = StateMachine.State.SignPostAttack;
                    enemyFollower.variant = UnityEngine.Random.Range(0, 2);
                    string animationName = enemyFollower.variant == 0 ? "attack-charge" : "attack-charge2";
                    enemyFollower.Spine.AnimationState.SetAnimation(1, animationName, false);
                    }
                    else
                    {
                    if ((double) (enemyFollower.RepathTimer += Time.deltaTime) > 0.20000000298023224)
                    {
                        enemyFollower.RepathTimer = 0.0f;
                        enemyFollower.givePath(enemyFollower.TargetObject.transform.position);
                    }
                    if ((UnityEngine.Object) enemyFollower.damageColliderEvents != (UnityEngine.Object) null)
                    {
                        if ((double) enemyFollower.state.Timer < 0.20000000298023224 && !enemyFollower.health.WasJustParried)
                        enemyFollower.damageColliderEvents.SetActive(true);
                        else
                        enemyFollower.damageColliderEvents.SetActive(false);
                    }
                    }
                    if ((UnityEngine.Object) enemyFollower.damageColliderEvents != (UnityEngine.Object) null)
                    {
                    enemyFollower.damageColliderEvents.SetActive(false);
                    break;
                    }
                    break;
                case StateMachine.State.SignPostAttack:
                    if ((UnityEngine.Object) enemyFollower.damageColliderEvents != (UnityEngine.Object) null)
                    enemyFollower.damageColliderEvents.SetActive(false);
                    enemyFollower.SimpleSpineFlash.FlashWhite(enemyFollower.state.Timer / SignPostDelay);
                    enemyFollower.state.Timer += Time.deltaTime;
                    if ((double) enemyFollower.state.Timer >= (double) SignPostDelay - (double) EnemyFollower.signPostParryWindow)
                    enemyFollower.canBeParried = true;
                    if (enemyFollower.health.team == Health.Team.PlayerTeam && (UnityEngine.Object) enemyFollower.TargetObject != (UnityEngine.Object) null)
                    {
                    enemyFollower.state.LookAngle = Utils.GetAngle(enemyFollower.transform.position, enemyFollower.TargetObject.transform.position);
                    enemyFollower.Spine.skeleton.ScaleX = (double) enemyFollower.state.LookAngle <= 90.0 || (double) enemyFollower.state.LookAngle >= 270.0 ? -1f : 1f;
                    enemyFollower.state.LookAngle = enemyFollower.state.facingAngle = Utils.GetAngle(enemyFollower.transform.position, enemyFollower.TargetObject.transform.position);
                    }
                    if ((double) enemyFollower.state.Timer >= (double) SignPostDelay)
                    {
                    enemyFollower.SimpleSpineFlash.FlashWhite(false);
                    CameraManager.shakeCamera(0.4f, enemyFollower.state.LookAngle);
                    enemyFollower.state.CURRENT_STATE = StateMachine.State.RecoverFromAttack;
                    // enemyFollower.speed = AttackSpeed * 0.0166666675f;
                    string animationName = enemyFollower.variant == 0 ? "attack-impact" : "attack-impact2";
                    enemyFollower.Spine.AnimationState.SetAnimation(1, animationName, false);
                    enemyFollower.canBeParried = true;
                    enemyFollower.StartCoroutine(enemyFollower.EnableDamageCollider(0.0f));
                    if (!string.IsNullOrEmpty(enemyFollower.attackSoundPath))
                    {
                        AudioManager.Instance.PlayOneShot(enemyFollower.attackSoundPath, enemyFollower.transform.position);
                        break;
                    }
                    break;
                    }
                    break;
                case StateMachine.State.RecoverFromAttack:
                    if ((double) AttackSpeed > 0.0)
                    AttackSpeed -= 1f * GameManager.DeltaTime;
                    // enemyFollower.speed = AttackSpeed * Time.deltaTime;
                    enemyFollower.SimpleSpineFlash.FlashWhite(false);
                    enemyFollower.canBeParried = (double) enemyFollower.state.Timer <= (double) EnemyFollower.attackParryWindow;
                    if ((double) (enemyFollower.state.Timer += Time.deltaTime) >= (AttackCount + 1 <= NumAttacks ? 0.5 : 1.0))
                    {
                    if (++AttackCount <= NumAttacks)
                    {
                        AttackSpeed = MaxAttackSpeed + (float) ((3 - NumAttacks) * 2);
                        enemyFollower.state.CURRENT_STATE = StateMachine.State.SignPostAttack;
                        enemyFollower.variant = UnityEngine.Random.Range(0, 2);
                        string animationName = "attack-charge";
                        enemyFollower.Spine.AnimationState.SetAnimation(1, animationName, false);
                        SignPostDelay = 0.3f;
                        break;
                    }
                    Loop = false;
                    enemyFollower.SimpleSpineFlash.FlashWhite(false);
                    break;
                    }
                    break;
                }
                yield return (object) null;
            }
            }
            enemyFollower.StartCoroutine(enemyFollower.ChasePlayer());
        }
    }
}
