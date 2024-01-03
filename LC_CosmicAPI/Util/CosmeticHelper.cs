using MoreCompany.Cosmetics;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LC_CosmicAPI.Util
{
	public class CosmeticHelper
	{
		private GameObject _baseObject = null;
		internal CosmeticHelper(GameObject baseObject)
		{
			if (!Plugin.HasMoreCompany || baseObject == null) return;
			var cosmeticApp = baseObject.GetComponent<CosmeticApplication>();
			if (cosmeticApp != null)
			{
				HasCosmeticApplication = true;
				_baseObject = baseObject;
				return;
			}
			cosmeticApp = baseObject.GetComponentInChildren<CosmeticApplication>();
			if (cosmeticApp != null)
			{
				HasCosmeticApplication = true;
				_baseObject = cosmeticApp.gameObject;
				return;
			}
		}

		private enum TransformId
		{
			Hip = 0,
			Chest = 1,
			Head = 2,
			LowerArmRight = 3,
			ShinLeft = 4,
			ShinRight = 5,
		}

		private void SetTransformForIndex(TransformId transformId, Transform transform)
		{
			if (!Plugin.HasMoreCompany || !HasCosmeticApplication || _baseObject == null) return;
			var cosmeticApplication = _baseObject.GetComponent<CosmeticApplication>();
			if (cosmeticApplication == null) return;

			switch (transformId)
			{
				case TransformId.Hip:
					cosmeticApplication.hip = transform;
					break;
				case TransformId.Chest:
					cosmeticApplication.chest = transform;
					break;
				case TransformId.Head:
					cosmeticApplication.head = transform;
					break;
				case TransformId.LowerArmRight:
					cosmeticApplication.lowerArmRight = transform;
					break;
				case TransformId.ShinLeft:
					cosmeticApplication.shinLeft = transform;
					break;
				case TransformId.ShinRight:
					cosmeticApplication.shinRight = transform;
					break;
				default:
					break;
			}

		}

		private Transform GetTransformForIndex(TransformId transformId)
		{
			if (!Plugin.HasMoreCompany || !HasCosmeticApplication || _baseObject == null) return default;
			var cosmeticApplication = _baseObject.GetComponent<CosmeticApplication>();
			if (cosmeticApplication == null) return default;

			return transformId switch
			{
				TransformId.Hip => cosmeticApplication.hip,
				TransformId.Chest => cosmeticApplication.chest,
				TransformId.Head => cosmeticApplication.head,
				TransformId.LowerArmRight => cosmeticApplication.lowerArmRight,
				TransformId.ShinLeft => cosmeticApplication.shinLeft,
				TransformId.ShinRight => cosmeticApplication.shinRight,
				_ => default,
			};
		}

		public Transform Hip
		{
			get => GetTransformForIndex(TransformId.Hip);
			set => SetTransformForIndex(TransformId.Hip, value);
		}
		public Transform Chest
		{
			get => GetTransformForIndex(TransformId.Chest);
			set => SetTransformForIndex(TransformId.Chest, value);
		}
		public Transform Head
		{
			get => GetTransformForIndex(TransformId.Head);
			set => SetTransformForIndex(TransformId.Head, value);
		}
		public Transform LowerArmRight
		{
			get => GetTransformForIndex(TransformId.LowerArmRight);
			set => SetTransformForIndex(TransformId.LowerArmRight, value);
		}
		public Transform ShinLeft
		{
			get => GetTransformForIndex(TransformId.ShinLeft);
			set => SetTransformForIndex(TransformId.ShinLeft, value);
		}
		public Transform ShinRight
		{
			get => GetTransformForIndex(TransformId.ShinRight);
			set => SetTransformForIndex(TransformId.ShinRight, value);
		}

		public bool HasCosmeticApplication { get; internal set; } = false;

		public void RefreshCosmeticPositions()
		{
			if (!Plugin.HasMoreCompany) return;
			var cosmeticApplication = _baseObject.GetComponent<CosmeticApplication>();

			cosmeticApplication.hip = Hip;
			cosmeticApplication.chest = Chest;
			cosmeticApplication.head = Head;
			cosmeticApplication.lowerArmRight = LowerArmRight;
			cosmeticApplication.shinLeft = ShinLeft;
			cosmeticApplication.shinRight = ShinRight;

			cosmeticApplication.RefreshAllCosmeticPositions();
		}
	}
}
