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
	[Desc("Displays an overlay when `SupportPower` is fully charged.")]
	public class WithSupportPowerChargedOverlayInfo : ConditionalTraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[SequenceReference]
		[Desc("Sequence name to use")]
		public readonly string ChargeSequence = "charged";

		[SequenceReference]
		[Desc("Sequence name to use")]
		public readonly string LoopSequence = "loop";

		[SequenceReference]
		[Desc("Sequence name to use")]
		public readonly string EndSequence = "end";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[PaletteReference("IsPlayerPalette")]
		[Desc("Custom palette name")]
		public readonly string Palette = "player";

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = true;

		public override object Create(ActorInitializer init) { return new WithSupportPowerChargedOverlay(init.Self, this); }
	}

	public class WithSupportPowerChargedOverlay : ConditionalTrait<WithSupportPowerChargedOverlayInfo>, INotifySupportPower
	{
		readonly Animation overlay;
		readonly WithSupportPowerChargedOverlayInfo info;
		bool visible;

		public WithSupportPowerChargedOverlay(Actor self, WithSupportPowerChargedOverlayInfo info)
			: base(info)
		{
			this.info = info;

			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			overlay = new Animation(self.World, rs.GetImage(self));

			var anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
				() => IsTraitDisabled || !visible,
				p => RenderUtils.ZOffsetFromCenter(self, p, 1));

			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

		void INotifySupportPower.Charged(Actor self)
		{
			visible = true;
			overlay.PlayThen(info.ChargeSequence,
				() => overlay.PlayRepeating(info.LoopSequence));
		}

		void INotifySupportPower.Activate(Actor self)
		{
			if (!string.IsNullOrEmpty(info.EndSequence))
				overlay.PlayThen(info.EndSequence,
					() => visible = false);
		}
	}
}
