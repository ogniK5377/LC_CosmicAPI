using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LC_CosmicAPI.Game
{
	public static class GameObjectExtensions
	{
		private static List<GameObject> GetEveryChildFromTransform(Transform parent, string[] ignoreList = null, List<GameObject> list = null)
		{
			if (ignoreList != null && ignoreList.Contains(parent.name)) return list;
			if (list == null) list = new();

			for (int i = 0; i < parent.childCount; i++)
			{
				var child = parent.GetChild(i);
				list.Add(child.gameObject);
				GetEveryChildFromTransform(child, ignoreList, list);
			}
			return list;
		}

		public static List<GameObject> GetEveryChild(this GameObject parent, string[] ignoreList = null, List<GameObject> list = null)
		{
			return GetEveryChildFromTransform(parent.transform, ignoreList, list);
		}
	}
}
