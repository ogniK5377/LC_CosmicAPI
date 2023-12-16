using HarmonyLib;
using LC_CosmicAPI.Game;
using LC_CosmicAPI.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace LC_CosmicAPI.Patches
{
	internal class DeadBodyPatch : ILethalPatch
	{
		[HarmonyPatch(typeof(DeadBodyInfo), "Start")]
		[HarmonyPostfix]
		public static void Start(ref DeadBodyInfo __instance)
		{
			var helper = __instance.playerScript.gameObject.GetComponent<ControllerHelper>();
			if (helper == null) return;

			if (__instance.gameObject.GetComponent<ControllerHelper>() == null)
				__instance.gameObject.AddComponent<ControllerHelper>();

			helper.SpawnDeadBodyEv(__instance);
		}

		[HarmonyPatch(typeof(DeadBodyInfo), "DeactivateBody")]
		[HarmonyPrefix]
		public static bool DeactivateBody(ref DeadBodyInfo __instance, bool setActive)
		{
			var helper = __instance.playerScript.gameObject.GetComponent<ControllerHelper>();
			if (helper == null) return false;

			bool replace = helper.DeadBodyUpdateEv(__instance, setActive);

			__instance.SetBodyPartsKinematic();
			__instance.isInShip = false;
			__instance.deactivated = true;
			return replace;
		}

		public override bool Preload()
		{
			return true;
		}
	}
}
