using BepInEx.Bootstrap;
using HarmonyLib;
using LC_CosmicAPI.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace LC_CosmicAPI.Game
{
	internal static class INetworkPrefabTypeFactory<T>
	{
		public static GameObject NetworkPrefab { get; internal set; } = null;
	}

	public static class INetworkableExtensions
	{
		public static GameObject NetworkInstantiate<T>(this INetworkable<T> self, bool destroyWithScene = false, ulong? owner = null)
		{
			var gameRef = UnityEngine.Object.Instantiate(INetworkPrefabTypeFactory<T>.NetworkPrefab);
			if (owner != null)
				gameRef.GetComponent<NetworkObject>().SpawnWithOwnership(owner.Value, destroyWithScene);
			else
				gameRef.GetComponent<NetworkObject>().Spawn(destroyWithScene);
			return gameRef;
		}

		public static GameObject NetworkInstantiate<T>(Transform parent, bool destroyWithScene = false, ulong? owner = null)
		{
			var gameRef = UnityEngine.Object.Instantiate(INetworkPrefabTypeFactory<T>.NetworkPrefab);
			if (owner != null)
				gameRef.GetComponent<NetworkObject>().SpawnWithOwnership(owner.Value, destroyWithScene);
			else
				gameRef.GetComponent<NetworkObject>().Spawn(destroyWithScene);
			return gameRef;
		}

		public static GameObject NetworkInstantiate<T>(Transform parent, bool instantiateInWorldSpace, bool destroyWithScene = false, ulong? owner = null)
		{
			var gameRef = UnityEngine.Object.Instantiate(INetworkPrefabTypeFactory<T>.NetworkPrefab, parent, instantiateInWorldSpace);
			if (owner != null)
				gameRef.GetComponent<NetworkObject>().SpawnWithOwnership(owner.Value, destroyWithScene);
			else
				gameRef.GetComponent<NetworkObject>().Spawn(destroyWithScene);
			return gameRef;
		}

		public static GameObject NetworkInstantiate<T>(Vector3 position, Quaternion rotation, bool destroyWithScene = false, ulong? owner = null)
		{
			var gameRef = UnityEngine.Object.Instantiate(INetworkPrefabTypeFactory<T>.NetworkPrefab, position, rotation);
			if (owner != null)
				gameRef.GetComponent<NetworkObject>().SpawnWithOwnership(owner.Value, destroyWithScene);
			else
				gameRef.GetComponent<NetworkObject>().Spawn(destroyWithScene);
			return gameRef;
		}

		public static GameObject NetworkInstantiate<T>(Vector3 position, Quaternion rotation, Transform parent, bool destroyWithScene = false, ulong? owner = null)
		{
			var gameRef = UnityEngine.Object.Instantiate(INetworkPrefabTypeFactory<T>.NetworkPrefab, position, rotation, parent);
			if (owner != null)
				gameRef.GetComponent<NetworkObject>().SpawnWithOwnership(owner.Value, destroyWithScene);
			else
				gameRef.GetComponent<NetworkObject>().Spawn(destroyWithScene);
			return gameRef;
		}
	}

	public interface INetworkable<T>
	{
		public abstract bool SetupNetworkablePrefab(GameObject networkObject);	
	}

	public static class Network
	{
		public delegate void OnGameNetworkManagerStartDelegate(GameNetworkManager gameNetworkManager, NetworkManager networkManager);
		public static event OnGameNetworkManagerStartDelegate OnGameNetworkManagerStart;

		public static bool IsServer => Level.RoundManager.IsServer;

		private static bool _hasStarted = false;
		private static readonly List<GameObject> _networkObjectsToRegister = new();
		internal static GameObject APINetworkPrefab = null;
		internal static void InvokeGameManagerStart(GameNetworkManager instance)
		{
			var netManager = instance.GetComponent<NetworkManager>();
			NetworkInitialize();
			foreach (var netObj in _networkObjectsToRegister)
			{
				if (netObj != null) netManager.AddNetworkPrefab(netObj);
			}
			_networkObjectsToRegister.Clear();
			_hasStarted = true;
			OnGameNetworkManagerStart?.Invoke(instance, netManager);
			CustomScrapManager.RegisterScrap();
		}

		public static void RegisterNetworkPrefab(GameObject prefab)
		{
			if (!_hasStarted) _networkObjectsToRegister.Add(prefab);
			else GameNetworkManager.Instance.GetComponent<NetworkManager>().AddNetworkPrefab(prefab);
		}

		public static GameObject Instantiate<T>(bool destroyWithScene = false, ulong? owner = null)
		{
			var gameRef = UnityEngine.Object.Instantiate(INetworkPrefabTypeFactory<T>.NetworkPrefab);
			if (owner != null)
				gameRef.GetComponent<NetworkObject>().SpawnWithOwnership(owner.Value, destroyWithScene);
			else
				gameRef.GetComponent<NetworkObject>().Spawn(destroyWithScene);
			return gameRef;
		}

		public static GameObject Instantiate<T>(Transform parent, bool destroyWithScene = false, ulong? owner = null)
		{
			var gameRef = UnityEngine.Object.Instantiate(INetworkPrefabTypeFactory<T>.NetworkPrefab);
			if (owner != null)
				gameRef.GetComponent<NetworkObject>().SpawnWithOwnership(owner.Value, destroyWithScene);
			else
				gameRef.GetComponent<NetworkObject>().Spawn(destroyWithScene);
			return gameRef;
		}

		public static GameObject Instantiate<T>(Transform parent, bool instantiateInWorldSpace, bool destroyWithScene = false, ulong? owner = null)
		{
			var gameRef = UnityEngine.Object.Instantiate(INetworkPrefabTypeFactory<T>.NetworkPrefab, parent, instantiateInWorldSpace);
			if (owner != null)
				gameRef.GetComponent<NetworkObject>().SpawnWithOwnership(owner.Value, destroyWithScene);
			else
				gameRef.GetComponent<NetworkObject>().Spawn(destroyWithScene);
			return gameRef;
		}

		public static GameObject Instantiate<T>(Vector3 position, Quaternion rotation, bool destroyWithScene = false, ulong? owner = null)
		{
			var gameRef = UnityEngine.Object.Instantiate(INetworkPrefabTypeFactory<T>.NetworkPrefab, position, rotation);
			if (owner != null)
				gameRef.GetComponent<NetworkObject>().SpawnWithOwnership(owner.Value, destroyWithScene);
			else
				gameRef.GetComponent<NetworkObject>().Spawn(destroyWithScene);
			return gameRef;
		}

		public static GameObject NetworkInstantiate<T>(Vector3 position, Quaternion rotation, Transform parent, bool destroyWithScene = false, ulong? owner = null)
		{
			var gameRef = UnityEngine.Object.Instantiate(INetworkPrefabTypeFactory<T>.NetworkPrefab, position, rotation, parent);
			if (owner != null)
				gameRef.GetComponent<NetworkObject>().SpawnWithOwnership(owner.Value, destroyWithScene);
			else
				gameRef.GetComponent<NetworkObject>().Spawn(destroyWithScene);
			return gameRef;
		}

		internal static void Startup()
		{
			_networkObjectsToRegister.Clear();
			APINetworkPrefab = BundleManager.LoadAsset<GameObject>("cosmic:/assets/modding/cosmic/network_object.prefab");
			Plugin.Log.LogDebug($"Loaded API network prefab {APINetworkPrefab}");
		}

		internal static void SetNetworkPrefab(Type type, GameObject prefab)
		{
			var property = typeof(INetworkPrefabTypeFactory<>).MakeGenericType(type).GetProperty("NetworkPrefab", BindingFlags.Static | BindingFlags.Public);
			property?.SetValue(null, prefab);
		}

		internal static bool NetworkInitialize()
		{
			if (APINetworkPrefab == null) return false;
			//if (assembly == null) assembly = Assembly.GetCallingAssembly();

			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				var types = assembly.GetTypes().Where(x => x.IsClass && x.BaseType != null && x.BaseType.IsGenericType && x.BaseType.GetGenericTypeDefinition() == typeof(INetworkable<>));
				if (types.Any())
				{
					foreach (var type in types)
					{
						var prefab = GameObject.Instantiate(APINetworkPrefab);
						prefab.hideFlags = HideFlags.HideAndDontSave;
						var networkable = Activator.CreateInstance(type);
						var setupMethod = type.GetMethod("SetupNetworkablePrefab");
						var setupResult = setupMethod?.Invoke(networkable, new object[] { prefab }) is bool;

						SetNetworkPrefab(type, prefab);
						/*var setMethod = type.BaseType.GetMethod("set_NetworkPrefab", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
						if (setupResult)
							setMethod?.Invoke(null, new object[] { prefab });*/
						_networkObjectsToRegister.Add(prefab);
					}
				}
			}

			return true;
		}
	}
}
