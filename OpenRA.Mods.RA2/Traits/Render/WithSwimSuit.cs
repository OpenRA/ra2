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
	[Desc("Change the sprite when swimming in water.")]
	public class WithSwimSuitInfo : ITraitInfo
	{
		[SequenceReference(null, true)] public readonly string InWaterPrefix = "swim-";

		public object Create(ActorInitializer init) { return new WithSwimSuit(init.Self, this); }
	}

	public class WithSwimSuit : IRenderInfantrySequenceModifier
	{
		readonly WithSwimSuitInfo info;
		readonly Actor self;

		public WithSwimSuit(Actor self, WithSwimSuitInfo info)
		{
			this.self = self;
			this.info = info;
		}

		bool IRenderInfantrySequenceModifier.IsModifyingSequence { get { return self.World.Map.GetTerrainInfo(self.Location).IsWater; } }
		string IRenderInfantrySequenceModifier.SequencePrefix { get { return info.InWaterPrefix; } }
	}
}