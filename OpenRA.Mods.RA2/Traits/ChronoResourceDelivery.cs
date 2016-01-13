#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("When returning to a refinery to deliver resources, this actor will teleport if possible.")]
	public class ChronoResourceDeliveryInfo : ITraitInfo, Requires<HarvesterInfo>
	{
		[Desc("The number of ticks between each check to see if we can teleport to the refinery.")]
		public readonly int CheckTeleportDelay = 10;

		[Desc("The visual effect to play upon teleporting.")]
		public readonly string TeleportEffect = null;

		[Desc("The palette for the teleport visual effect.")]
		public readonly string TeleportPalette = null;

		[Desc("The sound for the teleport.")]
		public readonly string TeleportSound = null;

		public virtual object Create(ActorInitializer init) { return new ChronoResourceDelivery(init.Self, this); }
	}

	public class ChronoResourceDelivery : INotifyHarvesterAction, ITick
	{
		public readonly ChronoResourceDeliveryInfo Info;

		CPos? dest = null;
		Activity nextActivity = null;
		int ticksTillCheck = 0;

		public ChronoResourceDelivery(Actor self, ChronoResourceDeliveryInfo info)
		{
			Info = info;
		}

		public void Tick(Actor self)
		{
			if (dest == null)
				return;

			if (ticksTillCheck <= 0)
			{
				ticksTillCheck = Info.CheckTeleportDelay;

				TeleportIfPossible(self);
			}
			else
				ticksTillCheck--;
		}

		public void MovingToResources(Actor self, CPos targetCell, Activity next)
		{
			Reset();
		}

		public void MovingToRefinery(Actor self, CPos targetCell, Activity next)
		{
			if (dest != null && dest.Value != targetCell)
				ticksTillCheck = 0;

			dest = targetCell;
			nextActivity = next;
		}

		public void MovementCancelled(Actor self)
		{
			Reset();
		}

		public void Harvested(Actor self, ResourceType resource) { }
		public void Docked() { }
		public void Undocked() { }

		void TeleportIfPossible(Actor self)
		{
			// We're already here; no need to interfere.
			if (self.Location == dest.Value)
			{
				Reset();
				return;
			}

			var pos = self.Trait<IPositionable>();
			var world = self.World;
			if (pos.CanEnterCell(dest.Value))
			{
				if (Info.TeleportSound != null)

					Game.Sound.Play(Info.TeleportSound, self.OccupiesSpace.CenterPosition);

				if (Info.TeleportEffect != null && Info.TeleportPalette != null)

					world.AddFrameEndTask(w => w.Add(new Explosion(w, self.OccupiesSpace.CenterPosition, Info.TeleportEffect, Info.TeleportPalette)));

				self.CancelActivity();
				self.QueueActivity(new SimpleTeleport(dest.Value));
				self.QueueActivity(nextActivity);
				Reset();
			}
		}

		void Reset()
		{
			ticksTillCheck = 0;
			dest = null;
			nextActivity = null;
		}
	}
}
