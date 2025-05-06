#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Traits.Render;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("Render an animated voxel based upon the voxel being inair.")]
	public sealed class WithVoxelHelicopterBodyInfo : ConditionalTraitInfo, IRenderActorPreviewVoxelsInfo, Requires<RenderVoxelsInfo>
	{
		public readonly string Sequence = "idle";

		[Desc("The rate of the voxel animation.")]
		public readonly int TickRate = 5;

		[Desc("Defines if the Voxel should have a shadow.")]
		public readonly bool ShowShadow = true;

		public override object Create(ActorInitializer init) { return new WithVoxelHelicopterBody(init.Self, this); }

		public IEnumerable<ModelAnimation> RenderPreviewVoxels(IModelCache cache,
			ActorPreviewInitializer init, RenderVoxelsInfo rv, string image, Func<WRot> orientation, int facings, PaletteReference p)
		{
			var model = cache.GetModelSequence(image, Sequence);
			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			var frame = init.GetValue<BodyAnimationFrameInit, uint>(this, 0);

			yield return new ModelAnimation(model, () => WVec.Zero,
				() => body.QuantizeOrientation(orientation(), facings),
				() => false, () => frame, ShowShadow);
		}
	}

	public sealed class WithVoxelHelicopterBody : ConditionalTrait<WithVoxelHelicopterBodyInfo>, IAutoMouseBounds, ITick, IActorPreviewInitModifier
	{
		readonly WithVoxelHelicopterBodyInfo info;
		readonly RenderVoxels rv;
		readonly ModelAnimation modelAnimation;
		readonly uint frames;
		uint tick, frame;

		public WithVoxelHelicopterBody(Actor self, WithVoxelHelicopterBodyInfo info)
			: base(info)
		{
			this.info = info;

			var body = self.Trait<BodyOrientation>();
			rv = self.Trait<RenderVoxels>();

			var voxel = rv.Renderer.ModelCache.GetModelSequence(rv.Image, info.Sequence);
			frames = voxel.Frames;
			modelAnimation = new ModelAnimation(voxel, () => WVec.Zero,
				() => body.QuantizeOrientation(self.Orientation),
				() => IsTraitDisabled, () => frame, info.ShowShadow);

			rv.Add(modelAnimation);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (self.World.Map.DistanceAboveTerrain(self.CenterPosition) > WDist.Zero)
				tick++;

			if (tick < info.TickRate)
				return;

			tick = 0;
			if (++frame == frames)
				frame = 0;
		}

		void IActorPreviewInitModifier.ModifyActorPreviewInit(Actor self, TypeDictionary inits)
		{
			inits.Add(new BodyAnimationFrameInit(frame));
		}

		Rectangle IAutoMouseBounds.AutoMouseoverBounds(Actor self, WorldRenderer wr)
		{
			return modelAnimation.ScreenBounds(self.CenterPosition, wr, rv.Info.Scale);
		}
	}
}
