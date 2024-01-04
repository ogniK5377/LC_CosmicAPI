using HarmonyLib;
using LC_CosmicAPI.Util;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace LC_CosmicAPI.Patches
{
	internal class RoundManagerPatch : ILethalPatch
	{
		[HarmonyPatch(typeof(RoundManager), "GeneratedFloorPostProcessing")]
		[HarmonyPrefix]
		public static void GeneratedFloorPostProcessingPre(ref RoundManager __instance)
		{
			if(__instance.IsServer)
				Game.Level.InvokeDungeonPostProcess(true);
		}

		[HarmonyPatch(typeof(RoundManager), "GeneratedFloorPostProcessing")]
		[HarmonyPostfix]
		public static void GeneratedFloorPostProcessingPost(ref RoundManager __instance)
		{
			if (__instance.IsServer)
				Game.Level.InvokeDungeonPostProcess(false);
		}

		[HarmonyPatch(typeof(RoundManager), "LoadNewLevelWait")]
		[HarmonyPrefix]
		public static void LoadNewLevelWait(ref RoundManager __instance, int randomSeed)
		{
			Game.Level.InvokeOnLoadLevel(ref __instance.currentLevel, randomSeed, true, true);
		}

		[HarmonyPatch(typeof(RoundManager), "GenerateNewFloor")]
		[HarmonyPrefix]
		public static void GenerateNewFloor(ref RoundManager __instance)
		{
			int randomSeed = __instance.playersManager.randomMapSeed;
			Game.Level.InvokeOnLoadLevel(ref __instance.currentLevel, randomSeed, false, true);
		}

		[HarmonyPatch(typeof(RoundManager), "FinishGeneratingLevel")]
		[HarmonyPrefix]
		public static void FinishGeneratingLevel(ref RoundManager __instance)
		{
			Game.Level.InvokeOnFinishGeneratingLevel();
		}

		[HarmonyPatch(typeof(RoundManager), "FinishGeneratingLevel")]
		[HarmonyPostfix]
		public static void FinishGeneratingLevelPostfix(ref RoundManager __instance)
		{
			Game.Level.InvokeOnFinishGeneratingLevelPostfix();
		}

		[HarmonyPatch(typeof(RoundManager), "FinishGeneratingNewLevelClientRpc")]
		[HarmonyPostfix]
		public static void FinishGeneratingNewLevelClientRpc(ref RoundManager __instance)
		{
			if (!__instance.IsServer && !__instance.NetworkManager.IsHost)
			{
				int randomSeed = __instance.playersManager.randomMapSeed;
				Game.Level.InvokeOnLoadLevel(ref __instance.currentLevel, randomSeed, false, false);
			}
		}

		[HarmonyPatch(typeof(RoundManager), "SyncScrapValuesClientRpc")]
		[HarmonyPrefix]
		public static void SyncScrapValuesClientRpcPre(ref RoundManager __instance, NetworkObjectReference[] spawnedScrap, int[] allScrapValue)
		{
			Game.Level.InvokeSyncScrapValues(true, spawnedScrap, allScrapValue);
		}

		[HarmonyPatch(typeof(RoundManager), "SyncScrapValuesClientRpc")]
		[HarmonyPostfix]
		public static void SyncScrapValuesClientRpcPost(ref RoundManager __instance, NetworkObjectReference[] spawnedScrap, int[] allScrapValue)
		{
			Game.Level.InvokeSyncScrapValues(false, spawnedScrap, allScrapValue);
		}


		[HarmonyPatch(typeof(RoundManager), "LoadNewLevelWait")]
		[HarmonyPostfix]
		public static void LoadNewLevelWait(ref RoundManager __instance)
		{
			if (__instance.IsServer)
			{
				int randomSeed = __instance.playersManager.randomMapSeed;
				Game.Level.InvokeOnLoadLevel(ref __instance.currentLevel, randomSeed, true, false);
			}
		}

		[HarmonyPatch(typeof(RoundManager), "SpawnScrapInLevel")]
		[HarmonyPrefix]
		public static void SpawnScrapInLevelPre()
		{
			Game.Level.InvokeOnSpawnScrapInLevel(true);
		}

		[HarmonyPatch(typeof(RoundManager), "SpawnScrapInLevel")]
		[HarmonyPostfix]
		public static void SpawnScrapInLevelPost()
		{
			Game.Level.InvokeOnSpawnScrapInLevel(false);
		}

		[HarmonyPatch(typeof(RoundManager), "SpawnEnemyGameObject")]
		[HarmonyPostfix]
		public static void SpawnEnemyGameObject(ref RoundManager __instance, NetworkObjectReference __result, Vector3 spawnPosition, float yRot, int enemyNumber, EnemyType enemyType = null)
		{
			Game.Level.InvokeOnSpawnEnemyGameObject(__result, enemyNumber);
		}

		[HarmonyPatch(typeof(RoundManager), "Start")]
		[HarmonyPostfix]
		public static void RoundManagerStart(ref RoundManager __instance)
		{
			Game.Level.InvokeOnRoundManagerStart(ref __instance);
		}

		public override bool Preload()
		{
			return true;
		}
	}
}
