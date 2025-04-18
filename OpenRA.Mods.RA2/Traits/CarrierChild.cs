#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.RA2.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("Can be bound to a spawner.")]
	public class CarrierChildInfo : BaseSpawnerChildInfo
	{
		[Desc("Move this close to the spawner, before entering it.")]
		public readonly WDist LandingDistance = new(5 * 1024);

		public override object Create(ActorInitializer init) { return new CarrierChild(this); }
	}

	public class CarrierChild : BaseSpawnerChild, INotifyIdle
	{
		public readonly CarrierChildInfo Info;

		CarrierParent spawnerParent;

		public CarrierChild(CarrierChildInfo info)
			: base(info)
		{
			Info = info;
		}

		public void EnterSpawner(Actor self)
		{
			if (Parent == null || Parent.IsDead)
				return;

			// Proceed with enter, if already at it.
			if (self.CurrentActivity is EnterCarrierParent)
				return;

			// Cancel whatever else self was doing and return.
			self.QueueActivity(false, new EnterCarrierParent(self, Parent, spawnerParent));
		}

		public override void LinkParent(Actor self, Actor parent, BaseSpawnerParent spawnerParent)
		{
			base.LinkParent(self, parent, spawnerParent);
			this.spawnerParent = spawnerParent as CarrierParent;
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			EnterSpawner(self);
		}

		public override void Stop(Actor self)
		{
			base.Stop(self);
			EnterSpawner(self);
		}
	}
}
