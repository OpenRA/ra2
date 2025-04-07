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

using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.RA2.Graphics
{
	public readonly struct ArcRenderable : IRenderable, IFinalizedRenderable
	{
		readonly Color color;
		readonly WPos end;
		readonly WAngle angle;
		readonly WDist width;
		readonly int segments;

		public ArcRenderable(WPos start, WPos end, int zOffset, WAngle angle, Color color, WDist width, int segments)
		{
			Pos = start;
			ZOffset = zOffset;
			this.end = end;
			this.angle = angle;
			this.color = color;
			this.width = width;
			this.segments = segments;
		}

		public WPos Pos { get; }
		public int ZOffset { get; }
		public bool IsDecoration => true;

		public IRenderable WithZOffset(int newOffset) { return new ArcRenderable(Pos, end, ZOffset, angle, color, width, segments); }
		public IRenderable OffsetBy(in WVec vec) { return new ArcRenderable(Pos + vec, end + vec, ZOffset, angle, color, width, segments); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }

		void IFinalizedRenderable.Render(WorldRenderer wr)
		{
			var screenWidth = wr.ScreenVector(new WVec(width, WDist.Zero, WDist.Zero))[0];

			var points = new float3[segments + 1];
			for (var i = 0; i <= segments; i++)
				points[i] = wr.Screen3DPosition(WPos.LerpQuadratic(Pos, end, angle, i, segments));

			Game.Renderer.WorldRgbaColorRenderer.DrawLine(points, screenWidth, color, false);
		}

		void IFinalizedRenderable.RenderDebugGeometry(WorldRenderer wr) { }
		Rectangle IFinalizedRenderable.ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
