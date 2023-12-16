using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace LC_CosmicAPI.Util
{
	public abstract class ILethalPatch
	{
		public ConfigFile Config { get; set; }
		public abstract bool Preload();
		public virtual void PatchAll(Harmony harmony, Type baseType)
		{
			harmony.PatchAll(baseType);
		}
	}
}
