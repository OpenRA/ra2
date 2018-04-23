#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.RA2.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	public class WithMindControlArcInfo : ITraitInfo
	{
		[Desc("Color of the arc")]
		public readonly Color Color = Color.Red;

		public readonly bool UsePlayerColor = false;

		public readonly int Transparency = 255;

		[Desc("Drawing from self.CenterPosition draws the curve from the foot. Add this much for better looks.")]
		public readonly WVec Offset = new WVec(0, 0, 0);

		[Desc("Angle of the ballistic arc, in WAngle")]
		public readonly WAngle Angle = new WAngle(64);

		[Desc("Draw with this many piecewise-linear lines")]
		public readonly int Segments = 16;

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

		public void Created(Actor self)
		{
			mindController = self.TraitOrDefault<MindController>();
			mindControllable = self.TraitOrDefault<MindControllable>();
		}

		void INotifySelected.Selected(Actor a) { }

		IEnumerable<IRenderable> IRenderAboveShroudWhenSelected.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			var color = Color.FromArgb(info.Transparency, info.UsePlayerColor ? self.Owner.Color.RGB : info.Color);

			if (mindController != null)
			{
				foreach (var s in mindController.Slaves)
					yield return new ArcRenderable(
						self.CenterPosition + info.Offset,
						s.CenterPosition + info.Offset,
						info.Angle, color, info.Segments);
				yield break;
			}

			if (mindControllable == null || mindControllable.Master == null || !mindControllable.Master.IsInWorld)
				yield break;

			yield return new ArcRenderable(
				mindControllable.Master.CenterPosition + info.Offset,
				self.CenterPosition + info.Offset,
				info.Angle, color, info.Segments);
		}

		bool IRenderAboveShroudWhenSelected.SpatiallyPartitionable
		{
			get { return false; }
		}
	}
}