using COTL_API.CustomTarotCard;
using HarmonyLib;
using Lamb.UI;
using MMBiomeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            spawnedFollower.Follower.Spine.AnimationState.AddAnimation(1, "Conversations/idle-hate", true, 0.0f);
            spawnedFollower.Follower.GetComponent<EnemyFollower>().enabled = true;

            var modifiedFollower = spawnedFollower.Follower.GetComponent<EnemyFollower>();
            
            // DAMAGE BALANCING TODO:check if commander TODO: Prestiges
            modifiedFollower.Damage = spawnedFollower.FollowerBrain._directInfoAccess.FollowerLevel * 0.5f;

            // HEALTH BALANCING TODO:check if commander
            Health component = spawnedFollower.Follower.GetComponent<Health>();
            component.totalHP = 1f + (spawnedFollower.FollowerBrain._directInfoAccess.FollowerLevel * 0.5f);
            component.HP = component.totalHP;

            //set to ally
            component.team = Health.Team.PlayerTeam;
            component.isPlayerAlly = true;
            Plugin.Log.LogInfo("ally was " + component.isPlayerAlly);
            Plugin.Log.LogInfo("hp was " + component._totalHP + " and _hp is " + component._HP + " and " + component.HP);

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

            return true;
        }

       /* [HarmonyPatch(typeof(Swipe), nameof(Swipe.OnTriggerEnter2D))] //*** THIS IS TEMPORARY, change to Health.DealDamage
        [HarmonyPrefix]
        public static bool PlayerFarming_OnTriggerEnterEvent(Collider2D collider)
        {
            Health componentInParent = collider.gameObject.GetComponent<Health>();
            if (componentInParent.team == Health.Team.PlayerTeam)
            {
                Plugin.Log.LogInfo("No attack self");
                return false;
            }

            return true;
        }*/

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

                    if (follower.Spine.AnimationName != "roll-stop" && follower.state.CURRENT_STATE == StateMachine.State.Moving && follower.Spine.AnimationName != "run-enemy")
                        follower.Spine.AnimationState.SetAnimation(1, "run-enemy", true);

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
                                yield return follower.StartCoroutine(follower.FightPlayer());
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
    }

}
