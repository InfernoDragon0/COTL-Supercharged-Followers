using DG.Tweening;
using HarmonyLib;
using MMBiomeGeneration;
using SuperchargedFollowers.Helpers;
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
            spawnedFollower.Follower.GetComponent<EnemyFollower>().gameObject.AddComponent<FollowerRoutines>();

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
                __instance.StartCoroutine(__instance.GetComponent<FollowerRoutines>().WaitForEnemy());

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

                __instance.StartCoroutine(__instance.GetComponent<FollowerRoutines>().ChaseEnemy());

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
                else {
                    //get follower regen bonus
                    var necklaceBonus = Helpers.Bonuses.GetNecklaceBonuses(followerInfo.Brain._directInfoAccess);
                    var commanderBonus = Helpers.Bonuses.GetCommanderBonuses(followerInfo.Brain._directInfoAccess);
                    var classBonus = Helpers.Bonuses.GetClassBonuses(followerInfo.Brain._directInfoAccess);
                    followerInfo.Health.HP += necklaceBonus.RegenBonus + commanderBonus.RegenBonus + classBonus.RegenBonus;
                    Plugin.Log.LogInfo("Regenerated " + followerInfo.Brain._directInfoAccess.Name + " to " + followerInfo.Health.HP + " HP");
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
    }
}
