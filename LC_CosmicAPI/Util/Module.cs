using BepInEx.Configuration;
using HarmonyLib;
using LC_CosmicAPI.Game;
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

namespace LC_CosmicAPI.Util
{
	public static class Module
	{
		// The current executing plugins name
		public static string PluginName => GetPluginName(Assembly.GetCallingAssembly());
		private static List<IPluginModule> _plugins = new();

		internal static string GetPluginName(Assembly assembly)
		{
			return Path.GetFileNameWithoutExtension(assembly.Location) + Path.DirectorySeparatorChar;
		}
		internal static string GetPluginDir(Assembly assembly)
		{
			return Path.GetDirectoryName(assembly.Location) + Path.DirectorySeparatorChar;
		}

		internal static void RuntimeInitialization(Assembly assembly)
		{
			var types = assembly.GetTypes();
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

		internal static void InvokeAttributeTypes(Assembly assembly, Harmony harmony, ConfigFile config)
		{
			var types = assembly.GetTypes();

			var typesToHandle = types.Where(x =>
			// Plugin start	
			(typeof(IPluginModule).IsAssignableFrom(x) && !x.IsAbstract && x.IsClass) ||
			// Lethal patches
			(x.IsClass && !x.IsAbstract && x.IsSubclassOf(typeof(ILethalPatch))) ||
			// Custom scrap
			((x.IsClass && !x.IsAbstract && x.IsSubclassOf(typeof(ICustomScrap))))
			);

			foreach (var type in typesToHandle)
			{
				if (typeof(IPluginModule).IsAssignableFrom(type))
				{
					InvokePluginStart(type);
				}
				else if (type.IsSubclassOf(typeof(ILethalPatch)))
				{
					InvokeLethalPatch(type, harmony, config);
				}
				else if(type.IsSubclassOf(typeof(ICustomScrap)))
				{
					InvokeCustomScrap(type);
				}
			}

		}

		private static void InvokePluginStart(Type type)
		{
			Plugin.Log.LogInfo($"Plugin start for {type.FullName}");
			var init = Activator.CreateInstance(type) as IPluginModule;
			_plugins.Add(init);
			init.OnPluginStart();
		}

		private static void InvokeLethalPatch(Type patchType, Harmony harmony, ConfigFile config)
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

		internal static void InvokeCustomScrap(Type type)
		{
			Plugin.Log.LogInfo($"Setting up custom scrap {type.FullName}");
			ICustomScrap scrap = Activator.CreateInstance(type) as ICustomScrap;
			CustomScrapManager.AddNewScrap(scrap);
		}

	}
}
