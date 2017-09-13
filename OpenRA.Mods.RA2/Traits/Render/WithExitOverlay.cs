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

using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("Renders an animation when when the actor is leaving from a production building.")]
	public class WithExitOverlayInfo : ITraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "exit-overlay";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Custom palette name")]
		[PaletteReference("IsPlayerPalette")] public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public object Create(ActorInitializer init) { return new WithExitOverlay(init.Self, this); }
	}

	public class WithExitOverlay : INotifyDamageStateChanged, INotifyBuildComplete, INotifySold, INotifyProduction, ITick
	{
		readonly Animation overlay;
		bool buildComplete, enable;
		CPos exit;

		public WithExitOverlay(Actor self, WithExitOverlayInfo info)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			// Always render instantly for units
			buildComplete = !self.Info.HasTraitInfo<BuildingInfo>();

			overlay = new Animation(self.World, rs.GetImage(self));
			overlay.PlayRepeating(info.Sequence);

			var anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
				() => !buildComplete || !enable);

			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

		void INotifyBuildComplete.BuildingComplete(Actor self)
		{
			buildComplete = true;
		}

		void INotifySold.Sold(Actor self) { }
		void INotifySold.Selling(Actor self)
		{
			buildComplete = false;
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			overlay.ReplaceAnim(RenderSprites.NormalizeSequence(overlay, e.DamageState, overlay.CurrentSequence.Name));
		}

		void INotifyProduction.UnitProduced(Actor self, Actor other, CPos exit)
		{
			this.exit = exit;
			enable = true;
		}

		void ITick.Tick(Actor self)
		{
			if (enable)
				enable = self.World.ActorMap.GetActorsAt(exit).Any(a => a != self);
		}
	}
}
