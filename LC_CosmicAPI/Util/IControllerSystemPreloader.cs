using GameNetcodeStuff;
using LC_CosmicAPI.Game;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LC_CosmicAPI.Util
{
	public abstract class IControllerSystemPreloader : MonoBehaviour
	{
		public abstract void OnPreload(ControllerHelper helper, PlayerControllerB playerController);
	}
}
