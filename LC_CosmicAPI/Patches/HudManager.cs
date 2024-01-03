using HarmonyLib;
using LC_CosmicAPI.Game;
using LC_CosmicAPI.Util;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine.InputSystem;

namespace LC_CosmicAPI.Patches
{
	
	internal class HudManagerPatches : ILethalPatch
	{
		private delegate bool CanPlayerScanDelegate(HUDManager __instance);
		private delegate float playerPingingScanDelegate(HUDManager __instance);

		private static CanPlayerScanDelegate CanPlayerScan;
		private static FieldInfo playerPingingScan;

		[HarmonyPatch(typeof(HUDManager), "PingScan_performed")]
		[HarmonyPrefix]
		public static void PingScan_performedPre(HUDManager __instance, ref InputAction.CallbackContext context, out bool __state)
		{
			__state = !(GameNetworkManager.Instance.localPlayerController == null) && context.performed && CanPlayerScan(__instance) && (float)playerPingingScan.GetValue(__instance) <= -1f;
			HUD.InvokeOnPingScan(true, __state, context);
		}

		[HarmonyPatch(typeof(HUDManager), "PingScan_performed")]
		[HarmonyPostfix]
		public static void PingScan_performedPost(HUDManager __instance, InputAction.CallbackContext context, bool __state)
		{
			HUD.InvokeOnPingScan(false, __state, context);
		}

		public override bool Preload()
		{
			CanPlayerScan = AccessTools.MethodDelegate<CanPlayerScanDelegate>(AccessTools.Method(typeof(HUDManager), "CanPlayerScan"));
			playerPingingScan = AccessTools.Field(typeof(HUDManager), "playerPingingScan");

			return true;
		}
	}
	
}
