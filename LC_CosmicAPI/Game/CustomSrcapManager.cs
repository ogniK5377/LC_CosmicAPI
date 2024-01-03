using HarmonyLib;
using LC_CosmicAPI.Util;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.UIElements;

namespace LC_CosmicAPI.Game
{
	internal class CustomScrapManager : IPluginModule
	{
		// I needed a way to ensure that custom items don't conflict with other item ids. Load order for plugins can change
		// items can have minor updates, new content can be added to the game etc. Here's what I came up with.
		// I store a new key inside the save file known as "Cosmic_CustomScrapHashes". All default built in items have a
		// hash for 0, or invalid. All custom items will have a unique hash.
		// When loading the items in for the first time, all the plugins will have been initialized, and have registed all the scrap.
		// If the client loads a save file with custom items, we check the hashes and FORCE the items to load in the same order.
		// If the game has an update, we can shift all the item indexes by N and account for that too!
		// This works both ways as I also handle removeal of mods & removeal of official game items
		internal const string HashSaveFileKey = "Cosmic_CustomScrapHashes";

		private static List<ICustomScrap> _scrapList = new();
		private static Dictionary<Level.MoonID, List<int>> _moonScrapLookup = new();
		private static Dictionary<ulong, int> _hashToItem = new();
		internal static List<ulong> _itemsLeft = new();
		internal static List<ulong> _itemHashListOrdered = new();

		internal static int CustomItemStartIndex = -1;

		internal static int GetRarityForItemIdOnMoon(Level.MoonID id, int itemId)
		{
			if (itemId >= _scrapList.Count || itemId < 0) return 0;
			return _scrapList[itemId].GetRarityForMoon(id);
		}

		internal static Item GetSpawnableItemForItemId(int itemId)
		{
			if (itemId >= _scrapList.Count || itemId < 0) return null;
			return _scrapList[itemId].ItemDetails;
		}

		internal static void OnFishedGeneratingLevel()
		{
			if(_moonScrapLookup.TryGetValue(Level.CurrentMoonID, out var scrapToSpawn))
			{
				foreach(var scrap in scrapToSpawn)
				{
					Level.CurrentSelectableLevel.spawnableScrap.Add(new SpawnableItemWithRarity()
					{
						rarity = GetRarityForItemIdOnMoon(Level.CurrentMoonID, scrap),
						spawnableItem = GetSpawnableItemForItemId(scrap),
					});
				}
			}
		}

		internal static ulong Hash64(string hashString)
		{
			// Netcode uses XXhash which works for us
			return Network.Hash64(hashString);
		}

		internal static bool IsCustomItemFromId(int itemId)
		{
			return itemId >= CustomItemStartIndex && itemId < StartOfRound.Instance.allItemsList.itemsList.Count;
		}

		internal static ulong HashItem(Item item)
		{
			return Hash64(item.itemName);
		}

		internal static bool DoesItemHashExist(ulong itemHash)
		{
			if (itemHash == 0) return false;
			return _hashToItem.ContainsKey(itemHash);
		}

		internal static void AddNewScrap(ICustomScrap scrap)
		{
			// Add to our global scrap list
			_scrapList.Add(scrap);
			int targetIndex = _scrapList.Count - 1;

			// Setup a easy quick lookup for only the specific moon we're at. Saves us having to loop through
			// everything all the time
			for (Level.MoonID moon = Level.MoonID.StartMoon; moon <= Level.MoonID.FinalMoon; moon++)
			{
				if (!_moonScrapLookup.ContainsKey(moon))
					_moonScrapLookup[moon] = new List<int>();

				if (scrap.ShouldSpawnOnMoon(moon))
					_moonScrapLookup[moon].Add(targetIndex);
			}
		}

		internal static Item GetItemFromHash(ulong hash, int desiredItemId = -1)
		{
			if(hash == 0) return null;
			if(_hashToItem.TryGetValue(hash, out int itemIndex)) {
				// If we want to update the item id, lets do it!
				if(desiredItemId != -1)
					_scrapList[itemIndex].ItemDetails.itemId = desiredItemId;

				return _scrapList[itemIndex].ItemDetails;
			}
			return null;
		}

		internal static void MarkHashAsLoaded(ulong hash)
		{
			Plugin.Log.LogDebug($"Loading existing item {hash}");
			_itemsLeft.Remove(hash);
		}

		internal static void RegisterScrap()
		{
			for(int i = 0; i < _scrapList.Count; i++)
			{
				var item = _scrapList[i].LoadItem();
				if(item == null) continue;
				Network.RegisterNetworkPrefab(item.spawnPrefab);
				_scrapList[i].ItemDetails = item;
				ulong itemNameHash = HashItem(item);
				_hashToItem[itemNameHash] = i;

				// Our item still needs to be loaded
				_itemsLeft.Add(itemNameHash);


				Plugin.Log.LogDebug($"registering scrap {i}, {itemNameHash}");
			}
		}

		public void OnPluginStart()
		{
			Level.OnPlayerConnected += Level_OnPlayerConnected;
		}

		private void Level_OnPlayerConnected(ulong clientId)
		{
			CosmicNetworkComponent.Instance.SyncCustomItemsToClient(CustomItemStartIndex, _itemHashListOrdered.ToArray(), clientId);
		}
	}
}
