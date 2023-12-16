using BepInEx.Configuration;
using HarmonyLib;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using UnityEngine;
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace LC_CosmicAPI.Util
{
	public static class Module
	{
		// The current executing plugins name
		public static string PluginName => Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar;

		internal static void RuntimeInitialization()
		{
			var types = Assembly.GetCallingAssembly().GetTypes();
			foreach (var type in types)
			{
				var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
				foreach (var method in methods)
				{
					var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
					if (attributes.Length > 0)
					{
						method.Invoke(null, null);
					}
				}
			}
		}

		internal static void LoadPatches(Harmony harmony, ConfigFile config)
		{
			var patches = Assembly.GetCallingAssembly().GetTypes().Where(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(typeof(ILethalPatch)));
			Plugin.Log.LogInfo($"Patching {patches.Count()}...");
			foreach (var patchType in patches)
			{
				ILethalPatch patch = Activator.CreateInstance(patchType) as ILethalPatch;
				patch.Config = config;
				if (patch.Preload())
				{
					patch.PatchAll(harmony, patchType);
					Plugin.Log.LogInfo($"Patching {patchType.FullName}");
				}
				else
				{
					Plugin.Log.LogInfo($"Preload failed for {patchType.FullName}");
				}
			}
		}

	}
}
