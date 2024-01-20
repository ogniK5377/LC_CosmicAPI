using GameNetcodeStuff;
using LC_CosmicAPI.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace LC_CosmicAPI.Game
{
	public static class InternalPlayerControllerBExtensions
	{
		public static ControllerHelper Helper(this PlayerControllerB controller)
		{
			if (controller == null) return null;
			if (controller.gameObject == null) return null;
			var helper = controller.gameObject.GetComponent<ControllerHelper>();
			if (!helper) 
				helper = controller.gameObject.AddComponent<ControllerHelper>();
			return helper;
		}
	}

	[DefaultExecutionOrder(-2)]
	public class ControllerHelper : MonoBehaviour
	{
		/// <summary>
		/// Get current player controller
		/// </summary>
		public PlayerControllerB PlayerController;

		/// <summary>
		/// The local player controller
		/// </summary>
		public static PlayerControllerB LocalPlayerController;

		/// <summary>
		/// The players dead body info
		/// </summary>
		public DeadBodyInfo DeadBody;

		/// <summary>
		/// The current players game object
		/// </summary>
		public GameObject PlayerGameObject => PlayerController.gameObject;

		/// <summary>
		/// The local players game object
		/// </summary>
		public static GameObject LocalPlayerGameObject => LocalPlayerController.gameObject;

		/// <summary>
		/// The local item transform. This is where players hold items on their view model
		/// </summary>
		public Transform LocalItemTransform => PlayerController.localItemHolder;

		/// <summary>
		/// The server item transform. This is where players hold items for everyone else
		/// </summary>
		public Transform ServerItemTransform => PlayerController.serverItemHolder;

		/// <summary>
		/// Returns the current Animator or SkinnedMeshRenderer game object
		/// </summary>
		public GameObject AnimatorObject => !IsCosmeticCharacter ? (IsAlive ? 
			PlayerController.playerBodyAnimator.gameObject : 
			DeadBody.GetComponent<SkinnedMeshRenderer>().rootBone.gameObject) :
			GetComponentInChildren<Animator>().gameObject;

		/// <summary>
		/// Gets the current players object ID
		/// </summary>
		public int PlayerObjectID => IsAlive ? (int)PlayerController.playerClientId : DeadBody.playerObjectId;
		
		/// <summary>
		/// Returns the amount of LODs the player has
		/// </summary>
		public int LODCount => (IsAlive || IsCosmeticCharacter) ? 3 : 1;

		/// <summary>
		/// Get the primary LOD SkinnedMeshRenderer
		/// </summary>
		public SkinnedMeshRenderer PrimaryLOD => IsAlive ? PlayerController.thisPlayerModel : DeadBody.gameObject.GetComponent<SkinnedMeshRenderer>();

		/// <summary>
		/// The current SuitID the player is wearing
		/// </summary>
		public int SuitID => PlayerController.currentSuitID;

		/// <summary>
		/// The current Material of the suit the player is wearing
		/// </summary>
		public Material SuitMaterial => StartOfRound.Instance.unlockablesList.unlockables[SuitID].suitMaterial;
		
		/// <summary>
		/// The current network ID of the player or the client id. This is used for networking!
		/// </summary>
		public ulong NetworkID => PlayerController.actualClientId;
		
		/// <summary>
		/// The clients current SteamID
		/// </summary>
		public ulong SteamID => PlayerController.playerSteamId;

		/// <summary>
		/// Are we attached to an object which has no controller or dead body and MoreCompany is installed
		/// </summary>
		public bool IsCosmeticCharacter => PlayerController == null && DeadBody == null && Cosmetics.HasCosmeticApplication;
		private CosmeticHelper _cosmeticHelper;
		
		/// <summary>
		/// Returns the current cosmetic helper for MoreCompany cosmetics. This is null if MoreCompany is not installed
		/// </summary>
		public CosmeticHelper Cosmetics => _cosmeticHelper;

		/// <summary>
		/// Checks if our object can have cosmetics and MoreCompany is installed
		/// </summary>
		public bool HasCosmeticApplication => _cosmeticHelper != null && _cosmeticHelper.HasCosmeticApplication;

		private bool _isLocalPlayer = false;
		private int LastSuitID = 0;
		private ulong _lastSteamId = 0;

		private static MethodInfo CheckConditionsForEmote = typeof(PlayerControllerB).GetMethod("CheckConditionsForEmote", BindingFlags.NonPublic);

		/// <summary>
		/// Check if the player can Emote
		/// </summary>
		public bool CanPerformEmote => CheckConditionsForEmote?.Invoke(PlayerController, null) is bool && !PlayerController.performingEmote;

		/// <summary>
		/// Check if the player is dead
		/// </summary>
		public bool IsDead => DeadBody != null && !IsCosmeticCharacter;

		/// <summary>
		/// Check if the player is alive
		/// </summary>
		public bool IsAlive => !IsDead && !IsCosmeticCharacter;

		/// <summary>
		/// Check if we're currently looking at the localplayer
		/// </summary>
		public bool LocalPlayer => _isLocalPlayer;

		public delegate bool OnDeadBodySpawnDelegate(DeadBodyInfo deadBodyInfo);
		public delegate bool UpdateDeadBodyEnableDelegate(DeadBodyInfo deadBodyInfo, bool setActive);
		public delegate bool UpdatePlayerModelEnabledDelegate(bool setActive);
		public delegate bool UpdatePlayerModelArmsEnabledDelegate(bool setActive);
		public delegate void OnSuitChangedDelegate(int suitId, Material suitMaterial);
		public delegate void SteamIDUpdatedDelegate(ulong steamId);

		/// <summary>
		/// Event is fired when the player changes their suit
		/// </summary>
		public event OnSuitChangedDelegate OnSuitChanged;

		/// <summary>
		/// Event is fired the player is spawned
		/// </summary>
		public event Action OnPlayerSpawned;

		/// <summary>
		/// Event is fired then the player object is destroyed
		/// </summary>
		public event Action OnPlayerDestroy;

		/// <summary>
		/// Event is fired when the player model is set to be active or disabled
		/// </summary>
		public event UpdatePlayerModelEnabledDelegate UpdatePlayerModelEnabled;

		/// <summary>
		/// Event is fired when the viewmodel is set to be active or disabled
		/// </summary>
		public event UpdatePlayerModelArmsEnabledDelegate UpdatePlayerModelArmsEnabled;

		/// <summary>
		/// Event is fired when the player animator is spawned
		/// </summary>
		public event Action OnSpawnPlayerAnimator;

		/// <summary>
		/// Event is fired the dead body is spawned
		/// </summary>
		public event OnDeadBodySpawnDelegate OnDeadBodySpawn;

		/// <summary>
		/// Event is fired when the dead bodies active state is changed
		/// </summary>
		public event UpdateDeadBodyEnableDelegate UpdateDeadBodyEnable;

		/// <summary>
		/// Event is fired when the steam ID is changed. This is useful for running
		/// SteamID restricted routines on a player.
		/// </summary>
		public event SteamIDUpdatedDelegate SteamIDUpdated;

		private Dictionary<string, GameObject> _boneCache = new();

		/// <summary>
		/// Find a bone within the player with a specific name
		/// </summary>
		/// <param name="name">Bone name</param>
		/// <returns>Bones gameObject</returns>
		public GameObject GetBoneFromName(string name)
		{
			if (_boneCache.TryGetValue(name, out var cachedBone))
				return cachedBone;

			var baseModel = PlayerGameObject.transform.GetFirstWithName("spine");

			var child = baseModel.GetFirstWithName(name);
			if (child != null)
			{
				_boneCache[name] = child.gameObject;
			}
			else
			{
				_boneCache[name] = null;
			}
			return _boneCache[name];
		}

		/// <summary>
		/// Get a player controller from a given player id
		/// </summary>
		/// <param name="playerId">Player ID</param>
		/// <returns>PlayerControllerB</returns>
		public static PlayerControllerB GetControllerFromPlayerID(int playerId)
		{
			if (Level.StartOfRound == null) return null;
			if (playerId < 0) return null;
			if (playerId >= Level.StartOfRound.allPlayerScripts.Length) return null;
			return Level.StartOfRound.allPlayerScripts[playerId];
		}

		/// <summary>
		/// Get a player game object from a given player id
		/// </summary>
		/// <param name="playerId">Player ID</param>
		/// <returns>Player GameObject</returns>
		public static GameObject GetPlayerObjectFromPlayerID(int playerId)
		{
			var controller = GetControllerFromPlayerID(playerId);
			if (controller == null) return null;
			return controller.gameObject;
		}

		/// <summary>
		/// Get the ControllerHelper from a player object
		/// </summary>
		/// <param name="obj">PlayerObject</param>
		/// <returns>ControllerHelper</returns>
		public static ControllerHelper GetHelperFromObject(GameObject obj)
		{
			ControllerHelper helper = null;
			helper = obj.GetComponent<ControllerHelper>();
			if (helper == null)
			{
				var deadBodyInfo = obj.GetComponent<DeadBodyInfo>();
				if (deadBodyInfo != null)
					if (deadBodyInfo.playerScript.gameObject != null)
						helper = deadBodyInfo.playerScript.gameObject.GetComponent<ControllerHelper>();
			}
			return helper;
		}

		/// <summary>
		/// Get the ControllerHelper from a given player id
		/// </summary>
		/// <param name="playerId">Player ID</param>
		/// <returns>ControllerHelper</returns>
		public static ControllerHelper GetHelperFromPlayerId(int playerId)
		{
			var playerObject = GetControllerFromPlayerID(playerId);
			if (playerObject == null) return null;
			return playerObject.GetComponent<ControllerHelper>();
		}

		private void HandleSuitChange()
		{
			if (PlayerController == null) return;
			if (PlayerController.currentSuitID < 0) return;
			if (StartOfRound.Instance == null) return;
			if (StartOfRound.Instance.unlockablesList == null) return;
			if (StartOfRound.Instance.unlockablesList.unlockables == null) return;
			if (StartOfRound.Instance.unlockablesList.unlockables.Count < PlayerController.currentSuitID) return;
			Material material = StartOfRound.Instance.unlockablesList.unlockables[PlayerController.currentSuitID].suitMaterial;
			OnSuitChanged?.Invoke(PlayerController.currentSuitID, material);
		}

		internal bool SpawnDeadBodyEv(DeadBodyInfo deadBodyInfo)
		{
			if (OnDeadBodySpawn == null) return false;
			bool wasAnyTrue = false;
			foreach (OnDeadBodySpawnDelegate f in OnDeadBodySpawn?.GetInvocationList())
			{
				wasAnyTrue |= f.Invoke(deadBodyInfo);
			}
			return wasAnyTrue;
		}
		internal bool DeadBodyUpdateEv(DeadBodyInfo deadBodyInfo, bool setActive)
		{
			if (UpdateDeadBodyEnable == null) return false;
			bool wasAnyTrue = false;
			foreach (UpdateDeadBodyEnableDelegate f in UpdateDeadBodyEnable?.GetInvocationList())
				wasAnyTrue |= f.Invoke(deadBodyInfo, setActive);
			return wasAnyTrue;
		}


		internal bool UpdateModelState(bool newState, bool? disableArms = null)
		{
			if (UpdatePlayerModelEnabled == null && UpdatePlayerModelArmsEnabled == null) return false;

			bool wasAnyTrue = false;
			if (UpdatePlayerModelEnabled != null)
			{
				foreach (UpdatePlayerModelEnabledDelegate f in UpdatePlayerModelEnabled?.GetInvocationList())
					wasAnyTrue |= f.Invoke(newState);
			}

			if (disableArms != null && UpdatePlayerModelArmsEnabled != null)
			{
				foreach (UpdatePlayerModelArmsEnabledDelegate f in UpdatePlayerModelArmsEnabled?.GetInvocationList())
					wasAnyTrue |= f.Invoke(disableArms.Value);
			}
			return wasAnyTrue;
		}

		internal void SpawnPlayerAnimatorEv()
		{
			OnSpawnPlayerAnimator?.Invoke();
		}

		private static List<Type> TypeCache = null;

		private void Start()
		{
			PlayerController = GetComponent<PlayerControllerB>();
			DeadBody = GetComponent<DeadBodyInfo>();
			if (Plugin.HasMoreCompany)
				_cosmeticHelper = new(AnimatorObject);
			else
				_cosmeticHelper = null;

			if (!IsCosmeticCharacter)
			{
				if (IsDead) PlayerController = DeadBody.playerScript;
				_isLocalPlayer = (PlayerController.IsOwner && PlayerController.isPlayerControlled) && (!PlayerController.IsServer || PlayerController.isHostPlayerObject) || PlayerController.isTestingPlayer;
				if (_isLocalPlayer) LocalPlayerController = PlayerController;
			}

			var preloaderSystems = typeof(IControllerSystemPreloader);

			if(TypeCache == null)
			{
				TypeCache = new();
				foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					try
					{
						var typeList = assembly.GetTypes().Where(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(preloaderSystems));
						if (typeList.Any())
							TypeCache.AddRange(typeList);
					}
					catch(Exception ex)
					{
						Plugin.Log.LogError(ex);
					}
				}
			}


			var systems = TypeCache;
			foreach (var system in systems)
			{
				if (GetComponent(system) == null)
				{
					var obj = gameObject.AddComponent(system) as IControllerSystemPreloader;
					obj.OnPreload(this, PlayerController);
				}
			}
			

			OnPlayerSpawned?.Invoke();
			HandleSuitChange();
		}

		private void LateUpdate()
		{
			if(IsCosmeticCharacter)
			{
				return;
			}
			if (LastSuitID != PlayerController.currentSuitID)
			{
				LastSuitID = PlayerController.currentSuitID;
				HandleSuitChange();
			}
			if (_lastSteamId != PlayerController.playerSteamId)
			{
				_lastSteamId = PlayerController.playerSteamId;
				SteamIDUpdated?.Invoke(_lastSteamId);
			}
		}

		private void OnDestroy()
		{
			OnPlayerDestroy?.Invoke();
		}

		/// <summary>
		/// Get an LOD from a given index
		/// </summary>
		/// <param name="index">LOD index</param>
		/// <returns>SkinnedMeshRenderer of the given LOD index</returns>
		public SkinnedMeshRenderer GetLOD(int index)
		{
			if (index >= LODCount || index < 0) return null;
			if (IsAlive)
			{
				return index switch
				{
					0 => PlayerController.thisPlayerModel,
					1 => PlayerController.thisPlayerModelLOD1,
					2 => PlayerController.thisPlayerModelLOD2,
					_ => null,
				};
			}
			else if(!IsCosmeticCharacter)
			{
				if (index != 0) return null;
				return DeadBody.gameObject.GetComponent<SkinnedMeshRenderer>();
			} else
			{
				return gameObject.transform.Find("LOD" + (index + 1)).gameObject.GetComponent<SkinnedMeshRenderer>();
			}
		}

		/// <summary>
		/// Sets all of the LODs active and shadowcasting modes 
		/// </summary>
		/// <param name="currentState"></param>
		public void SetModelLODState(bool currentState)
		{
			for (int i = 0; i < LODCount; i++)
			{
				var lod = GetLOD(i);
				if (lod != null)
				{
					lod.enabled = currentState;
					lod.shadowCastingMode = currentState ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
				}
			}
		}
	}
}
