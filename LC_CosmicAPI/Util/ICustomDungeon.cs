using DunGen.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace LC_CosmicAPI.Util
{
	public abstract class ICustomDungeon : IMoonRarity
	{
		internal int FlowIndex{ get; set; }
		public abstract DungeonFlow LoadDungeonFlow();
	}
}
