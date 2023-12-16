using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LC_CosmicAPI.Game
{
	public static class Level
	{
		public enum CollisionMask
		{
			Default = 1 << 0,
			Transparent = 1 << 1,
			Player = 1 << 3,
			Room = 1 << 8,
			Foliage = 1 << 10,
			Unknown11 = 1 << 11,
			Colliders = 1 << 12,
			Unknown28 = 1 << 28,

			Walkable = Default | Player | Room | Unknown11 | Unknown28,
			CollidersRoom = Colliders | Room,
			CollidersRoomPlayer = CollidersRoom | Player,
			CollidersRoomDefault = CollidersRoom | Default,
			CollidersRoomDefaultPlayers = CollidersRoomDefault | Player,
			CollidersRoomDefaultFoliage = CollidersRoomDefault | Foliage,
		}

		public static Action OnBeginDungeonPostProcess;
		public static Action OnFinishDungeonPostProcess;

		public static Action<SelectableLevel, int, bool> OnBeginLoadLevel;
		public static Action<SelectableLevel, int, bool> OnFinishLoadLevel;

		public static RoundManager RoundManager => RoundManager.Instance != null ? RoundManager.Instance : UnityEngine.Object.FindObjectOfType<RoundManager>();
		public static StartOfRound StartOfRound => RoundManager != null ? RoundManager.playersManager : null;
		public static StartOfRound PlayerManager => StartOfRound;

		public static int GetMask(CollisionMask mask)
		{
			return (int)mask;
		}

		public static int CurrentSeed => PlayerManager != null ? PlayerManager.randomMapSeed : 0;

		internal static void InvokeDungeonPostProcess(bool isPre)
		{
			if(isPre) OnBeginDungeonPostProcess?.Invoke();
			else OnFinishDungeonPostProcess?.Invoke();
		}

		internal static void InvokeOnLoadLevel(ref SelectableLevel level, int seed, bool isServer, bool isPre)
		{
			if (isPre) OnBeginLoadLevel?.Invoke(level, seed, isServer);
			else OnFinishLoadLevel?.Invoke(level, seed, isServer);
		}

		public static GameObject GetPlayerObject(int playerId)
		{
			if(playerId < 0) return null;

			var roundStart = StartOfRound;
			if(roundStart != null)
			{
				if (playerId >= roundStart.allPlayerObjects.Length)
					return null;
				return roundStart.allPlayerObjects[playerId];
			}

			var playerScripts = GameObject.FindObjectsOfType<PlayerControllerB>();
			if(playerScripts.Length == 0) return null;
			foreach(var playerScript in playerScripts)
			{
				if ((int)playerScript.playerClientId == playerId)
					return playerScript.gameObject;
			}
			return null;
		}

		public static PlayerControllerB GetPlayerController(int playerId)
		{
			var playerObject = GetPlayerObject(playerId);
			if(playerObject == null) return null;
			return playerObject.GetComponent<PlayerControllerB>();
		}
	}
}
