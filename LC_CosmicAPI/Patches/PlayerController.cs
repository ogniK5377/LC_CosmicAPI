using GameNetcodeStuff;
using HarmonyLib;
using LC_CosmicAPI.Game;
using LC_CosmicAPI.Util;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LC_CosmicAPI.Patches
{
	internal class PlayerControllerPatch : ILethalPatch
	{
		[HarmonyPatch(typeof(PlayerControllerB), "SpawnPlayerAnimation")]
		[HarmonyPostfix]
		public static void SpawnPlayerAnimation(ref PlayerControllerB __instance)
		{
			for (int i = 0; i < StartOfRound.Instance.allPlayerObjects.Length; i++)
			{
				PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[i];
				var player = playerControllerB.gameObject;
				if (!player.GetComponent<ControllerHelper>())
				{
					player.AddComponent<ControllerHelper>();
				}
				player.GetComponent<ControllerHelper>().SpawnPlayerAnimatorEv();
			}
		}

		[HarmonyPatch(typeof(PlayerControllerB), "DisablePlayerModel")]
		[HarmonyPrefix]
		public static bool DisablePlayerModel(ref PlayerControllerB __instance, GameObject playerObject, bool enable, bool disableLocalArms)
		{
			var helperController = playerObject.GetComponent<ControllerHelper>();
			if (helperController == null) return false;

			bool shouldReplace = helperController.UpdateModelState(enable, disableLocalArms);

			if (disableLocalArms)
			{
				__instance.thisPlayerModelArms.enabled = false;
			}
			return shouldReplace;
		}

		public override bool Preload()
		{
			return true;
		}
	}
}
