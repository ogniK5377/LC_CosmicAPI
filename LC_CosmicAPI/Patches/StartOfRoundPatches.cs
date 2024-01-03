using HarmonyLib;
using LC_CosmicAPI.Game;
using LC_CosmicAPI.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace LC_CosmicAPI.Patches
{
	internal class StartOfRoundPatches : ILethalPatch
	{
		private static MethodInfo _OnLoadShipGrabbaleMethodInfo = SymbolExtensions.GetMethodInfo(() => OnLoadShipGrabbableItems());

		private static void AddRemainingItems()
		{
			if (CustomScrapManager._itemsLeft.Count > 0)
			{
				// We have some unloaded items, lets load them in now!
				foreach (var itemHash in CustomScrapManager._itemsLeft)
				{
					var item = CustomScrapManager.GetItemFromHash(itemHash, StartOfRound.Instance.allItemsList.itemsList.Count);

					if (item == null) continue; // oops
					StartOfRound.Instance.allItemsList.itemsList.Add(item);
					CustomScrapManager._itemHashListOrdered.Add(itemHash);
				}
				CustomScrapManager._itemsLeft.Clear();
			}
		}

		private static void OnLoadShipGrabbableItems()
		{
			if(!ES3.KeyExists(CustomScrapManager.HashSaveFileKey, GameNetworkManager.Instance.currentSaveFileName))
			{
				Plugin.Log.LogDebug("No custom items in save");
				AddRemainingItems();
				// We don't have any custom items in our save
				return;
			}
			int currentItemIdCount = StartOfRound.Instance.allItemsList.itemsList.Count;
			CustomScrapManager.CustomItemStartIndex = currentItemIdCount;

			// Index = Item ID saved
			// Value = Item Hash for lookup
			ulong[] itemIdHashList = ES3.Load<ulong[]>(CustomScrapManager.HashSaveFileKey, GameNetworkManager.Instance.currentSaveFileName);

			// Get the first index of where our first custom item exists.
			int firstCustomItemIndex = Array.FindIndex(itemIdHashList, item => item != 0);
			if (firstCustomItemIndex == -1)
			{
				AddRemainingItems();
				return; // No custom items, no need to deal with anything
			}
			// Shifting to allow new items to load between updates and mods
			// as well as handling the deletion of previous custom scrap mods
			int itemIdShift = 0;
			if(firstCustomItemIndex < currentItemIdCount)
			{
				// Items were added to the base game! Lets shift our items to the end of the list
				itemIdShift = currentItemIdCount - firstCustomItemIndex;
			}

			for (int itemId = firstCustomItemIndex; itemId < itemIdHashList.Length; itemId++)
			{
				ulong itemHash = itemIdHashList[itemId];
				// If our hash is 0, skip! Lets consider these to be default built in items
				if (itemHash == 0) continue;

				// Specify our new item ID so mods can keep track
				int newItemIndex = itemId + itemIdShift;

				// Get our item and update the ID!
				var item = CustomScrapManager.GetItemFromHash(itemHash, itemId + itemIdShift);
				if (item == null)
				{
					// The item doesn't exist anymore! Maybe the mod was removed?
					// No matter, lets decrement our item shift and continue so we have no empty slots.
					// Perhaps a placeholder dummy item can work here instead?
					itemIdShift--;
					continue;
				}

				// Add our item to the total item list
				StartOfRound.Instance.allItemsList.itemsList.Add(item);
				CustomScrapManager._itemHashListOrdered.Add(itemHash);
				CustomScrapManager.MarkHashAsLoaded(itemHash);
			}

			AddRemainingItems();
		}

		[HarmonyPatch(typeof(StartOfRound), "LoadShipGrabbableItems")]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> LoadShipGrabbableItemsILPatch(IEnumerable<CodeInstruction> instructions)
		{
			// We want to hook just after shipItemSaveData so we don't have to do reprocessing all the time
			var found = false;
			int nCallCountTillInsert = 2;
			foreach (var instruction in instructions)
			{
				if(nCallCountTillInsert == 0)
				{
					yield return new CodeInstruction(OpCodes.Call, _OnLoadShipGrabbaleMethodInfo);
					nCallCountTillInsert--;
				}
				if(instruction.Is(OpCodes.Ldstr, "shipItemSaveData"))
				{
					found = true;
				}

				if(nCallCountTillInsert > 0 && found && instruction.opcode == OpCodes.Call)
				{
					nCallCountTillInsert--;
				}
				yield return instruction;
			}
			if (found is false || nCallCountTillInsert >= 0)
				Plugin.Log.LogError("Failed to patch LoadShipGrabbableItems");
		}

		[HarmonyPatch(typeof(StartOfRound), "OnClientConnect")]
		[HarmonyPostfix]
		public static void OnClientConnectPatch(StartOfRound __instance, ulong clientId)
		{
			if (!__instance.IsServer) return;
			Level.InvokeOnPlayerConnected(clientId);
		}

		public override bool Preload()
		{
			return true;
		}
	}
}
