using HarmonyLib;
using LC_CosmicAPI.Game;
using LC_CosmicAPI.Util;

namespace LC_CosmicAPI.Patches
{
	internal class NetworkPatches : ILethalPatch
	{
		[HarmonyPatch(typeof(GameNetworkManager))]
		[HarmonyPostfix]
		private static void GameNetworkManagerStart(ref GameNetworkManager __instance)
		{
			Network.InvokeGameManagerStart(__instance);
		}

		public override bool Preload()
		{
			return true;
		}
	}
}
