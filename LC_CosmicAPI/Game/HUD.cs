using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem;

namespace LC_CosmicAPI.Game
{
	public static class HUD
	{
		public delegate void OnPingScanDelegate(bool isScanning, InputAction.CallbackContext context);

		/// <summary>
		/// Event for when the player begins to scan for scrap
		/// </summary>
		public static event OnPingScanDelegate OnBeginPingScan;

		/// <summary>
		/// Event for when the player finishes scanning for scrap
		/// </summary>
		public static event OnPingScanDelegate OnFinishPingScan;

		/// <summary>
		/// Get the current HUDManager instance
		/// </summary>
		public static HUDManager Manager => HUDManager.Instance != null ? HUDManager.Instance : UnityEngine.Object.FindAnyObjectByType<HUDManager>();

		internal static void InvokeOnPingScan(bool isPre, bool isScanning, in InputAction.CallbackContext context)
		{
			if(isPre) OnBeginPingScan?.Invoke(isScanning, context);
			else OnFinishPingScan?.Invoke(isScanning, context);
		}
	}
}
