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

using OpenRA.Mods.Cnc.Traits;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("Renders the chronosphere bubble effects.")]
	public class WithChronosphereOverlayInfo : ITraitInfo
	{
		[Desc("Image used for the teleport effects. Defaults to the actor's type.")]
		public readonly string Image = null;

		[Desc("Sequence used for the effect played where the unit jumped from.")]
		[SequenceReference("Image")]
		public readonly string WarpInSequence = null;

		[Desc("Sequence used for the effect played where the unit jumped to.")]
		[SequenceReference("Image")]
		public readonly string WarpOutSequence = null;

		[Desc("Palette to render the warp in/out sprites in.")]
		[PaletteReference]
		public readonly string Palette = "effect";

		public object Create(ActorInitializer init) { return new WithChronosphereOverlay(init, this); }
	}

	public class WithChronosphereOverlay : INotifyChronosphere
	{
		readonly WithChronosphereOverlayInfo info;
		readonly Actor self;

		public WithChronosphereOverlay(ActorInitializer init, WithChronosphereOverlayInfo info)
		{
			self = init.Self;
			this.info = info;
		}

		void INotifyChronosphere.Teleporting(WPos from, WPos to)
		{
			var image = info.Image ?? self.Info.Name;

			self.World.AddFrameEndTask(w =>
			{
				if (info.WarpInSequence != null)
					w.Add(new SpriteEffect(from, w, image, info.WarpInSequence, info.Palette));

				if (info.WarpOutSequence != null)
					w.Add(new SpriteEffect(to, w, image, info.WarpOutSequence, info.Palette));
			});
		}
	}
}
