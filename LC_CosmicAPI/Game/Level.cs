using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
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

		public enum MoonID
		{
			Unknown = -1,
			Experimentation = 0,
			Assurance = 1,
			Vow = 2,
			Company = 3,
			March = 4,
			Rend = 5,
			Dine = 6,
			Offense = 7,
			Titan = 8,

			StartMoon = Experimentation,
			FinalMoon = Titan,
		}

		public delegate void SyncScrapValuesDelegate(NetworkObjectReference[] spawnedScrap, int[] allScrapValue);
		public delegate void OnSpawnEnemyGameObjectDelegate(NetworkObjectReference spawnedEnemyReference, int enemyNumber);

		public static event Action OnBeginDungeonPostProcess;
		public static event Action OnFinishDungeonPostProcess;

		// ref SelectableLevel level, int seed, bool isServer

		public delegate void OnBeginLoadLevelDelegate(ref SelectableLevel level, int seed, bool isServer);
		public delegate void OnFinishLoadLevelDelegate(ref SelectableLevel level, int seed, bool isServer);

		public static event OnBeginLoadLevelDelegate OnBeginLoadLevel;
		public static event OnFinishLoadLevelDelegate OnFinishLoadLevel;

		public static event SyncScrapValuesDelegate OnBeginSyncScrapValues;
		public static event SyncScrapValuesDelegate OnFinishSyncScrapValues;

		public static event Action OnBeginSpawnScrapInLevel;
		public static event Action OnFinishSpawnScrapInLevel;

		public static event Action OnFinishGeneratingLevel;
		public static event OnSpawnEnemyGameObjectDelegate OnSpawnEnemyGameObject;

		public static RoundManager RoundManager => RoundManager.Instance != null ? RoundManager.Instance : UnityEngine.Object.FindObjectOfType<RoundManager>();
		public static StartOfRound StartOfRound => RoundManager != null ? RoundManager.playersManager : null;
		public static StartOfRound PlayerManager => StartOfRound;

		public static SelectableLevel CurrentSelectableLevel => RoundManager.currentLevel;

		public static MoonID CurrentMoonID => CurrentSelectableLevel == null ? MoonID.Unknown : (MoonID)CurrentSelectableLevel.levelID;


		public delegate void EnemySpawnedDelegate(NetworkObjectReference enemyReference);

		internal static Dictionary<int, List<EnemySpawnedDelegate>> _enemySpawnedCallbacks = new();

		public static EnemyAI GetSpawnedEnemyAIFromIndex(int index)
		{
			if (index < 0 || index >= RoundManager.SpawnedEnemies.Count) return null;
			return RoundManager.SpawnedEnemies[index];
		}
		public static GameObject GetSpawnedEnemyObjectFromIndex(int index)
		{
			var ai = GetSpawnedEnemyAIFromIndex(index);
			if(ai == null) return null;
			return ai.gameObject;
		}

		public static List<T> GetSpawnedEnemiesOfType<T>() where T : EnemyAI
		{
			List<T> list = new List<T>();
			foreach (var enemy in RoundManager.SpawnedEnemies)
				if (enemy is T) list.Add(enemy as T);
			return list;
		}
		public static T GetFirstEnemiesOfType<T>() where T : EnemyAI
		{
			foreach (var enemy in RoundManager.SpawnedEnemies)
				if (enemy is T enemyT) return enemyT;
			return null;
		}

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

		internal static void InvokeSyncScrapValues(bool isPre, NetworkObjectReference[] spawnedScrap, int[] allScrapValue)
		{
			if (isPre) OnBeginSyncScrapValues?.Invoke(spawnedScrap, allScrapValue);
			else OnFinishSyncScrapValues?.Invoke(spawnedScrap, allScrapValue);
		}

		internal static void InvokeOnLoadLevel(ref SelectableLevel level, int seed, bool isServer, bool isPre)
		{
			if (isPre) OnBeginLoadLevel?.Invoke(ref level, seed, isServer);
			else OnFinishLoadLevel?.Invoke(ref level, seed, isServer);
		}

		internal static void InvokeOnFinishGeneratingLevel()
		{
			OnFinishGeneratingLevel?.Invoke();
			CustomScrapManager.OnFishedGeneratingLevel();
		}

		internal static void InvokeOnSpawnScrapInLevel(bool isPre)
		{
			if (isPre) OnBeginSpawnScrapInLevel?.Invoke();
			else OnFinishSpawnScrapInLevel?.Invoke();
		}

		internal static void InvokeOnSpawnEnemyGameObject(NetworkObjectReference spawnedEnemyReference, int enemyNumber)
		{
			if(_enemySpawnedCallbacks.TryGetValue(enemyNumber, out var delegateList))
			{
				if(delegateList.Count == 0)
				{
					_enemySpawnedCallbacks.Remove(enemyNumber);
				} else
				{
					var firstCallback = delegateList.First();
					delegateList.Remove(firstCallback);
					_enemySpawnedCallbacks[enemyNumber] = delegateList;

					firstCallback(spawnedEnemyReference);
				}
			}
			OnSpawnEnemyGameObject?.Invoke(spawnedEnemyReference, enemyNumber);
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

		public static void ForceSpawnEnemyAtRandomVent(int enemyId, int spawnTime = 0)
		{
			if (!RoundManager.IsServer) return;
			List<EnemyVent> ventList = new();
			for (int i = 0; i < RoundManager.allEnemyVents.Length; i++)
			{
				if (!RoundManager.allEnemyVents[i].occupied)
				{
					ventList.Add(RoundManager.allEnemyVents[i]);
				}
			}
			// No vents!
			if (ventList.Count == 0) return;
			var vent = ventList.OrderBy(x => Guid.NewGuid()).First();
			RoundManager.enemySpawnTimes.Add(spawnTime);

			var spawnableEnemy = Level.CurrentSelectableLevel.Enemies[enemyId];
			vent.enemyType = spawnableEnemy.enemyType;
			vent.enemyTypeIndex = enemyId;
			vent.occupied = true;
			vent.spawnTime = spawnTime;
			spawnableEnemy.enemyType.numberSpawned++;

			vent.SyncVentSpawnTimeClientRpc(spawnTime, enemyId);
		}

		public static void ForceSpawnEnemyAtRandomVentWithEnemyCallback(int enemyId, EnemySpawnedDelegate callback, int spawnTime = 0)
		{
			if (!RoundManager.IsServer) return;
			List<EnemyVent> ventList = new();
			for (int i = 0; i < RoundManager.allEnemyVents.Length; i++)
			{
				if (!RoundManager.allEnemyVents[i].occupied)
				{
					ventList.Add(RoundManager.allEnemyVents[i]);
				}
			}
			// No vents!
			if (ventList.Count == 0) return;
			var vent = ventList.OrderBy(x => Guid.NewGuid()).First();
			RoundManager.enemySpawnTimes.Add(spawnTime);

			var spawnableEnemy = Level.CurrentSelectableLevel.Enemies[enemyId];
			vent.enemyType = spawnableEnemy.enemyType;
			vent.enemyTypeIndex = enemyId;
			vent.occupied = true;
			vent.spawnTime = spawnTime;
			spawnableEnemy.enemyType.numberSpawned++;

			if (!_enemySpawnedCallbacks.ContainsKey(enemyId))
				_enemySpawnedCallbacks[enemyId] = new();

			_enemySpawnedCallbacks[enemyId].Add(callback);

			vent.SyncVentSpawnTimeClientRpc(spawnTime, enemyId);
		}
	}
}
