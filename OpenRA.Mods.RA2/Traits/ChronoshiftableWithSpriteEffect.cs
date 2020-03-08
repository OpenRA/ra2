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
	[Desc("Can be teleported via Chronoshift power.")]
	public class ChronoshiftableWithSpriteEffectInfo : ChronoshiftableInfo
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

		public override object Create(ActorInitializer init) { return new ChronoshiftableWithSpriteEffect(init, this); }
	}

	public class ChronoshiftableWithSpriteEffect : Chronoshiftable
	{
		readonly ChronoshiftableWithSpriteEffectInfo info;

		public ChronoshiftableWithSpriteEffect(ActorInitializer init, ChronoshiftableWithSpriteEffectInfo info)
			: base(init, info)
		{
			this.info = info;
		}

		public override bool Teleport(Actor self, CPos targetLocation, int duration, bool killCargo, Actor chronosphere)
		{
			var image = info.Image ?? self.Info.Name;

			var cachedSourcePosition = self.CenterPosition;
			var cachedTargetPosition = self.World.Map.CenterOfCell(targetLocation);

			self.World.AddFrameEndTask(w =>
			{
				if (info.WarpInSequence != null)
					w.Add(new SpriteEffect(cachedSourcePosition, w, image, info.WarpInSequence, info.Palette));

				if (info.WarpOutSequence != null)
					w.Add(new SpriteEffect(cachedTargetPosition, w, image, info.WarpOutSequence, info.Palette));
			});

			return base.Teleport(self, targetLocation, duration, killCargo, chronosphere);
		}
	}
}
