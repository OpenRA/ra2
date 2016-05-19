#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	public class UpgradeInWaterInfo : ITraitInfo, Requires<UpgradeManagerInfo>
	{
		[UpgradeGrantedReference]
		public readonly string[] InWaterUpgrades = { "water-borne" };

		public object Create(ActorInitializer init) { return new UpgradeInWater(init, this); }
	}

	public class UpgradeInWater : ITick
	{
		readonly Actor self;
		readonly UpgradeInWaterInfo info;
		readonly UpgradeManager manager;

		public UpgradeInWater(ActorInitializer init, UpgradeInWaterInfo info)
		{
			self = init.Self;
			this.info = info;
			manager = self.Trait<UpgradeManager>();
		}

		bool wasWater;
		public void Tick(Actor self)
		{
			var isWater = self.World.Map.GetTerrainInfo(self.Location).IsWater;
			if (isWater != wasWater)
			{
				if (isWater)
				{
					foreach (var up in info.InWaterUpgrades)
						manager.GrantUpgrade(self, up, this);
				}
				else
				{
					foreach (var up in info.InWaterUpgrades)
						manager.RevokeUpgrade(self, up, this);
				}

				wasWater = isWater;
			}
		}
	}
}