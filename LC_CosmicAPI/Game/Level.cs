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
		/// <summary>
		/// Collision masks defined in the game
		/// </summary>
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

		/// <summary>
		/// IDs of each moon
		/// </summary>
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

		public delegate void OnRoundManagerStartDelegate(ref RoundManager roundManager);
		public delegate void SyncScrapValuesDelegate(NetworkObjectReference[] spawnedScrap, int[] allScrapValue);
		public delegate void OnSpawnEnemyGameObjectDelegate(NetworkObjectReference spawnedEnemyReference, int enemyNumber);

		/// <summary>
		/// Event is called when RoundManager.GeneratedFloorPostProcessing starts
		/// </summary>
		public static event Action OnBeginDungeonPostProcess;


		/// <summary>
		/// Event is called when RoundManager.GeneratedFloorPostProcessing finishes
		/// </summary>
		public static event Action OnFinishDungeonPostProcess;

		public delegate void OnPlayerConnectedDelegate(ulong clientId);
		public delegate void OnBeginLoadLevelDelegate(ref SelectableLevel level, int seed, bool isServer);
		public delegate void OnFinishLoadLevelDelegate(ref SelectableLevel level, int seed, bool isServer);


		/// <summary>
		/// Event is called when the level is loaded. This is called just before generation begins
		/// </summary>
		public static event OnBeginLoadLevelDelegate OnBeginLoadLevel;

		/// <summary>
		/// Event is called when the level is loaded. This is called just after generation begins
		/// </summary>
		public static event OnFinishLoadLevelDelegate OnFinishLoadLevel;

		/// <summary>
		/// Event is called when SyncScrapValuesClientRpc is called.
		/// </summary>
		public static event SyncScrapValuesDelegate OnBeginSyncScrapValues;

		/// <summary>
		/// Event is called after SyncScrapValuesClientRpc is called.
		/// </summary>
		public static event SyncScrapValuesDelegate OnFinishSyncScrapValues;

		/// <summary>
		/// Event is called before SpawnScrapInLevel
		/// </summary>
		public static event Action OnBeginSpawnScrapInLevel;

		/// <summary>
		/// Event is called after SpawnScrapInLevel
		/// </summary>
		public static event Action OnFinishSpawnScrapInLevel;

		/// <summary>
		/// Event is called once the level has finished generating the dungeon
		/// </summary>
		public static event Action OnFinishGeneratingLevel;

		/// <summary>
		/// Event is called when an enemy is spawned
		/// </summary>
		public static event OnSpawnEnemyGameObjectDelegate OnSpawnEnemyGameObject;

		/// <summary>
		/// Event is called when the client connects to the server
		/// </summary>
		public static event OnPlayerConnectedDelegate OnPlayerConnected;

		/// <summary>
		/// Event is called when the round manager is first started
		/// </summary>
		public static event OnRoundManagerStartDelegate OnRoundManagerStart;

		/// <summary>
		/// Fetch the current RoundManager instance
		/// </summary>
		public static RoundManager RoundManager => RoundManager.Instance != null ? RoundManager.Instance : UnityEngine.Object.FindObjectOfType<RoundManager>();
		
		/// <summary>
		/// Fetch the current StartOfRound instance
		/// </summary>
		public static StartOfRound StartOfRound => RoundManager != null ? RoundManager.playersManager : null;
		
		/// <summary>
		/// Fetch the current PlayerManager, this is just an alias for StartOfRound
		/// </summary>
		public static StartOfRound PlayerManager => StartOfRound;

		/// <summary>
		/// Fetch a list of all items
		/// </summary>
		public static List<Item> ItemList => StartOfRound.allItemsList.itemsList;

		/// <summary>
		/// Get the current selectable level which is loaded
		/// </summary>
		public static SelectableLevel CurrentSelectableLevel => RoundManager.currentLevel;

		/// <summary>
		/// Fetch the current moon ID.
		/// </summary>
		public static MoonID CurrentMoonID => CurrentSelectableLevel == null ? MoonID.Unknown : (MoonID)CurrentSelectableLevel.levelID;
		public delegate void EnemySpawnedDelegate(NetworkObjectReference enemyReference);

		internal static Dictionary<int, List<EnemySpawnedDelegate>> _enemySpawnedCallbacks = new();

		/// <summary>
		/// Gets the EnemyAI component from an spawned enemy index
		/// </summary>
		/// <param name="index">SpawnedEnemies index</param>
		/// <returns>EnemyAI</returns>
		public static EnemyAI GetSpawnedEnemyAIFromIndex(int index)
		{
			if (index < 0 || index >= RoundManager.SpawnedEnemies.Count) return null;
			return RoundManager.SpawnedEnemies[index];
		}

		/// <summary>
		/// Gets the enemy game object from an spawned enemy index
		/// </summary>
		/// <param name="index">SpawnedEnemies index</param>
		/// <returns>Enemy GameObject</returns>
		public static GameObject GetSpawnedEnemyObjectFromIndex(int index)
		{
			var ai = GetSpawnedEnemyAIFromIndex(index);
			if (ai == null) return null;
			return ai.gameObject;
		}

		/// <summary>
		/// Gets a list of spawned enemies of a specific enemy type
		/// </summary>
		/// <typeparam name="T">EnemyAI</typeparam>
		/// <returns>A list of EnemyAIs</returns>
		public static List<T> GetSpawnedEnemiesOfType<T>() where T : EnemyAI
		{
			List<T> list = new List<T>();
			foreach (var enemy in RoundManager.SpawnedEnemies)
				if (enemy is T) list.Add(enemy as T);
			return list;
		}

		/// <summary>
		/// Gets the first spawned enemies of a specific enemy type
		/// </summary>
		/// <typeparam name="T">EnemyAI</typeparam>
		/// <returns>EnemyAI, null if it doesn't exist</returns>
		public static T GetFirstEnemiesOfType<T>() where T : EnemyAI
		{
			foreach (var enemy in RoundManager.SpawnedEnemies)
				if (enemy is T enemyT) return enemyT;
			return null;
		}

		/// <summary>
		/// Get an collision mask
		/// </summary>
		/// <param name="mask">Collision mask</param>
		/// <returns>Collision mask as an int</returns>
		public static int GetMask(CollisionMask mask)
		{
			return (int)mask;
		}

		/// <summary>
		/// The current maps seed
		/// </summary>
		public static int CurrentSeed => PlayerManager != null ? PlayerManager.randomMapSeed : 0;

		internal static void InvokeDungeonPostProcess(bool isPre)
		{
			if (isPre) OnBeginDungeonPostProcess?.Invoke();
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

		internal static void InvokeOnFinishGeneratingLevelPostfix()
		{

		}

		internal static void InvokeOnSpawnScrapInLevel(bool isPre)
		{
			if (isPre) OnBeginSpawnScrapInLevel?.Invoke();
			else OnFinishSpawnScrapInLevel?.Invoke();
		}

		internal static void InvokeOnPlayerConnected(ulong clientId)
		{
			OnPlayerConnected?.Invoke(clientId);
		}

		internal static void InvokeOnSpawnEnemyGameObject(NetworkObjectReference spawnedEnemyReference, int enemyNumber)
		{
			if (_enemySpawnedCallbacks.TryGetValue(enemyNumber, out var delegateList))
			{
				if (delegateList.Count == 0)
				{
					_enemySpawnedCallbacks.Remove(enemyNumber);
				}
				else
				{
					var firstCallback = delegateList.First();
					delegateList.Remove(firstCallback);
					_enemySpawnedCallbacks[enemyNumber] = delegateList;

					firstCallback(spawnedEnemyReference);
				}
			}
			OnSpawnEnemyGameObject?.Invoke(spawnedEnemyReference, enemyNumber);
		}

		internal static void InvokeOnRoundManagerStart(ref RoundManager __roundmanager)
		{
			OnRoundManagerStart?.Invoke(ref __roundmanager);
		}

		/// <summary>
		/// Gets a player object from a player id
		/// </summary>
		/// <param name="playerId">Player ID</param>
		/// <returns>Player game object</returns>
		public static GameObject GetPlayerObject(int playerId)
		{
			if (playerId < 0) return null;

			var roundStart = StartOfRound;
			if (roundStart != null)
			{
				if (playerId >= roundStart.allPlayerObjects.Length)
					return null;
				return roundStart.allPlayerObjects[playerId];
			}

			var playerScripts = GameObject.FindObjectsOfType<PlayerControllerB>();
			if (playerScripts.Length == 0) return null;
			foreach (var playerScript in playerScripts)
			{
				if ((int)playerScript.playerClientId == playerId)
					return playerScript.gameObject;
			}
			return null;
		}

		/// <summary>
		/// Gets a player controller from a player id
		/// </summary>
		/// <param name="playerId">Player ID</param>
		/// <returns>Player game controller</returns>
		public static PlayerControllerB GetPlayerController(int playerId)
		{
			var playerObject = GetPlayerObject(playerId);
			if (playerObject == null) return null;
			return playerObject.GetComponent<PlayerControllerB>();
		}

		/// <summary>
		/// Spawn an enemy inside the dungeon
		/// </summary>
		/// <param name="enemyId">The enemy index within the current level</param>
		/// <param name="spawnTime">How long until the enemy spawns</param>
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

		/// <summary>
		/// Spawn an enemy inside the dungeon and returns a callback for the spawned enemy when it does spawn
		/// </summary>
		/// <param name="enemyId">The enemy index within the current level</param>
		/// <param name="callback">A callback for when the enemy does spawn</param>
		/// <param name="spawnTime">How long until the enemy spawns</param>
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
