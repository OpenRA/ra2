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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.RA2.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("Draws an arc between a mindcontroller actor and all its victims",
		"or an actively mindcontrolled actor and it's controller.")]
	public class WithMindControlArcInfo : ITraitInfo
	{
		[Desc("Color of the arc.")]
		public readonly Color Color = Color.Red;

		public readonly bool UsePlayerColor = false;

		public readonly int Transparency = 255;

		[Desc("Relative offset from the actor's center position where the arc should start.")]
		public readonly WVec Offset = new WVec(0, 0, 0);

		[Desc("The angle of the arc.")]
		public readonly WAngle Angle = new WAngle(64);

		[Desc("The width of the arc.")]
		public readonly WDist Width = new WDist(43);

		[Desc("Controls how fine-grained the resulting arc should be.")]
		public readonly int QuantizedSegments = 16;

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset = 0;

		public virtual object Create(ActorInitializer init) { return new WithMindControlArc(init.Self, this); }
	}

	public class WithMindControlArc : IRenderAboveShroudWhenSelected, INotifySelected, INotifyCreated
	{
		readonly WithMindControlArcInfo info;
		MindController mindController;
		MindControllable mindControllable;

		public WithMindControlArc(Actor self, WithMindControlArcInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			mindController = self.TraitOrDefault<MindController>();
			mindControllable = self.TraitOrDefault<MindControllable>();
		}

		void INotifySelected.Selected(Actor a) { }

		IEnumerable<IRenderable> IRenderAboveShroudWhenSelected.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			var color = Color.FromArgb(info.Transparency, info.UsePlayerColor ? self.Owner.Color : info.Color);

			if (mindController != null)
			{
				foreach (var s in mindController.Slaves)
					yield return new ArcRenderable(
						self.CenterPosition + info.Offset,
						s.CenterPosition + info.Offset,
						info.ZOffset, info.Angle, color, info.Width, info.QuantizedSegments);
				yield break;
			}

			if (mindControllable == null || mindControllable.Master == null || !mindControllable.Master.IsInWorld)
				yield break;

			yield return new ArcRenderable(
				mindControllable.Master.CenterPosition + info.Offset,
				self.CenterPosition + info.Offset,
				info.ZOffset, info.Angle, color, info.Width, info.QuantizedSegments);
		}

		bool IRenderAboveShroudWhenSelected.SpatiallyPartitionable
		{
			get { return false; }
		}
	}
}
