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

using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits.Render
{
	[Desc("Displays an overlay when `NukePower` is triggered.")]
	public class WithNuclearMissileSmokeOverlayInfo : ConditionalTraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[SequenceReference]
		[Desc("Sequence name to use")]
		public readonly string Sequence = "smoke";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[PaletteReference("IsPlayerPalette")]
		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName.")]
		public readonly bool IsPlayerPalette = false;

		[Desc("Measured in ticks.")]
		public readonly int Delay = 0;

		public override object Create(ActorInitializer init) { return new WithNuclearMissileSmokeOverlay(init.Self, this); }
	}

	public class WithNuclearMissileSmokeOverlay : ConditionalTrait<WithNuclearMissileSmokeOverlayInfo>, INotifyNuke, ITick
	{
		readonly Animation overlay;
		bool visible, isLaunched;
		int launchDelay;

		public WithNuclearMissileSmokeOverlay(Actor self, WithNuclearMissileSmokeOverlayInfo info)
			: base(info)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			overlay = new Animation(self.World, rs.GetImage(self));
			overlay.PlayThen(info.Sequence, () => visible = false);

			var anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
				() => IsTraitDisabled || !visible,
				p => RenderUtils.ZOffsetFromCenter(self, p, 1));

			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

		void INotifyNuke.Launching(Actor self)
		{
			isLaunched = true;
			launchDelay = Info.Delay;
		}

		void ITick.Tick(Actor self)
		{
			if (!isLaunched)
				return;

			if (launchDelay-- > 0)
				return;

			if (!visible)
			{
				visible = true;
				overlay.PlayThen(overlay.CurrentSequence.Name, () => visible = false);
				isLaunched = false;
			}
		}
	}
}
