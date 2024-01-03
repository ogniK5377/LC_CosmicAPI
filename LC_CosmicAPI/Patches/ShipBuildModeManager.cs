using HarmonyLib;
using LC_CosmicAPI.Util;
using UnityEngine;

namespace LC_CosmicAPI.Patches
{
	internal class ShipBuildModeManagerPatches : ILethalPatch
	{
		[HarmonyPatch(typeof(ShipBuildModeManager), "PlaceShipObject")]
		[HarmonyPrefix]
		public static void PlaceShipObjectPre(ref ShipBuildModeManager __instance, Vector3 placementPosition, Vector3 placementRotation, PlaceableShipObject placeableObject, bool placementSFX)
		{
			Game.Ship.InvokePlaceShipObject(true, placementPosition, placementRotation, placeableObject);
		}

		[HarmonyPatch(typeof(ShipBuildModeManager), "PlaceShipObject")]
		[HarmonyPostfix]
		public static void PlaceShipObjectPost(ref ShipBuildModeManager __instance, Vector3 placementPosition, Vector3 placementRotation, PlaceableShipObject placeableObject, bool placementSFX)
		{
			Game.Ship.InvokePlaceShipObject(false, placementPosition, placementRotation, placeableObject);
		}

		public override bool Preload()
		{
			return true;
		}
	}
}
