#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("When returning to a refinery to deliver resources, this actor will teleport if possible.")]
	public class ChronoResourceDeliveryInfo : ITraitInfo, Requires<HarvesterInfo>
	{
		[Desc("The number of ticks between each check to see if we can teleport to the refinery.")]
		public readonly int CheckTeleportDelay = 10;

		[Desc("Image used for the teleport effects. Defaults to the actor's type.")]
		public readonly string Image = null;

		[Desc("Sequence used for the effect played where the harvester jumped from.")]
		[SequenceReference("Image")]
		public readonly string WarpInSequence = null;

		[Desc("Sequence used for the effect played where the harvester jumped to.")]
		[SequenceReference("Image")]
		public readonly string WarpOutSequence = null;

		[Desc("Palette to render the warp in/out sprites in.")]
		[PaletteReference]
		public readonly string Palette = "effect";

		[Desc("Sound played where the harvester jumped from.")]
		public readonly string WarpInSound = null;

		[Desc("Sound where the harvester jumped to.")]
		public readonly string WarpOutSound = null;

		public virtual object Create(ActorInitializer init) { return new ChronoResourceDelivery(init.Self, this); }
	}

	public class ChronoResourceDelivery : INotifyHarvesterAction, ITick
	{
		readonly ChronoResourceDeliveryInfo info;

		CPos? destination;
		Actor refinery;
		int ticksTillCheck;

		// TODO: Rewrite this entire thing, possible to be a subclass of harvester
		// and make it work properly with activities
		public ChronoResourceDelivery(Actor self, ChronoResourceDeliveryInfo info)
		{
			this.info = info;
		}

		void ITick.Tick(Actor self)
		{
			if (!destination.HasValue)
				return;

			if (ticksTillCheck <= 0)
			{
				ticksTillCheck = info.CheckTeleportDelay;

				TeleportIfPossible(self);
			}
			else
				ticksTillCheck--;
		}

		void INotifyHarvesterAction.MovingToResources(Actor self, CPos targetCell)
		{
			Reset();
		}

		void INotifyHarvesterAction.MovingToRefinery(Actor self, Actor refineryActor)
		{
			var iao = refineryActor.Trait<IAcceptResources>();
			var targetCell = refineryActor.Location + iao.DeliveryOffset;
			if (destination != null && destination.Value != targetCell)
				ticksTillCheck = 0;

			refinery = refineryActor;
			destination = targetCell;
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
			if (self.Location == destination.Value)
			{
				Reset();
				return;
			}

			// HACK: Cancelling the current activity will call Reset, so cache the destination here
			var dest = destination.Value;
			var pos = self.Trait<IPositionable>();
			if (pos.CanEnterCell(dest))
			{
				self.CancelActivity();
				self.QueueActivity(new ChronoResourceTeleport(dest, info));

				// HACK: Manually queue a delivery and new find since we just cancelled all activities
				self.QueueActivity(new DeliverResources(self, refinery));
				self.QueueActivity(new FindAndDeliverResources(self, refinery));
				Reset();
			}
		}

		void Reset()
		{
			ticksTillCheck = 0;
			destination = null;
			refinery = null;
		}
	}
}
