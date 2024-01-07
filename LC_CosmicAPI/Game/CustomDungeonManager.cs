using DunGen;
using DunGen.Graph;
using HarmonyLib;
using LC_CosmicAPI.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Xml.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LC_CosmicAPI.Game
{
	internal class CustomDungeonManager : IPluginModule
	{
		private static List<ICustomDungeon> _dungeonList = new();
		private static Dictionary<Level.MoonID, List<IntWithRarity>> _moonToDungeonLookup = new();

		private static bool DEBUG_ONLY_CUSTOM = false;
		private static int? FLOW_COUNT = null;
		internal static void AddNewDungeon(ICustomDungeon dungeon)
		{
			_dungeonList.Add(dungeon);
		}

		public void OnPluginStart()
		{
			Level.OnRoundManagerStart += Level_OnRoundManagerStart;
			Level.OnBeginLoadLevel += Level_OnBeginLoadLevel;
		}

		private void DoAssetReplacementOnTiles(ref DungeonFlow dungeonFlow)
		{
			NetworkManager networkManager = UnityEngine.Object.FindObjectOfType<NetworkManager>();
			foreach(var node in dungeonFlow.Nodes)
			{
				foreach(var tileset in node.TileSets)
				{
					foreach(var tile in tileset.TileWeights.Weights)
					{
						// Map objects to replace
						var mapObjects = tile.Value.GetComponentsInChildren<RandomMapObject>(true);

						// Sync objects to replace
						var syncedObjects = tile.Value.GetComponentsInChildren<SpawnSyncedObject>(true);

						for (int mapObjectIdx = 0; mapObjectIdx < mapObjects.Length; mapObjectIdx++)
						{
							for (int i = 0; i < mapObjects[mapObjectIdx].spawnablePrefabs.Count; i++)
							{
								var prefab = Network.GetNetworkPrefabFromName(mapObjects[mapObjectIdx].spawnablePrefabs[i].name);
								if (prefab == null) continue;
								Plugin.Log.LogDebug($"Doing RandomMapObject replacement for {prefab.Prefab.name}");
								mapObjects[mapObjectIdx].spawnablePrefabs[i] = prefab.Prefab;
							}
						}

						for (int i = 0; i < syncedObjects.Length; i++)
						{
							var prefab = Network.GetNetworkPrefabFromName(syncedObjects[i].spawnPrefab.name);
							if (prefab == null) continue;
							Plugin.Log.LogDebug($"Doing RandomMapObject SpawnSyncedObject for {prefab.Prefab.name}");
							syncedObjects[i].spawnPrefab = prefab.Prefab;
						}
					}
				}
			}
		}

		private void Level_OnRoundManagerStart(ref RoundManager roundManager)
		{
			if (!FLOW_COUNT.HasValue) FLOW_COUNT = roundManager.dungeonFlowTypes.Length;
			foreach (var dungeon in _dungeonList)
			{
				int currentDungeonCount = roundManager.dungeonFlowTypes.Length;

				var flowType = dungeon.LoadDungeonFlow();
				DoAssetReplacementOnTiles(ref flowType);

				Level.RoundManager.dungeonFlowTypes = Level.RoundManager.dungeonFlowTypes.AddToArray(flowType);
				dungeon.FlowIndex = currentDungeonCount;

				Plugin.Log.LogDebug($"Adding new dungeon {flowType.name}");

				// Add to our lookup table
				for (Level.MoonID moonId = Level.MoonID.StartMoon; moonId <= Level.MoonID.FinalMoon; moonId++)
				{
					if (!dungeon.ShouldSpawnOnMoon(moonId)) continue;
					if (!_moonToDungeonLookup.ContainsKey(moonId))
						_moonToDungeonLookup[moonId] = new List<IntWithRarity>();

					var element = new IntWithRarity();
					element.rarity = dungeon.GetRarityForMoon(moonId);
					element.id = currentDungeonCount;

					_moonToDungeonLookup[moonId].Add(element);
				}
			}
		}

		private void Level_OnBeginLoadLevel(ref SelectableLevel level, int seed, bool isServer)
		{
			if (isServer) return;

			if (_moonToDungeonLookup.TryGetValue(Level.CurrentMoonID, out var customMoonFlows))
			{
				if (customMoonFlows.Count == 0) return;

				// Clear all previous flow types so only ours exists
				if (DEBUG_ONLY_CUSTOM)
					level.dungeonFlowTypes = new IntWithRarity[] { };

				var currentFlowTypes = level.dungeonFlowTypes;

				foreach ( var element in customMoonFlows)
				{
					if (currentFlowTypes.Any(x => x.id == element.id)) continue;
					level.dungeonFlowTypes = level.dungeonFlowTypes.AddToArray(element);
					Plugin.Log.LogDebug($"Adding custom flow type {element.id}");
				}
			}
		}
	}
}
