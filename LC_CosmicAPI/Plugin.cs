using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LC_CosmicAPI.Game;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace LC_CosmicAPI
{
    [BepInPlugin(PluginDetails.GUID, PluginDetails.Name, PluginDetails.Version)]
	public class Plugin : BaseUnityPlugin
    {
        internal static Harmony Harmony { get; private set; } = new(PluginDetails.PatchID);
        internal static ManualLogSource Log { get; private set;} = new(PluginDetails.GUID);
        internal static string PluginPath { get; private set; }

        /// <summary>
        /// Startup the API for your plugin!
        /// Returns true if success!
        /// </summary>
        public static bool InitializeAPI(Harmony harmony, ConfigFile config)
        {
			try
			{
				var assembly = Assembly.GetCallingAssembly();
				Util.Module.InvokeAttributeTypes(assembly, harmony, config);
				Util.Module.RuntimeInitialization(assembly);
				return true;
			}
			catch (Exception ex)
			{
				Log.LogError(ex);
                return false;
			}
		}

        private void Awake()
        {
            // Plugin startup logic
            PluginPath = Path.GetDirectoryName(Info.Location) + Path.DirectorySeparatorChar;
			BepInEx.Logging.Logger.Sources.Add(Log);

#pragma warning disable CS0162 // Unreachable code detected
			if (PluginDetails.AllowDiskLogging)
				BepInEx.Logging.Logger.Listeners.Add(new FilteredDiskLogListener(PluginDetails.GUID, "LC_CosmicAPI.log", 500, LogLevel.All));
#pragma warning restore CS0162 // Unreachable code detected

			var executingAssembly = Assembly.GetExecutingAssembly();
			try
            {
                // Needed for networking RPCs
                Game.Network.Startup();
                Util.Module.RuntimeInitialization(executingAssembly);
				Util.Module.InvokeAttributeTypes(executingAssembly, Harmony, Config);
            }
            catch (Exception ex)
            {
                Log.LogError(ex);
            }
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginDetails.Name} is loaded!");
        }
	}
}