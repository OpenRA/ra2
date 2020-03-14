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

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;
using Color = OpenRA.Primitives.Color;

namespace OpenRA.Mods.RA2.Traits
{
	using Util = OpenRA.Graphics.Util;

	[Desc("Global palette effect with a fixed color.")]
	public class WeatherPaletteEffectInfo : ITraitInfo
	{
		public readonly string[] ExcludePalette = { "cursor", "chrome", "colorpicker", "fog", "shroud", "effect" };

		[Desc("Used to pre-multiply colors.")]
		public readonly float Ratio = 0.6f;

		[Desc("Measured in ticks.")]
		public readonly int Length = 40;

		public readonly Color Color = Color.LightGray;

		[Desc("Set this when using multiple independent flash effects.")]
		public readonly string Type = null;

		public object Create(ActorInitializer init) { return new WeatherPaletteEffect(this); }
	}

	public class WeatherPaletteEffect : IPaletteModifier, ITick
	{
		public readonly WeatherPaletteEffectInfo Info;

		int remainingFrames;

		public WeatherPaletteEffect(WeatherPaletteEffectInfo info)
		{
			Info = info;
		}

		public void Enable(int ticks)
		{
			if (ticks == -1)
				remainingFrames = Info.Length;
			else
				remainingFrames = ticks;
		}

		void ITick.Tick(Actor self)
		{
			if (remainingFrames > 0)
				remainingFrames--;
		}

		void IPaletteModifier.AdjustPalette(IReadOnlyDictionary<string, MutablePalette> palettes)
		{
			if (remainingFrames == 0)
				return;

			foreach (var palette in palettes)
			{
				if (Info.ExcludePalette.Contains(palette.Key))
					continue;

				for (var x = 0; x < Palette.Size; x++)
				{
					var orig = palette.Value.GetColor(x);
					var c = Info.Color;
					var color = Color.FromArgb(orig.A, ((int)c.R).Clamp(0, 255), ((int)c.G).Clamp(0, 255), ((int)c.B).Clamp(0, 255));
					var final = Util.PremultipliedColorLerp(Info.Ratio, orig, Util.PremultiplyAlpha(Color.FromArgb(orig.A, color)));
					palette.Value.SetColor(x, final);
				}
			}
		}
	}
}
