using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LC_CosmicAPI.Game
{
	public static class TransformExtensions
	{
		public static List<Transform> GetEveryChild(this Transform parent, string[] ignoreList = null, List<Transform> list = null)
		{
			if (ignoreList != null && ignoreList.Contains(parent.name)) return list;

			if (list == null) list = new();
			for (int i = 0; i < parent.childCount; i++)
			{
				var child = parent.GetChild(i);
				list.Add(child);
				GetEveryChild(child, ignoreList, list);
			}
			return list;
		}

		public static Transform GetFirstWithName(this Transform transform, string name)
		{
			if (transform.name == name) return transform;
			for (int i = 0; i < transform.childCount; i++)
			{
				var child = transform.GetChild(i);
				if (GetFirstWithName(child, name) != null) return child;
			}
			return null;
		}
	}
}
