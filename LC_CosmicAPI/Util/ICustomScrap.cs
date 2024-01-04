using LC_CosmicAPI.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace LC_CosmicAPI.Util
{
	public abstract class ICustomScrap : IMoonRarity
	{
		internal Item ItemDetails { get; set; }

		public abstract Item LoadItem();
	}
}
