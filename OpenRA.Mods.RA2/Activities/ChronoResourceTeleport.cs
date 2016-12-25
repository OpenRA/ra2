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

using OpenRA.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.RA2.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Activities
{
	public class ChronoResourceTeleport : Activity
	{
		readonly CPos destination;
		readonly ChronoResourceDeliveryInfo info;

		public ChronoResourceTeleport(CPos destination, ChronoResourceDeliveryInfo info)
		{
			this.destination = destination;
			this.info = info;
		}

		public override Activity Tick(Actor self)
		{
			var image = info.Image ?? self.Info.Name;

			var sourcepos = self.CenterPosition;

			if (info.WarpInSequence != null)
				self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(sourcepos, w, image, info.WarpInSequence, info.Palette)));

			if (info.WarpInSound != null)
				Game.Sound.Play(SoundType.World, info.WarpInSound, self.CenterPosition);

			self.Trait<IPositionable>().SetPosition(self, destination);
			self.Generation++;

			var destinationpos = self.CenterPosition;

			if (info.WarpOutSequence != null)
				self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(destinationpos, w, image, info.WarpOutSequence, info.Palette)));

			if (info.WarpOutSound != null)
				Game.Sound.Play(SoundType.World, info.WarpOutSound, self.CenterPosition);

			return NextActivity;
		}
	}
}
