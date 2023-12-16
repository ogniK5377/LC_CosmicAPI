using HarmonyLib;
using LC_CosmicAPI.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace LC_CosmicAPI.Patches
{
	internal class RoundManagerPatch : ILethalPatch
	{
		[HarmonyPatch(typeof(RoundManager))]
		[HarmonyPrefix]
		public static void GeneratedFloorPostProcessingPre(ref RoundManager __instance)
		{
			if(__instance.IsServer)
				Game.Level.InvokeDungeonPostProcess(true);
		}

		[HarmonyPatch(typeof(RoundManager))]
		[HarmonyPostfix]
		public static void GeneratedFloorPostProcessingPost(ref RoundManager __instance)
		{
			if (__instance.IsServer)
				Game.Level.InvokeDungeonPostProcess(false);
		}

		[HarmonyPatch(typeof(RoundManager))]
		[HarmonyPrefix]
		public static void LoadNewLevelWait(ref RoundManager __instance, int randomSeed)
		{
			Game.Level.InvokeOnLoadLevel(ref __instance.currentLevel, randomSeed, true, true);
		}

		[HarmonyPatch(typeof(RoundManager))]
		[HarmonyPrefix]
		public static void GenerateNewFloor(ref RoundManager __instance)
		{
			int randomSeed = __instance.playersManager.randomMapSeed;
			Game.Level.InvokeOnLoadLevel(ref __instance.currentLevel, randomSeed, false, true);
		}

		[HarmonyPatch(typeof(RoundManager))]
		[HarmonyPostfix]
		public static void FinishGeneratingNewLevelClientRpc(ref RoundManager __instance)
		{
			if (!__instance.IsServer && !__instance.NetworkManager.IsHost)
			{
				int randomSeed = __instance.playersManager.randomMapSeed;
				Game.Level.InvokeOnLoadLevel(ref __instance.currentLevel, randomSeed, false, false);
			}
		}


		[HarmonyPatch(typeof(RoundManager))]
		[HarmonyPostfix]
		public static void LoadNewLevelWait(ref RoundManager __instance)
		{
			if (__instance.IsServer)
			{
				int randomSeed = __instance.playersManager.randomMapSeed;
				Game.Level.InvokeOnLoadLevel(ref __instance.currentLevel, randomSeed, true, false);
			}
		}

		public override bool Preload()
		{
			return true;
		}
	}
}
