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
		public static StartOfRound RoundStart => StartOfRound.Instance != null ? StartOfRound.Instance : UnityEngine.Object.FindObjectOfType<StartOfRound>();
		public static RoundManager Round => RoundManager.Instance != null ? RoundManager.Instance : UnityEngine.Object.FindObjectOfType<RoundManager>();

		public PlayerControllerB PlayerController;
		public PlayerControllerB LocalPlayerController;
		public DeadBodyInfo DeadBody;
		public GameObject PlayerGameObject => PlayerController.gameObject;
		public GameObject LocalPlayerGameObject => LocalPlayerController.gameObject;

		public Transform LocalItemTransform => PlayerController.localItemHolder;
		public Transform ServerItemTransform => PlayerController.serverItemHolder;

		public GameObject AnimatorObject => !IsCosmeticCharacter ? (IsAlive ? 
			PlayerController.playerBodyAnimator.gameObject : 
			DeadBody.GetComponent<SkinnedMeshRenderer>().rootBone.gameObject) :
			GetComponentInChildren<Animator>().gameObject;
		public int PlayerObjectID => IsAlive ? (int)PlayerController.playerClientId : DeadBody.playerObjectId;
		public int LODCount => (IsAlive || IsCosmeticCharacter) ? 3 : 1;
		public SkinnedMeshRenderer PrimaryLOD => IsAlive ? PlayerController.thisPlayerModel : DeadBody.gameObject.GetComponent<SkinnedMeshRenderer>();

		public int SuitID => PlayerController.currentSuitID;
		public Material SuitMaterial => StartOfRound.Instance.unlockablesList.unlockables[SuitID].suitMaterial;
		public ulong NetworkID => PlayerController.actualClientId;
		public ulong SteamID => PlayerController.playerSteamId;
		
		// More company cosmetic viewer
		public bool IsCosmeticCharacter => PlayerController == null && DeadBody == null && Cosmetics.HasCosmeticApplication;
		private CosmeticHelper _cosmeticHelper;
		public CosmeticHelper Cosmetics => _cosmeticHelper;
		public bool HasCosmeticApplication => _cosmeticHelper != null && _cosmeticHelper.HasCosmeticApplication;

		private bool _isLocalPlayer = false;
		private int LastSuitID = 0;
		private ulong _lastSteamId = 0;

		private static MethodInfo CheckConditionsForEmote = typeof(PlayerControllerB).GetMethod("CheckConditionsForEmote", BindingFlags.NonPublic);

		public bool CanPerformEmote => CheckConditionsForEmote?.Invoke(PlayerController, null) is bool && !PlayerController.performingEmote;

		public bool IsDead => DeadBody != null && !IsCosmeticCharacter;
		public bool IsAlive => !IsDead && !IsCosmeticCharacter;
		public bool LocalPlayer => _isLocalPlayer;

		public delegate bool OnDeadBodySpawnDelegate(DeadBodyInfo deadBodyInfo);
		public delegate bool UpdateDeadBodyEnableDelegate(DeadBodyInfo deadBodyInfo, bool setActive);
		public delegate bool UpdatePlayerModelEnabledDelegate(bool setActive);
		public delegate bool UpdatePlayerModelArmsEnabledDelegate(bool setActive);
		public delegate void OnSuitChangedDelegate(int suitId, Material suitMaterial);
		public delegate void SteamIDUpdatedDelegate(ulong steamId);


		public event OnSuitChangedDelegate OnSuitChanged;
		public event Action OnPlayerSpawned;
		public event Action OnPlayerDestroy;
		public event UpdatePlayerModelEnabledDelegate UpdatePlayerModelEnabled;
		public event UpdatePlayerModelArmsEnabledDelegate UpdatePlayerModelArmsEnabled;
		public event Action OnSpawnPlayerAnimator;

		public event OnDeadBodySpawnDelegate OnDeadBodySpawn;
		public event UpdateDeadBodyEnableDelegate UpdateDeadBodyEnable;
		public event SteamIDUpdatedDelegate SteamIDUpdated;

		private Dictionary<string, GameObject> _boneCache = new();


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

		public static PlayerControllerB GetControllerFromPlayerID(int playerId)
		{
			if (RoundStart == null) return null;
			if (playerId < 0) return null;
			if (playerId >= RoundStart.allPlayerScripts.Length) return null;
			return RoundStart.allPlayerScripts[playerId];
		}

		public static GameObject GetPlayerObjectFromPlayerID(int playerId)
		{
			var controller = GetControllerFromPlayerID(playerId);
			if (controller == null) return null;
			return controller.gameObject;
		}

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

		public bool SpawnDeadBodyEv(DeadBodyInfo deadBodyInfo)
		{
			if (OnDeadBodySpawn == null) return false;
			bool wasAnyTrue = false;
			foreach (OnDeadBodySpawnDelegate f in OnDeadBodySpawn?.GetInvocationList())
			{
				wasAnyTrue |= f.Invoke(deadBodyInfo);
			}
			return wasAnyTrue;
		}
		public bool DeadBodyUpdateEv(DeadBodyInfo deadBodyInfo, bool setActive)
		{
			if (UpdateDeadBodyEnable == null) return false;
			bool wasAnyTrue = false;
			foreach (UpdateDeadBodyEnableDelegate f in UpdateDeadBodyEnable?.GetInvocationList())
				wasAnyTrue |= f.Invoke(deadBodyInfo, setActive);
			return wasAnyTrue;
		}


		public bool UpdateModelState(bool newState, bool? disableArms = null)
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

		public void SpawnPlayerAnimatorEv()
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
					var typeList = assembly.GetTypes().Where(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(preloaderSystems));
					if (typeList.Any())
						TypeCache.AddRange(typeList);
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
