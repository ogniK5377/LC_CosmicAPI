using GameNetcodeStuff;
using System;
using System.Collections.Generic;
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

		public GameObject AnimatorObject => IsAlive ? PlayerController.playerBodyAnimator.gameObject : DeadBody.GetComponent<SkinnedMeshRenderer>().rootBone.gameObject;
		public int PlayerObjectID => IsAlive ? (int)PlayerController.playerClientId : DeadBody.playerObjectId;
		public int LODCount => IsAlive ? 3 : 1;
		public SkinnedMeshRenderer PrimaryLOD => IsAlive ? PlayerController.thisPlayerModel : DeadBody.gameObject.GetComponent<SkinnedMeshRenderer>();

		public int SuitID => PlayerController.currentSuitID;
		public Material SuitMaterial => StartOfRound.Instance.unlockablesList.unlockables[SuitID].suitMaterial;

		public ulong SteamID => PlayerController.playerSteamId;

		private bool _isLocalPlayer = false;
		private int LastSuitID = 0;
		private ulong _lastSteamId = 0;

		public bool IsDead => DeadBody != null;
		public bool IsAlive => !IsDead;
		public bool LocalPlayer => _isLocalPlayer;

		public event Action<int, Material> OnSuitChanged;
		public event Action OnPlayerSpawned;
		public event Action OnPlayerDestroy;
		public event Func<bool, bool> UpdatePlayerModelEnabled;
		public event Func<bool, bool> UpdatePlayerModelArmsEnabled;
		public event Action OnSpawnPlayerAnimator;

		public event Func<DeadBodyInfo, bool> OnDeadBodySpawn;
		public event Func<DeadBodyInfo, bool, bool> UpdateDeadBodyEnable;
		public event Action<ulong> SteamIDUpdated;

		private Dictionary<string, GameObject> _boneCache = new();

		public GameObject GetBoneFromName(string name)
		{
			if (_boneCache.TryGetValue(name, out var cachedBone))
				return cachedBone;

			var baseModel = PlayerGameObject.transform.GetFirstWithName("spine");
			var child = baseModel.GetFirstWithName(name);
			if (child != null)
			{
				_boneCache[name] = baseModel.GetFirstWithName(name).gameObject;
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
			Plugin.Log.LogDebug($"Hello i am {SteamID} and {PlayerObjectID}");
			if (OnDeadBodySpawn == null) return false;
			Plugin.Log.LogDebug(deadBodyInfo);
			Plugin.Log.LogDebug(OnDeadBodySpawn);
			bool wasAnyTrue = false;
			foreach (Func<DeadBodyInfo, bool> f in OnDeadBodySpawn?.GetInvocationList())
			{
				Plugin.Log.LogDebug("iter");
				wasAnyTrue |= f.Invoke(deadBodyInfo);
			}
			return wasAnyTrue;
		}
		public bool DeadBodyUpdateEv(DeadBodyInfo deadBodyInfo, bool setActive)
		{
			if (UpdateDeadBodyEnable == null) return false;
			bool wasAnyTrue = false;
			foreach (Func<DeadBodyInfo, bool, bool> f in UpdateDeadBodyEnable?.GetInvocationList())
				wasAnyTrue |= f.Invoke(deadBodyInfo, setActive);
			return wasAnyTrue;
		}


		public bool UpdateModelState(bool newState, bool? disableArms = null)
		{
			if (UpdatePlayerModelEnabled == null && UpdatePlayerModelArmsEnabled == null) return false;

			bool wasAnyTrue = false;
			if (UpdatePlayerModelEnabled != null)
			{
				foreach (Func<bool, bool> f in UpdatePlayerModelEnabled?.GetInvocationList())
					wasAnyTrue |= f.Invoke(newState);
			}

			if (disableArms != null && UpdatePlayerModelArmsEnabled != null)
			{
				foreach (Func<bool, bool> f in UpdatePlayerModelArmsEnabled?.GetInvocationList())
					wasAnyTrue |= f.Invoke(disableArms.Value);
			}
			return wasAnyTrue;
		}

		public void SpawnPlayerAnimatorEv()
		{
			OnSpawnPlayerAnimator?.Invoke();
		}

		private void Start()
		{
			Plugin.Log.LogDebug("Controller Helper Init");
			PlayerController = GetComponent<PlayerControllerB>();
			DeadBody = GetComponent<DeadBodyInfo>();

			if (IsDead) PlayerController = DeadBody.playerScript;
			_isLocalPlayer = (PlayerController.IsOwner && PlayerController.isPlayerControlled) && (!PlayerController.IsServer || PlayerController.isHostPlayerObject) || PlayerController.isTestingPlayer;

			if (_isLocalPlayer) LocalPlayerController = PlayerController;

			OnPlayerSpawned?.Invoke();
			HandleSuitChange();
		}

		private void LateUpdate()
		{
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
			else
			{
				if (index != 0) return null;
				return DeadBody.gameObject.GetComponent<SkinnedMeshRenderer>();
			}
		}

		public void SetModelLODState(bool currentState)
		{
			for (int i = 0; i < LODCount; i++)
			{
				var lod = GetLOD(i);
				if (lod != null) lod.enabled = currentState;
			}
		}
	}
}
