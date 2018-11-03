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

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Activities
{
	class EnterCarrierMaster : Enter
	{
		readonly Actor master; // remember the spawner.
		readonly CarrierMaster spawnerMaster;

		public EnterCarrierMaster(Actor self, Actor master, CarrierMaster spawnerMaster, EnterBehaviour enterBehaviour)
			: base(self, master, enterBehaviour)
		{
			this.master = master;
			this.spawnerMaster = spawnerMaster;
		}

		protected override bool CanReserve(Actor self)
		{
			return true; // Slaves are always welcome.
		}

		protected override ReserveStatus Reserve(Actor self)
		{
			// TryReserveElseTryAlternateReserve calls Reserve and
			// the default inplementation of Reserve() returns TooFar when
			// the aircraft carrier is hovering over a building.
			// Since spawners don't need reservation (and have no reservation trait),
			// just return Ready so that spawner can enter no matter where the spawner is.
			return ReserveStatus.Ready;
		}

		protected override void OnInside(Actor self)
		{
			// Master got killed :(
			if (master.IsDead)
				return;

			Done(self); // Stop slaves from exiting.

			// Load this thingy.
			// Issue attack move to the rally point.
			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead || master.IsDead)
					return;

                spawnerMaster.PickupSlave(master, self);
				w.Remove(self);

				// Insta repair.
				if (spawnerMaster.Info.InstaRepair)
				{
					var health = self.Trait<Health>();
					self.InflictDamage(self, new Damage(-health.MaxHP));
				}

                // Insta re-arm. (Delayed launching is handled at spawner.)
                var ammoPools = self.TraitsImplementing<AmmoPool>().Where(p => !p.AutoReloads).ToArray();
				if (ammoPools != null)
					foreach (var pool in ammoPools)
						while (pool.GiveAmmo(self, 1)); // fill 'er up.
			});
		}
	}
}
