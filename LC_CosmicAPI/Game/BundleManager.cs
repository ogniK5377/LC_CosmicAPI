using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Unity.Netcode;
using System.Reflection;
using LC_CosmicAPI.Util;

namespace LC_CosmicAPI.Game
{
	public static class BundleManager
	{
		private static Dictionary<string, AssetBundle> Bundles = new();
		private static Dictionary<string, UnityEngine.Object> LoadedAssets = new();

		private static string AssetBundlePath(Assembly assembly, string assetBundleSm, string path)
		{
			(string pluginName, string assetBundle) = GetPluginAndBundleFromBundle(assembly, assetBundleSm);
			return pluginName + '|' + assetBundle + ":/" + path;
		}

		private static (string, string) GetPluginAndBundleFromBundle(Assembly assembly, string bundleName)
		{
			if (!bundleName.Contains('|'))
			{
				return (Util.Module.GetPluginName(assembly), bundleName);
			}
			else
			{
				var bundleSplit = bundleName.Split('|');
				return (bundleSplit[0], bundleSplit[1]);
			}
		}

		private static string GetBundleFilename(string bundleName)
		{
			if (!bundleName.Contains('|'))
				return bundleName;
			return bundleName.Split('|')[1];
		}

		private static string GetProperBundleName(Assembly assembly, string bundleName)
		{
			if (!bundleName.Contains('|'))
			{
				(string plugin, string bundle) = GetPluginAndBundleFromBundle(assembly, bundleName);
				return plugin + '|' + bundle;
			}
			return bundleName;
		}

		/// <summary>
		/// Loads an asset from a bundle or fetches the asset from a cache.
		/// This method uses the bundlename:/Path/Within/Bundle format!
		/// </summary>
		/// <typeparam name="T">Unity asset type</typeparam>
		/// <param name="bundleWithPath">The asset bundles name as the "drive" and the path within the asset</param>
		/// <returns>The loaded asset</returns>
		/// <exception cref="Exception">Failed to find asset or load it!</exception>
		public static T LoadAsset<T>(string bundleWithPath) where T : UnityEngine.Object
		{
			string[] splitStr = bundleWithPath.Split(":/");
			if (splitStr.Length != 2)
			{
				throw new Exception($"Invalid path specified, expecting bundle:/Path/within/bundle");
			}

			return LoadAssetInternal<T>(splitStr[0], splitStr[1], Assembly.GetCallingAssembly());
		}

		private static T LoadAssetInternal<T>(string bundleName, string path, Assembly assembly) where T : UnityEngine.Object
		{
			if (bundleName == null) throw new FileNotFoundException($"No bundle name provided");
			path = path.ToLower();

			bundleName = GetProperBundleName(assembly, bundleName);
			var pathToBundle = AssetBundlePath(assembly, bundleName, path);
			if (LoadedAssets.TryGetValue(pathToBundle, out var cachedAsset))
			{
				return cachedAsset as T;
			}

			var bundleFileName = GetBundleFilename(bundleName);

			if (Bundles.TryGetValue(bundleName, out var preloadedBundle))
			{
				// We already loaded the bundle, lets see if we can fetch the object
				var obj = preloadedBundle.LoadAsset<T>(path);
				if (obj == null) throw new FileNotFoundException($"Failed to find file {path} in bundle {bundleFileName}");

				// Found it, lets cache it!
				LoadedAssets[pathToBundle] = obj;
				return obj as T;
			}
			// We haven't loaded our bundle yet! Lets load it and try again
			var bundlePath = Path.Combine(Util.Module.GetPluginDir(assembly), "CosmicBundles", bundleFileName);
			var bundle = AssetBundle.LoadFromFile(bundlePath);
			if (bundle == null) throw new FileNotFoundException($"Failed to find bundle {bundleFileName}");

			Bundles[bundleName] = bundle;

			var objNew = bundle.LoadAsset<T>(path);
			if (objNew == null) throw new FileNotFoundException($"Failed to find file {path} in bundle {bundleFileName}");
			LoadedAssets[pathToBundle] = objNew;
			return objNew as T;
		}

		private static bool UnloadAssetInternal<T>(string bundleName, string path, Assembly assembly) where T : UnityEngine.Object
		{
			if (bundleName == null) throw new FileNotFoundException($"No bundle name provided");
			path = path.ToLower();

			bundleName = GetProperBundleName(assembly, bundleName);
			var pathToBundle = AssetBundlePath(assembly, bundleName, path);

			// Nothing removed
			if (!LoadedAssets.ContainsKey(pathToBundle))
				return false;

			LoadedAssets.Remove(pathToBundle);
			return true;
		}

		/// <summary>
		/// Loads an asset with a specified bundle name or fetches it from a cache.
		/// </summary>
		/// <typeparam name="T">Unity asset type</typeparam>
		/// <param name="bundleName">Name of the bundle</param>
		/// <param name="path">Path within the asset bundle</param>
		/// <returns>The loaded asset</returns>
		/// <exception cref="FileNotFoundException">Failed to find asset or load it!</exception>
		public static T LoadAsset<T>(string bundleName, string path) where T : UnityEngine.Object
		{
			return LoadAssetInternal<T>(bundleName, path, Assembly.GetCallingAssembly());
		}

		/// <summary>
		/// Removes the asset from the cache
		/// </summary>
		/// <typeparam name="T">Unity asset type</typeparam>
		/// <param name="bundleName">The name of the bundle</param>
		/// <param name="path">Path within the asset bundle</param>
		/// <returns>true if the asset was removed, false if it wasn't loaded or doesn't exist</returns>
		/// <exception cref="FileNotFoundException"></exception>
		public static bool UnloadAsset<T>(string bundleName, string path) where T : UnityEngine.Object
		{
			return UnloadAssetInternal<T>(bundleName, path, Assembly.GetCallingAssembly());
		}

		/// <summary>
		/// Removes the asset from the cache
		/// This method uses the bundlename:/Path/Within/Bundle format!
		/// </summary>
		/// <typeparam name="T">Unity asset type</typeparam>
		/// <param name="bundleWithPath">The asset bundles name as the "drive" and the path within the asset</param>
		/// <returns>true if the asset was removed, false if it wasn't loaded or doesn't exist</returns>
		/// <exception cref="Exception"></exception>
		public static bool UnloadAsset<T>(string bundleWithPath) where T : UnityEngine.Object
		{
			string[] splitStr = bundleWithPath.Split(":/");
			if (splitStr.Length != 2)
			{
				throw new Exception($"Invalid path specified, expecting bundle:/Path/within/bundle");
			}
			return UnloadAssetInternal<T>(splitStr[0], splitStr[1], Assembly.GetCallingAssembly());
		}
	}
}
