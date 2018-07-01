#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("Can be slaved to a spawner.")]
	public class CarrierSlaveInfo : BaseSpawnerSlaveInfo
	{
		[Desc("Move this close to the spawner, before entering it.")]
		public readonly WDist LandingDistance = new WDist(5 * 1024);

		public override object Create(ActorInitializer init) { return new CarrierSlave(init, this); }
	}

	public class CarrierSlave : BaseSpawnerSlave, INotifyBecomingIdle
	{
		public CarrierSlaveInfo Info { get; private set; }

		readonly AmmoPool[] ammoPools;

		CarrierMaster spawnerMaster;

		public CarrierSlave(ActorInitializer init, CarrierSlaveInfo info) : base(init, info)
		{
			Info = info;
			ammoPools = init.Self.TraitsImplementing<AmmoPool>().ToArray();
		}

		public void EnterSpawner(Actor self)
		{
			// Hopefully, self will be disposed shortly afterwards by SpawnerSlaveDisposal policy.
			if (Master == null || Master.IsDead)
				return;

			// Proceed with enter, if already at it.
			if (self.CurrentActivity is EnterCarrierMaster)
				return;

			// Cancel whatever else self was doing and return.
			self.CancelActivity();

			var tgt = Target.FromActor(Master);

			if (self.TraitOrDefault<AttackPlane>() != null) // Let attack planes approach me first, before landing.
				self.QueueActivity(new Fly(self, tgt, WDist.Zero, Info.LandingDistance));

			self.QueueActivity(new EnterCarrierMaster(self, Master, spawnerMaster, EnterBehaviour.Exit));
		}

		public override void LinkMaster(Actor self, Actor master, BaseSpawnerMaster spawnerMaster)
		{
			base.LinkMaster(self, master, spawnerMaster);
			this.spawnerMaster = spawnerMaster as CarrierMaster;
		}

		bool NeedToReload(Actor self)
		{
			// The unit may not have ammo but will have unlimited ammunitions.
			if (ammoPools.Length == 0)
				return false;

			return ammoPools.All(x => !x.AutoReloads && !x.HasAmmo());
		}

		public virtual void OnBecomingIdle(Actor self)
		{
			EnterSpawner(self);
		}
	}
}
