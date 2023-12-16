using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
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
				Util.Module.LoadPatches(harmony, config);
				Util.Module.RuntimeInitialization();
                Game.Network.NetworkInitialize();
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

			new ILHook(typeof(StackTrace).GetMethod("AddFrames", BindingFlags.Instance | BindingFlags.NonPublic), IlHook);


#pragma warning disable CS0162 // Unreachable code detected
			if (PluginDetails.AllowDiskLogging)
				BepInEx.Logging.Logger.Listeners.Add(new FilteredDiskLogListener(PluginDetails.GUID, "LC_CosmicAPI.log", 500, LogLevel.All));
#pragma warning restore CS0162 // Unreachable code detected

            try
            {
                // Needed for networking RPCs
                Game.Network.Startup();
                Util.Module.RuntimeInitialization();
                Util.Module.LoadPatches(Harmony, Config);
            }
            catch (Exception ex)
            {
                Log.LogError(ex);
            }
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginDetails.Name} is loaded!");
        }



		private void IlHook(ILContext il)
		{
			var cursor = new ILCursor(il);
			cursor.GotoNext(
				x => x.MatchCallvirt(typeof(StackFrame).GetMethod("GetFileLineNumber", BindingFlags.Instance | BindingFlags.Public))
			);
			cursor.RemoveRange(2);
			cursor.EmitDelegate<Func<StackFrame, string>>(GetLineOrIL);
		}

		private static string GetLineOrIL(StackFrame instance)
		{
			var line = instance.GetFileLineNumber();
			if (line == StackFrame.OFFSET_UNKNOWN || line == 0)
			{
				return "IL_" + instance.GetILOffset().ToString("X4");
			}

			return line.ToString();
		}
	}
}