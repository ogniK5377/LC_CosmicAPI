using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LC_CosmicAPI.Game
{
	public static class Ship
	{
		public static ShipBuildModeManager BuildModeManager => ShipBuildModeManager.Instance;
		public static bool InBuildMode => BuildModeManager.InBuildMode;

		public delegate void PlaceShipObjectDelegate(Vector3 placementPosition, Vector3 placementRotation, PlaceableShipObject placeableObject);

		/// <summary>
		/// Event is called when an unlockable is placed on the ship
		/// </summary>
		public static event PlaceShipObjectDelegate OnBeginPlaceShipObject;

		/// <summary>
		/// Event is called after an unlockable is placed on the ship
		/// </summary>
		public static event PlaceShipObjectDelegate OnFinishPlaceShipObject;

		internal static void InvokePlaceShipObject(bool isPre, Vector3 placementPosition, Vector3 placementRotation, PlaceableShipObject placeableObject)
		{
			if (isPre) OnBeginPlaceShipObject?.Invoke(placementPosition, placementRotation, placeableObject);
			else OnFinishPlaceShipObject?.Invoke(placementPosition, placementRotation, placeableObject);
		}
	}
}
