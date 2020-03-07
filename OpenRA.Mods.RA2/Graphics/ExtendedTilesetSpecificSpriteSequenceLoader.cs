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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;

namespace OpenRA.Mods.RA2.Graphics
{
	public class ExtendedTilesetSpecificSpriteSequenceLoader : DefaultSpriteSequenceLoader
	{
		public readonly string DefaultSpriteExtension = ".shp";
		public readonly Dictionary<string, string> TilesetExtensions = new Dictionary<string, string>();
		public readonly Dictionary<string, string> TilesetCodes = new Dictionary<string, string>();
		public readonly Dictionary<string, string> TilesetSuffixes = new Dictionary<string, string>();

		public ExtendedTilesetSpecificSpriteSequenceLoader(ModData modData)
			: base(modData)
		{
			var metadata = modData.Manifest.Get<SpriteSequenceFormat>().Metadata;
			MiniYaml yaml;

			if (metadata.TryGetValue("DefaultSpriteExtension", out yaml))
				DefaultSpriteExtension = yaml.Value;

			if (metadata.TryGetValue("TilesetExtensions", out yaml))
				TilesetExtensions = yaml.ToDictionary(kv => kv.Value);

			if (metadata.TryGetValue("TilesetCodes", out yaml))
				TilesetCodes = yaml.ToDictionary(kv => kv.Value);

			if (metadata.TryGetValue("TilesetSuffixes", out yaml))
				TilesetSuffixes = yaml.ToDictionary(kv => kv.Value);
		}

		public override ISpriteSequence CreateSequence(ModData modData, TileSet tileSet, SpriteCache cache, string sequence, string animation, MiniYaml info)
		{
			return new ExtendedTilesetSpecificSpriteSequence(modData, tileSet, cache, this, sequence, animation, info);
		}
	}

	public class ExtendedTilesetSpecificSpriteSequence : DefaultSpriteSequence
	{
		public ExtendedTilesetSpecificSpriteSequence(ModData modData, TileSet tileSet, SpriteCache cache, ISpriteSequenceLoader loader, string sequence, string animation, MiniYaml info)
			: base(modData, tileSet, cache, loader, sequence, animation, info) { }

		string ResolveTilesetId(TileSet tileSet, Dictionary<string, MiniYaml> d)
		{
			var tsId = tileSet.Id;

			MiniYaml yaml;
			if (d.TryGetValue("TilesetOverrides", out yaml))
			{
				var tsNode = yaml.Nodes.FirstOrDefault(n => n.Key == tsId);
				if (tsNode != null)
					tsId = tsNode.Value.Value;
			}

			return tsId;
		}

		protected override string GetSpriteSrc(ModData modData, TileSet tileSet, string sequence, string animation, string sprite, Dictionary<string, MiniYaml> d)
		{
			var loader = (ExtendedTilesetSpecificSpriteSequenceLoader)Loader;

			var spriteName = sprite ?? sequence;

			if (LoadField(d, "UseTilesetCode", false))
			{
				string code;
				if (loader.TilesetCodes.TryGetValue(ResolveTilesetId(tileSet, d), out code))
					spriteName = spriteName.Substring(0, 1) + code + spriteName.Substring(2, spriteName.Length - 2);
			}

			if (LoadField(d, "UseTilesetSuffix", false))
			{
				string tilesetSuffix;
				if (loader.TilesetSuffixes.TryGetValue(ResolveTilesetId(tileSet, d), out tilesetSuffix))
					spriteName = spriteName + tilesetSuffix;
			}

			if (LoadField(d, "AddExtension", true))
			{
				var useTilesetExtension = LoadField(d, "UseTilesetExtension", false);

				string tilesetExtension;
				if (useTilesetExtension && loader.TilesetExtensions.TryGetValue(ResolveTilesetId(tileSet, d), out tilesetExtension))
					return spriteName + tilesetExtension;

				return spriteName + loader.DefaultSpriteExtension;
			}

			return spriteName;
		}
	}
}
