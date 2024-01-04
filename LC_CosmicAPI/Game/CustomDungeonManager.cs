using DunGen;
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

		private static bool DEBUG_ONLY_CUSTOM = true;
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

		private void Level_OnRoundManagerStart(ref RoundManager roundManager)
		{
			if (!FLOW_COUNT.HasValue) FLOW_COUNT = roundManager.dungeonFlowTypes.Length;
			foreach (var dungeon in _dungeonList)
			{
				int currentDungeonCount = roundManager.dungeonFlowTypes.Length;

				var flowType = dungeon.LoadDungeonFlow();
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
