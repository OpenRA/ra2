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
	[Desc("This actor displays an overlay when spawning a missile.")]
	public class WithMissileLaunchOverlayInfo : ITraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[SequenceReference]
		[Desc("Sequence to use for charge animation.")]
		public readonly string Sequence = "takeoff";

		[Desc("Which sprite body to play the animation on.")]
		public readonly string Body = "body";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[PaletteReference]
		[Desc("Custom palette name")]
		public readonly string Palette = null;

		public object Create(ActorInitializer init) { return new WithMissileLaunchOverlay(init, this); }
	}

	public class WithMissileLaunchOverlay : INotifyMissileSpawn
	{
		readonly WithMissileLaunchOverlayInfo info;
		readonly Animation overlay;
		readonly AnimationWithOffset animation;
		readonly RenderSprites renderSprites;

		public WithMissileLaunchOverlay(ActorInitializer init, WithMissileLaunchOverlayInfo info)
		{
			this.info = info;
			renderSprites = init.Self.Trait<RenderSprites>();
			overlay = new Animation(init.World, renderSprites.GetImage(init.Self));
			var body = init.Self.Trait<BodyOrientation>();
			animation = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(init.Self, init.Self.Orientation))),
				() => false);
		}

		void INotifyMissileSpawn.Launching(Actor self, Target target)
		{
			renderSprites.Add(animation, info.Palette);

			// Remove the animation once it is complete
			overlay.PlayBackwardsThen(info.Sequence, () => self.World.AddFrameEndTask(w => renderSprites.Remove(animation)));
		}
	}
}
