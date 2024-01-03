using HarmonyLib;
using LC_CosmicAPI.Game;
using LC_CosmicAPI.Util;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace LC_CosmicAPI.Patches
{
	internal class NetworkPatches : ILethalPatch
	{
		private static MethodInfo _OnBeginSavingItems = SymbolExtensions.GetMethodInfo(() => OnBeginSavingItems());

		private static void OnBeginSavingItems()
		{
			string saveFileName = GameNetworkManager.Instance.currentSaveFileName;
			List<ulong> itemHashes = new();

			int itemIdCount = StartOfRound.Instance.allItemsList.itemsList.Count;

			for (int itemIndex = 0; itemIndex < itemIdCount; itemIndex++)
			{
				// Built in items
				if(!CustomScrapManager.IsCustomItemFromId(itemIndex))
				{
					itemHashes.Add(0);
					continue;
				}

				// Store the custom hash for this current item index
				var item = StartOfRound.Instance.allItemsList.itemsList[itemIndex];
				var itemHash = CustomScrapManager.HashItem(item);
				itemHashes.Add(itemHash);
			}

			// Save our hashes to the save file!
			ES3.Save<ulong[]>(CustomScrapManager.HashSaveFileKey, itemHashes.ToArray(), saveFileName);
		}

		[HarmonyPatch(typeof(GameNetworkManager), "Start")]
		[HarmonyPostfix]
		private static void GameNetworkManagerStart(ref GameNetworkManager __instance)
		{
			Network.InvokeGameManagerStart(__instance);
		}

		[HarmonyPatch(typeof(GameNetworkManager), "SaveItemsInShip")]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> SaveItemsInShipILPatch(IEnumerable<CodeInstruction> instructions)
		{
			// Gotta patch this better

			var es3DeleteKey = AccessTools.Method(typeof(ES3), "DeleteKey", new System.Type[] { typeof(string), typeof(string) });
			bool bFoundStr = false;
			bool bFound = false;
			bool bFound2 = false;
			bool bFound3 = false;

			foreach (var instruction in instructions)
			{

				if (!bFoundStr)
				{
					if (instruction.Is(OpCodes.Ldstr, "shipItemSaveData")) bFoundStr = true;
					yield return instruction;
				}
				else if (bFoundStr && !bFound && instruction.Calls(es3DeleteKey))
				{
					yield return instruction;

					// Auto delete key
					// ES3.DeleteKey(CustomScrapManager.HashSaveFileKey, this.currentSaveFileName);
					yield return new CodeInstruction(OpCodes.Ldstr, CustomScrapManager.HashSaveFileKey);
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GameNetworkManager), "currentSaveFileName"));
					yield return new CodeInstruction(OpCodes.Call, es3DeleteKey);

					bFound = true;
				}
				else if (bFound && !bFound2 && instruction.Is(OpCodes.Ldstr, "shipGrabbableItemPos"))
				{
					yield return instruction;
					bFound2 = true;
				}
				else if(bFound && bFound2 && !bFound3 && instruction.opcode == OpCodes.Ret)
				{
					yield return new CodeInstruction(OpCodes.Call, _OnBeginSavingItems);
					yield return instruction;
					bFound3 = true;
				}
				else
				{
					yield return instruction;
				}
			}


			if (!bFound || !bFound2)
			{
				Plugin.Log.LogError("Failed to patch SaveItemsInShip");
				if (!bFound) Plugin.Log.LogError("   Patch1 Failure");
				if (!bFound2) Plugin.Log.LogError("   Patch2 Failure");
			} else
			{
				Plugin.Log.LogDebug("Patching successful for SaveItemsInShip");
			}
		}

		public override bool Preload()
		{
			return true;
		}
	}
}
