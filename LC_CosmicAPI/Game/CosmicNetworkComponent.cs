using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace LC_CosmicAPI.Game
{
	internal class CosmicNetworkComponent : NetworkBehaviour, INetworkable<CosmicNetworkComponent>
	{
		public bool SetupNetworkablePrefab(GameObject networkObject)
		{
			networkObject.AddComponent<CosmicNetworkComponent>();
			return true;
		}

		static CosmicNetworkComponent _instance = null;
		public static CosmicNetworkComponent Instance {
			get
			{
				// If it exist, lets use that
				if(_instance != null) return _instance;
				
				// Lets search for an already created object and store it
				_instance = UnityEngine.Object.FindObjectOfType<CosmicNetworkComponent>();
				if(_instance != null ) return _instance;

				// Still nothing, lets create it and store it!
				_instance = Network.Instantiate<CosmicNetworkComponent>(true).GetComponent<CosmicNetworkComponent>();
				return _instance;
			}
		}

		public void SyncCustomItemsToClient(int startItemIndex, ulong[] itemHashes, ulong clientId)
		{
			if (!IsServer) return;
			ClientRpcParams clientRpcParams = new ClientRpcParams
			{
				Send = new ClientRpcSendParams
				{
					TargetClientIds = new ulong[] { clientId }
				}
			};

			SyncCustomItems_ClientRpc(startItemIndex, itemHashes, clientRpcParams);
		}

		public void SyncCustomItemsToClient(int startItemIndex, ulong[] itemHashes, ControllerHelper controllerHelper) =>
			SyncCustomItemsToClient(startItemIndex, itemHashes, controllerHelper.NetworkID);

		[ClientRpc]
		private void SyncCustomItems_ClientRpc(int startItemIndex, ulong[] itemHashes, ClientRpcParams clientRpcParams = default)
		{
			if(itemHashes.Length == 0 || IsOwner)
			{
				return;
			}

			int currentItemCount = Level.ItemList.Count;

			// Deterministic missing item replacements. Fixed seed so a collection of people who don't have the mod will see
			// the same items
			System.Random rand = new(13377331);

			// We're also using startItemIndex here to ensure we actually are synced. A lot of mods just will randomly add items anywhere
			// and just pray that everything links up. Forcing the sync to take place and match the index of the server ensures that we have no issues.
			for(int i = 0; i < itemHashes.Length; i++)
			{
				// We increment the random each time so everyone who doesn't have a specific mod will see the same items
				// It will prevent confusion between the situation like, "Hey get the X item. Where? You mean the Y item?"
				int randomItemId = rand.Next(0, currentItemCount);
				int desiredIndex = startItemIndex + i;

				var item = CustomScrapManager.GetItemFromHash(itemHashes[i], desiredIndex);
				if (item == null) {
					// Invalid hash or our item doesn't exist!
					// Lets use our randomItemId to insert a placeholder for the custom item here
					// so everything can still sync
					Level.ItemList.Insert(desiredIndex, Level.ItemList[randomItemId]);
					continue;
				}

				// Add our custom item so everything is in the same index
				Level.ItemList.Insert(desiredIndex, item);
			}
		}
	}
}
