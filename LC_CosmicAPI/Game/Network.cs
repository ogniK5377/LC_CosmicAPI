using HarmonyLib;
using LC_CosmicAPI.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace LC_CosmicAPI.Game
{
	public abstract class INetworkable<T> : NetworkBehaviour
	{
		public static GameObject Prefab { get; internal set; }
		// Called once on plugin init. This prepares a network prefab with
		// a network object
		public abstract bool SetupNetworkablePrefab(GameObject networkObject);

		public GameObject NetworkInstantiate(bool destroyWithScene = false, ulong? owner = null)
		{
			var gameRef = Instantiate(Prefab);
			if (owner != null)
				gameRef.GetComponent<NetworkObject>().SpawnWithOwnership(owner.Value, destroyWithScene);
			else
				gameRef.GetComponent<NetworkObject>().Spawn(destroyWithScene);
			return gameRef;
		}

		public GameObject NetworkInstantiate(Transform parent, bool destroyWithScene = false, ulong? owner = null)
		{
			var gameRef = Instantiate(Prefab, parent);
			if (owner != null)
				gameRef.GetComponent<NetworkObject>().SpawnWithOwnership(owner.Value, destroyWithScene);
			else
				gameRef.GetComponent<NetworkObject>().Spawn(destroyWithScene);
			return gameRef;
		}

		public GameObject NetworkInstantiate(Transform parent, bool instantiateInWorldSpace, bool destroyWithScene = false, ulong? owner = null)
		{
			var gameRef = Instantiate(Prefab, parent, instantiateInWorldSpace);
			if (owner != null)
				gameRef.GetComponent<NetworkObject>().SpawnWithOwnership(owner.Value, destroyWithScene);
			else
				gameRef.GetComponent<NetworkObject>().Spawn(destroyWithScene);
			return gameRef;
		}

		public GameObject NetworkInstantiate(Vector3 position, Quaternion rotation, bool destroyWithScene = false, ulong? owner = null)
		{
			var gameRef = Instantiate(Prefab, position, rotation);
			if (owner != null)
				gameRef.GetComponent<NetworkObject>().SpawnWithOwnership(owner.Value, destroyWithScene);
			else
				gameRef.GetComponent<NetworkObject>().Spawn(destroyWithScene);
			return gameRef;
		}

		public GameObject NetworkInstantiate(Vector3 position, Quaternion rotation, Transform parent, bool destroyWithScene = false, ulong? owner = null)
		{
			var gameRef = Instantiate(Prefab, position, rotation, parent);
			if (owner != null)
				gameRef.GetComponent<NetworkObject>().SpawnWithOwnership(owner.Value, destroyWithScene);
			else
				gameRef.GetComponent<NetworkObject>().Spawn(destroyWithScene);
			return gameRef;
		}
	}

	public static class Network
	{
		public static Action<GameNetworkManager, NetworkManager> GameNetworkManager_Start;

		private static List<GameObject> _networkObjectsToRegister = new();
		internal static GameObject APINetworkPrefab = null;
		internal static void InvokeGameManagerStart(GameNetworkManager instance)
		{
			var netManager = instance.GetComponent<NetworkManager>();
			foreach (var netObj in _networkObjectsToRegister)
			{
				if (netObj != null) netManager.AddNetworkPrefab(netObj);
			}
			_networkObjectsToRegister.Clear();
			GameNetworkManager_Start?.Invoke(instance, netManager);
		}

		internal static void Startup()
		{
			_networkObjectsToRegister.Clear();
			APINetworkPrefab = BundleManager.LoadAsset<GameObject>("cosmic:/assets/modding/cosmic/network_object.prefab");
			NetworkInitialize();
		}

		internal static bool NetworkInitialize()
		{
			if (APINetworkPrefab == null) return false;
			var types = Assembly.GetCallingAssembly().GetTypes().Where(
				x => x.IsClass && x.BaseType != null && x.BaseType.IsGenericType && x.BaseType.GetGenericTypeDefinition() == typeof(INetworkable<>));
			
			foreach(var type in types)
			{
				var prefab = GameObject.Instantiate(APINetworkPrefab);
				var networkable = Activator.CreateInstance(type);
				var setupMethod = type.GetMethod("SetupNetworkablePrefab");
				var setupResult = setupMethod?.Invoke(networkable, new object[] { prefab }) is bool;
				if (setupResult)
					type.GetProperty("Prefab")?.SetValue(networkable, prefab);
			}
			return true;
		}
	}
}
