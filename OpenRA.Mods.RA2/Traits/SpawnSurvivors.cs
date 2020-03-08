#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("Spawns survivors when an actor is destroyed or sold.")]
	public class SpawnSurvivorsInfo : ConditionalTraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("The actors spawned.")]
		public readonly string[] Actors = { };

		[Desc("DeathType(s) that trigger spawning. Leave empty to always spawn.")]
		public readonly BitSet<DamageType> DeathTypes = default(BitSet<DamageType>);

		public override object Create(ActorInitializer actor) { return new SpawnSurvivors(this); }
	}

	public class SpawnSurvivors : ConditionalTrait<SpawnSurvivorsInfo>, INotifyKilled
	{
		public SpawnSurvivors(SpawnSurvivorsInfo info)
			: base(info) { }

		void INotifyKilled.Killed(Actor self, AttackInfo attack)
		{
			if (IsTraitDisabled)
				return;

			if (!Info.DeathTypes.IsEmpty && !attack.Damage.DamageTypes.Overlaps(Info.DeathTypes))
				return;

			Spawn(self);
		}

		void Spawn(Actor self)
		{
			var buildingInfo = self.Info.TraitInfoOrDefault<BuildingInfo>();
			var eligibleLocations = buildingInfo != null
				? buildingInfo.Tiles(self.Location).ToList()
				: new List<CPos>() { self.World.Map.CellContaining(self.CenterPosition) };

			self.World.AddFrameEndTask(w =>
			{
				foreach (var actorType in Info.Actors)
				{
					var td = new TypeDictionary();

					td.Add(new OwnerInit(self.Owner));
					td.Add(new LocationInit(eligibleLocations.Random(w.SharedRandom)));

					var unit = w.CreateActor(true, actorType.ToLowerInvariant(), td);
					var mobile = unit.TraitOrDefault<Mobile>();
					if (mobile != null)
						mobile.Nudge(unit, self, true);
				}
			});
		}
	}
}
