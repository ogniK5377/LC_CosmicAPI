using LC_CosmicAPI.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace LC_CosmicAPI.Util
{
	public abstract class ICustomScrap
	{
		protected virtual int ExperimentationRarity { get; } = 0;
		protected virtual int AssuranceRarity { get; } = 0;
		protected virtual int VowRarity { get; } = 0;
		protected virtual int OffenseRarity { get; } = 0;
		protected virtual int MarchRarity { get; } = 0;
		protected virtual int RendRarity { get; } = 0;
		protected virtual int DineRarity { get; } = 0;
		protected virtual int TitanRarity { get; } = 0;

		public int GetRarityForMoon(Level.MoonID moonId)
		{
			switch (moonId)
			{
				case Level.MoonID.Experimentation: return ExperimentationRarity;
				case Level.MoonID.Assurance: return AssuranceRarity;
				case Level.MoonID.Vow: return VowRarity;
				case Level.MoonID.Offense: return OffenseRarity;
				case Level.MoonID.March: return MarchRarity;
				case Level.MoonID.Rend: return RendRarity;
				case Level.MoonID.Dine: return DineRarity;
				case Level.MoonID.Titan: return TitanRarity;
				default:
					return 0;
			}
		}

		public bool ShouldSpawnOnMoon(Level.MoonID moonId)
		{
			return GetRarityForMoon(moonId) > 0;
		}

		internal Item ItemDetails { get; set; }

		public abstract Item LoadItem();
	}
}
