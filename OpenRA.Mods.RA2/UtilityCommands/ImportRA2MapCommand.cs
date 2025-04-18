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
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.Cnc.FileFormats;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.FileFormats;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.UtilityCommands
{
	sealed class ImportRA2MapCommand : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--import-ra2-map"; } }
		bool IUtilityCommand.ValidateArguments(string[] args) { return args.Length >= 2; }

		static readonly Dictionary<byte, string> OverlayToActor = new()
		{
			{ 0x01, "gasand" },
			{ 0x03, "gawall" },
			{ 0x18, "bridge1" },
			{ 0x19, "bridge2" },
			{ 0x1A, "nawall" },
			{ 0x27, "tracks01" },
			{ 0x28, "tracks02" },
			{ 0x29, "tracks03" },
			{ 0x2A, "tracks04" },
			{ 0x2B, "tracks05" },
			{ 0x2C, "tracks06" },
			{ 0x2D, "tracks07" },
			{ 0x2E, "tracks08" },
			{ 0x2F, "tracks09" },
			{ 0x30, "tracks10" },
			{ 0x31, "tracks11" },
			{ 0x32, "tracks12" },
			{ 0x33, "tracks13" },
			{ 0x34, "tracks14" },
			{ 0x35, "tracks15" },
			{ 0x36, "tracks16" },
			{ 0x37, "tracktunnel01" },
			{ 0x38, "tracktunnel02" },
			{ 0x39, "tracktunnel03" },
			{ 0x3A, "tracktunnel04" },

			// Bridges
			{ 0x4A, "lobrdg_b" }, // lobrdg01
			{ 0x4B, "lobrdg_b" }, // lobrdg02
			{ 0x4C, "lobrdg_b" }, // lobrdg03
			{ 0x4D, "lobrdg_b" }, // lobrdg04
			{ 0x4E, "lobrdg_b" }, // lobrdg05
			{ 0x4F, "lobrdg_b" }, // lobrdg06
			{ 0x50, "lobrdg_b" }, // lobrdg07
			{ 0x51, "lobrdg_b" }, // lobrdg08
			{ 0x52, "lobrdg_b" }, // lobrdg09
			{ 0x53, "lobrdg_a" }, // lobrdg10
			{ 0x54, "lobrdg_a" }, // lobrdg11
			{ 0x55, "lobrdg_a" }, // lobrdg12
			{ 0x56, "lobrdg_a" }, // lobrdg13
			{ 0x57, "lobrdg_a" }, // lobrdg14
			{ 0x58, "lobrdg_a" }, // lobrdg15
			{ 0x59, "lobrdg_a" }, // lobrdg16
			{ 0x5A, "lobrdg_a" }, // lobrdg17
			{ 0x5B, "lobrdg_a" }, // lobrdg18
			{ 0x5C, "lobrdg_r_se" }, // lobrdg19
			{ 0x5D, "lobrdg_r_se" }, // lobrdg20
			{ 0x5E, "lobrdg_r_nw" }, // lobrdg21
			{ 0x5F, "lobrdg_r_nw" }, // lobrdg22
			{ 0x60, "lobrdg_r_ne" }, // lobrdg23
			{ 0x61, "lobrdg_r_ne" }, // lobrdg24
			{ 0x62, "lobrdg_r_sw" }, // lobrdg25
			{ 0x63, "lobrdg_r_sw" }, // lobrdg26
			{ 0x64, "lobrdg_b_d" }, // lobrdg27
			{ 0x65, "lobrdg_a_d" }, // lobrdg28

			// Ramps
			{ 0x7A, "lobrdg_r_se" }, // lobrdg1
			{ 0x7B, "lobrdg_r_nw" }, // lobrdg2
			{ 0x7C, "lobrdg_r_ne" }, // lobrdg3
			{ 0x7D, "lobrdg_r_sw" }, // lobrdg4

			// Other
			{ 0xA7, null }, // veinhole
			{ 0xA8, "srock01" },
			{ 0xA9, "srock02" },
			{ 0xAA, "srock03" },
			{ 0xAB, "srock04" },
			{ 0xAC, "srock05" },
			{ 0xAD, "trock01" },
			{ 0xAE, "trock02" },
			{ 0xAF, "trock03" },
			{ 0xB0, "trock04" },
			{ 0xB1, "trock05" },
			{ 0xB2, null }, // veinholedummy
			{ 0xB3, "crate" },

			// Fences
			{ 0xCB, "cafncb" }, // black fence
			{ 0xCC, "cafncw" }, // white fence

			// Concrete Bridges
			{ 0xCD, "lobrdb_b" }, // lobrdb01
			{ 0xCE, "lobrdb_b" }, // lobrdb02
			{ 0xCF, "lobrdb_b" }, // lobrdb03
			{ 0xD0, "lobrdb_b" }, // lobrdb04
			{ 0xD1, "lobrdb_b" }, // lobrdb05
			{ 0xD2, "lobrdb_b" }, // lobrdb06
			{ 0xD3, "lobrdb_b" }, // lobrdb07
			{ 0xD4, "lobrdb_b" }, // lobrdb08
			{ 0xD5, "lobrdb_b" }, // lobrdb09
			{ 0xD6, "lobrdb_a" }, // lobrdb10
			{ 0xD7, "lobrdb_a" }, // lobrdb11
			{ 0xD8, "lobrdb_a" }, // lobrdb12
			{ 0xD9, "lobrdb_a" }, // lobrdb13
			{ 0xDA, "lobrdb_a" }, // lobrdb14
			{ 0xDB, "lobrdb_a" }, // lobrdb15
			{ 0xDC, "lobrdb_a" }, // lobrdb16
			{ 0xDD, "lobrdb_a" }, // lobrdb17
			{ 0xDE, "lobrdb_a" }, // lobrdb18
			{ 0xDF, "lobrdb_r_se" }, // lobrdb19
			{ 0xE0, "lobrdb_r_se" }, // lobrdb20
			{ 0xE1, "lobrdb_r_nw" }, // lobrdb21
			{ 0xE2, "lobrdb_r_nw" }, // lobrdb22
			{ 0xE3, "lobrdb_r_ne" }, // lobrdb23
			{ 0xE4, "lobrdb_r_ne" }, // lobrdb24
			{ 0xE5, "lobrdb_r_sw" }, // lobrdb25
			{ 0xE6, "lobrdb_r_sw" }, // lobrdb26
			{ 0xE7, "lobrdb_b_d" }, // lobrdb27
			{ 0xE8, "lobrdb_a_d" }, // lobrdb28

			// Concrete Ramps
			{ 0xE9, "lobrdb_r_se" }, // lobrdb1
			{ 0xEA, "lobrdb_r_nw" }, // lobrdb2
			{ 0xEB, "lobrdb_r_ne" }, // lobrdb3
			{ 0xEC, "lobrdb_r_sw" }, // lobrdb4

			// Others
			{ 0xF0, "cakrmw" }, // Kremlin walls
			{ 0xF1, "cafncp" }, // prison camp fence
			{ 0xF2, "crate" }, // wcrate (water crate)
		};

		static readonly Dictionary<byte, Size> OverlayShapes = new()
		{
			{ 0x4A, new Size(1, 3) },
			{ 0x4B, new Size(1, 3) },
			{ 0x4C, new Size(1, 3) },
			{ 0x4D, new Size(1, 3) },
			{ 0x4E, new Size(1, 3) },
			{ 0x4F, new Size(1, 3) },
			{ 0x50, new Size(1, 3) },
			{ 0x51, new Size(1, 3) },
			{ 0x52, new Size(1, 3) },
			{ 0x53, new Size(3, 1) },
			{ 0x54, new Size(3, 1) },
			{ 0x55, new Size(3, 1) },
			{ 0x56, new Size(3, 1) },
			{ 0x57, new Size(3, 1) },
			{ 0x58, new Size(3, 1) },
			{ 0x59, new Size(3, 1) },
			{ 0x5A, new Size(3, 1) },
			{ 0x5B, new Size(3, 1) },
			{ 0x5C, new Size(1, 3) },
			{ 0x5D, new Size(1, 3) },
			{ 0x5E, new Size(1, 3) },
			{ 0x5F, new Size(1, 3) },
			{ 0x60, new Size(3, 1) },
			{ 0x61, new Size(3, 1) },
			{ 0x62, new Size(3, 1) },
			{ 0x63, new Size(3, 1) },
			{ 0x64, new Size(1, 3) },
			{ 0x65, new Size(3, 1) },
			{ 0x7A, new Size(1, 3) },
			{ 0x7B, new Size(1, 3) },
			{ 0x7C, new Size(3, 1) },
			{ 0x7D, new Size(3, 1) },
			{ 0xCD, new Size(1, 3) },
			{ 0xCE, new Size(1, 3) },
			{ 0xCF, new Size(1, 3) },
			{ 0xD0, new Size(1, 3) },
			{ 0xD1, new Size(1, 3) },
			{ 0xD2, new Size(1, 3) },
			{ 0xD3, new Size(1, 3) },
			{ 0xD4, new Size(1, 3) },
			{ 0xD5, new Size(1, 3) },
			{ 0xD6, new Size(3, 1) },
			{ 0xD7, new Size(3, 1) },
			{ 0xD8, new Size(3, 1) },
			{ 0xD9, new Size(3, 1) },
			{ 0xDA, new Size(3, 1) },
			{ 0xDB, new Size(3, 1) },
			{ 0xDC, new Size(3, 1) },
			{ 0xDD, new Size(3, 1) },
			{ 0xDE, new Size(3, 1) },
			{ 0xDF, new Size(1, 3) },
			{ 0xE0, new Size(1, 3) },
			{ 0xE1, new Size(1, 3) },
			{ 0xE2, new Size(1, 3) },
			{ 0xE3, new Size(3, 1) },
			{ 0xE4, new Size(3, 1) },
			{ 0xE5, new Size(3, 1) },
			{ 0xE6, new Size(3, 1) },
			{ 0xE7, new Size(1, 3) },
			{ 0xE8, new Size(3, 1) },
			{ 0xE9, new Size(1, 3) },
			{ 0xEA, new Size(1, 3) },
			{ 0xEB, new Size(3, 1) },
			{ 0xEC, new Size(3, 1) },
		};

		static readonly Dictionary<byte, DamageState> OverlayToHealth = new()
		{
			// 1,3 wooden bridge tiles
			{ 0x4A, DamageState.Undamaged },
			{ 0x4B, DamageState.Undamaged },
			{ 0x4C, DamageState.Undamaged },
			{ 0x4D, DamageState.Undamaged },
			{ 0x4E, DamageState.Heavy },
			{ 0x4F, DamageState.Heavy },
			{ 0x50, DamageState.Heavy },
			{ 0x51, DamageState.Critical },
			{ 0x52, DamageState.Critical },

			// 1,3 concrete bridge tiles
			{ 0xCD, DamageState.Undamaged },
			{ 0xCE, DamageState.Undamaged },
			{ 0xCF, DamageState.Undamaged },
			{ 0xD0, DamageState.Undamaged },
			{ 0xD1, DamageState.Heavy },
			{ 0xD2, DamageState.Heavy },
			{ 0xD3, DamageState.Heavy },
			{ 0xD4, DamageState.Critical },
			{ 0xD5, DamageState.Critical },

			// 3,1 wooden bridge tiles
			{ 0x53, DamageState.Undamaged },
			{ 0x54, DamageState.Undamaged },
			{ 0x55, DamageState.Undamaged },
			{ 0x56, DamageState.Undamaged },
			{ 0x57, DamageState.Heavy },
			{ 0x58, DamageState.Heavy },
			{ 0x59, DamageState.Heavy },
			{ 0x5A, DamageState.Critical },
			{ 0x5B, DamageState.Critical },

			// 3,1 concrete bridge tiles
			{ 0xD6, DamageState.Undamaged },
			{ 0xD7, DamageState.Undamaged },
			{ 0xD8, DamageState.Undamaged },
			{ 0xD9, DamageState.Undamaged },
			{ 0xDA, DamageState.Heavy },
			{ 0xDB, DamageState.Heavy },
			{ 0xDC, DamageState.Heavy },
			{ 0xDD, DamageState.Critical },
			{ 0xDE, DamageState.Critical },

			// Wooden Ramps
			{ 0x5C, DamageState.Undamaged },
			{ 0x5D, DamageState.Heavy },
			{ 0x5E, DamageState.Undamaged },
			{ 0x5F, DamageState.Heavy },
			{ 0x60, DamageState.Undamaged },
			{ 0x61, DamageState.Heavy },
			{ 0x62, DamageState.Undamaged },
			{ 0x63, DamageState.Heavy },

			// Concrete Ramps
			{ 0xDF, DamageState.Undamaged },
			{ 0xE0, DamageState.Heavy },
			{ 0xE1, DamageState.Undamaged },
			{ 0xE2, DamageState.Heavy },
			{ 0xE3, DamageState.Undamaged },
			{ 0xE4, DamageState.Heavy },
			{ 0xE5, DamageState.Undamaged },
			{ 0xE6, DamageState.Heavy },

			// Wooden ramp duplicates
			{ 0x7A, DamageState.Undamaged },
			{ 0x7B, DamageState.Undamaged },
			{ 0x7C, DamageState.Undamaged },
			{ 0x7D, DamageState.Undamaged },

			// Concrete ramp duplicates
			{ 0xE9, DamageState.Undamaged },
			{ 0xEA, DamageState.Undamaged },
			{ 0xEB, DamageState.Undamaged },
			{ 0xEC, DamageState.Undamaged },

			// Wooden dead bridge placeholders
			{ 0x64, DamageState.Undamaged },
			{ 0x65, DamageState.Undamaged },

			// Concrete dead bridge placeholders
			{ 0xE7, DamageState.Undamaged },
			{ 0xE8, DamageState.Undamaged },
		};

		static readonly Dictionary<byte, byte[]> ResourceFromOverlay = new()
		{
			// Ore
			{
				0x01, new byte[]
				{
					0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F,
					0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79,

					// third ore - sometimes used by third party mappers
					0x7F, 0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88,
					0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F, 0x90, 0x91, 0x92,
				}
			},

			// Gems
			{
				0x02, new byte[]
				{
					0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26,

					0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0x9C,
					0x9D, 0x9E, 0x9F, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6
				}
			}
		};

		static readonly string[] LampActors =
		{
			"GALITE", "INGALITE", "NEGLAMP", "REDLAMP", "NEGRED", "GRENLAMP", "BLUELAMP", "YELWLAMP",
			"INYELWLAMP", "PURPLAMP", "INPURPLAMP", "INORANLAMP", "INGRNLMP", "INREDLMP", "INBLULMP"
		};

		static readonly Dictionary<string, string> ReplaceActors = new()
		{
			{ "adog", "dog" }
		};

		[Desc("FILENAME", "Convert a Red Alert 2 map to the OpenRA format.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = utility.ModData;

			var filename = args[1];
			var file = new IniFile(File.Open(args[1], FileMode.Open));
			var basic = file.GetSection("Basic");
			var mapSection = file.GetSection("Map");
			var tileset = mapSection.GetValue("Theater", "");
			var iniSize = mapSection.GetValue("Size", "0, 0, 0, 0").Split(',').Select(int.Parse).ToArray();
			var iniBounds = mapSection.GetValue("LocalSize", "0, 0, 0, 0").Split(',').Select(int.Parse).ToArray();
			var size = new Size(iniSize[2], 2 * iniSize[3]);

			if (!utility.ModData.DefaultTerrainInfo.TryGetValue(tileset, out var terrainInfo))
				throw new InvalidDataException($"Unknown tileset {tileset}");

			var map = new Map(Game.ModData, terrainInfo, size.Width, size.Height)
			{
				Title = basic.GetValue("Name", Path.GetFileNameWithoutExtension(filename)),
				Author = "Westwood Studios",
				Bounds = new Rectangle(iniBounds[0], iniBounds[1], iniBounds[2], 2 * iniBounds[3] + 2 * iniBounds[1]),
				RequiresMod = utility.ModData.Manifest.Id
			};

			var fullSize = new int2(iniSize[2], iniSize[3]);
			ReadTiles(map, file, fullSize);
			ReadActors(map, file, "Structures", fullSize);
			ReadActors(map, file, "Units", fullSize);
			ReadActors(map, file, "Infantry", fullSize);
			ReadTerrainActors(map, file, fullSize);
			ReadWaypoints(map, file, fullSize);
			ReadOverlay(map, file, fullSize);
			ReadLighting(map, file);
			ReadLamps(map, file);

			var spawnCount = map.ActorDefinitions.Count(n => n.Value.Value == "mpspawn");
			var mapPlayers = new MapPlayers(map.Rules, spawnCount);
			map.PlayerDefinitions = mapPlayers.ToMiniYaml();

			var dest = Path.GetFileNameWithoutExtension(args[1]) + ".oramap";
			map.Save(ZipFileLoader.Create(dest));
			Console.WriteLine(dest + " saved.");
		}

		static void UnpackLZO(byte[] src, byte[] dest)
		{
			var srcOffset = 0U;
			var destOffset = 0U;

			while (destOffset < dest.Length && srcOffset < src.Length)
			{
				var srcLength = BitConverter.ToUInt16(src, (int)srcOffset);
				var destLength = (uint)BitConverter.ToUInt16(src, (int)srcOffset + 2);
				srcOffset += 4;
				LZOCompression.DecodeInto(src, srcOffset, srcLength, dest, destOffset, ref destLength);
				srcOffset += srcLength;
				destOffset += destLength;
			}
		}

		static void UnpackLCW(byte[] src, byte[] dest, byte[] temp)
		{
			var srcOffset = 0;
			var destOffset = 0;

			while (destOffset < dest.Length)
			{
				var srcLength = BitConverter.ToUInt16(src, srcOffset);
				var destLength = BitConverter.ToUInt16(src, srcOffset + 2);
				srcOffset += 4;
				LCWCompression.DecodeInto(src, temp, srcOffset);
				Array.Copy(temp, 0, dest, destOffset, destLength);
				srcOffset += srcLength;
				destOffset += destLength;
			}
		}

		static void ReadTiles(Map map, IniFile file, int2 fullSize)
		{
			var terrainInfo = (ITemplatedTerrainInfo)Game.ModData.DefaultTerrainInfo[map.Tileset];
			var mapSection = file.GetSection("IsoMapPack5");

			var data = Convert.FromBase64String(string.Concat(mapSection.Select(kvp => kvp.Value)));
			var cells = (fullSize.X * 2 - 1) * fullSize.Y;
			var lzoPackSize = cells * 11 + 4; // last 4 bytes contains a lzo pack header saying no more data is left
			var isoMapPack = new byte[lzoPackSize];
			UnpackLZO(data, isoMapPack);

			var mf = new MemoryStream(isoMapPack);
			for (var i = 0; i < cells; i++)
			{
				var rx = mf.ReadUInt16();
				var ry = mf.ReadUInt16();
				var tilenum = mf.ReadUInt16();
				/*var zero1 = */mf.ReadInt16();
				var subtile = mf.ReadUInt8();
				var z = mf.ReadUInt8();
				/*var zero2 = */mf.ReadUInt8();

				var dx = rx - ry + fullSize.X - 1;
				var dy = rx + ry - fullSize.X - 1;
				var mapCell = new MPos(dx / 2, dy);
				var cell = mapCell.ToCPos(map);

				if (map.Tiles.Contains(cell))
				{
					if (!terrainInfo.Templates.ContainsKey(tilenum))
						tilenum = subtile = 0;

					map.Tiles[cell] = new TerrainTile(tilenum, subtile);
					map.Height[cell] = z;
				}
			}
		}

		static void ReadOverlay(Map map, IniFile file, int2 fullSize)
		{
			var overlaySection = file.GetSection("OverlayPack");
			var overlayCompressed = Convert.FromBase64String(string.Concat(overlaySection.Select(kvp => kvp.Value)));
			var overlayPack = new byte[1 << 18];
			var temp = new byte[1 << 18];
			UnpackLCW(overlayCompressed, overlayPack, temp);

			var overlayDataSection = file.GetSection("OverlayDataPack");
			var overlayDataCompressed = Convert.FromBase64String(string.Concat(overlayDataSection.Select(kvp => kvp.Value)));
			var overlayDataPack = new byte[1 << 18];
			UnpackLCW(overlayDataCompressed, overlayDataPack, temp);

			var overlayIndex = new CellLayer<int>(map);
			overlayIndex.Clear(0xFF);

			for (var y = 0; y < fullSize.Y; y++)
			{
				for (var x = fullSize.X * 2 - 2; x >= 0; x--)
				{
					var dx = (ushort)x;
					var dy = (ushort)(y * 2 + x % 2);

					var uv = new MPos(dx / 2, dy);
					var rx = (ushort)((dx + dy) / 2 + 1);
					var ry = (ushort)(dy - rx + fullSize.X + 1);

					if (!map.Resources.Contains(uv))
						continue;

					overlayIndex[uv] = rx + 512 * ry;
				}
			}

			foreach (var cell in map.AllCells)
			{
				var overlayType = overlayPack[overlayIndex[cell]];
				if (overlayType == 0xFF)
					continue;

				if (OverlayToActor.TryGetValue(overlayType, out var actorType))
				{
					if (string.IsNullOrEmpty(actorType))
						continue;

					var shape = new Size(1, 1);
					if (OverlayShapes.TryGetValue(overlayType, out shape))
					{
						// Only import the top-left cell of multi-celled overlays
						var aboveType = overlayPack[overlayIndex[cell - new CVec(1, 0)]];
						if (shape.Width > 1 && aboveType != 0xFF)
							if (OverlayToActor.TryGetValue(aboveType, out var a) && a == actorType)
								continue;

						var leftType = overlayPack[overlayIndex[cell - new CVec(0, 1)]];
						if (shape.Height > 1 && leftType != 0xFF)
							if (OverlayToActor.TryGetValue(leftType, out var a) && a == actorType)
								continue;
					}

					var ar = new ActorReference(actorType)
					{
						new LocationInit(cell),
						new OwnerInit("Neutral")
					};

					if (OverlayToHealth.TryGetValue(overlayType, out var damageState))
					{
						var health = 100;
						if (damageState == DamageState.Critical)
							health = 25;
						else if (damageState == DamageState.Heavy)
							health = 50;
						else if (damageState == DamageState.Medium)
							health = 75;

						if (health != 100)
							ar.Add(new HealthInit(health));
					}

					map.ActorDefinitions.Add(new MiniYamlNode("Actor" + map.ActorDefinitions.Count, ar.Save()));

					continue;
				}

				var resourceType = ResourceFromOverlay
					.Where(kv => kv.Value.Contains(overlayType))
					.Select(kv => kv.Key)
					.FirstOrDefault();

				if (resourceType != 0)
				{
					map.Resources[cell] = new ResourceTile(resourceType, overlayDataPack[overlayIndex[cell]]);
					continue;
				}

				Console.WriteLine($"{cell} unknown overlay {overlayType}");
			}
		}

		static void ReadWaypoints(Map map, IniFile file, int2 fullSize)
		{
			var waypointsSection = file.GetSection("Waypoints", true);
			foreach (var kv in waypointsSection)
			{
				var pos = int.Parse(kv.Value);
				var ry = pos / 1000;
				var rx = pos - ry * 1000;
				var dx = rx - ry + fullSize.X - 1;
				var dy = rx + ry - fullSize.X - 1;
				var cell = new MPos(dx / 2, dy).ToCPos(map);

				var ar = new ActorReference((!int.TryParse(kv.Key, out var wpindex) || wpindex > 7) ? "waypoint" : "mpspawn")
				{
					new LocationInit(cell),
					new OwnerInit("Neutral")
				};

				map.ActorDefinitions.Add(new MiniYamlNode("Actor" + map.ActorDefinitions.Count, ar.Save()));
			}
		}

		static void ReadTerrainActors(Map map, IniFile file, int2 fullSize)
		{
			var terrainSection = file.GetSection("Terrain", true);
			foreach (var kv in terrainSection)
			{
				var pos = int.Parse(kv.Key);
				var ry = pos / 1000;
				var rx = pos - ry * 1000;
				var dx = rx - ry + fullSize.X - 1;
				var dy = rx + ry - fullSize.X - 1;
				var cell = new MPos(dx / 2, dy).ToCPos(map);
				var name = kv.Value.ToLowerInvariant();

				if (ReplaceActors.TryGetValue(name, out var newName))
					name = newName;

				var ar = new ActorReference(name)
				{
					new LocationInit(cell),
					new OwnerInit("Neutral")
				};

				if (!map.Rules.Actors.ContainsKey(name))
					Console.WriteLine($"Ignoring unknown actor type: `{name}`");
				else
					map.ActorDefinitions.Add(new MiniYamlNode("Actor" + map.ActorDefinitions.Count, ar.Save()));
			}
		}

		static void ReadActors(Map map, IniFile file, string type, int2 fullSize)
		{
			var structuresSection = file.GetSection(type, true);
			foreach (var kv in structuresSection)
			{
				var isDeployed = false;
				var entries = kv.Value.Split(',');

				var name = entries[1].ToLowerInvariant();

				var health = short.Parse(entries[2]);
				var rx = int.Parse(entries[3]);
				var ry = int.Parse(entries[4]);
				var facing = (byte)(224 - byte.Parse(entries[type == "Infantry" ? 7 : 5]));

				var dx = rx - ry + fullSize.X - 1;
				var dy = rx + ry - fullSize.X - 1;
				var cell = new MPos(dx / 2, dy).ToCPos(map);

				var ar = new ActorReference(name)
				{
					new LocationInit(cell),
					new OwnerInit("Neutral")
				};

				if (type == "Infantry")
				{
					var subcell = 0;
					switch (byte.Parse(entries[5]))
					{
						case 2: subcell = 3; break;
						case 3: subcell = 1; break;
						case 4: subcell = 2; break;
					}

					if (subcell != 0)
						ar.Add(new SubCellInit((SubCell)subcell));
				}

				if (health != 256)
					ar.Add(new HealthInit(100 * health / 256));

				ar.Add(new FacingInit(WAngle.FromFacing(facing)));

				if (isDeployed)
					ar.Add(new DeployStateInit(DeployState.Deployed));

				if (!map.Rules.Actors.ContainsKey(name))
					Console.WriteLine($"Ignoring unknown actor type: `{name}`");
				else
					map.ActorDefinitions.Add(new MiniYamlNode("Actor" + map.ActorDefinitions.Count, ar.Save()));
			}
		}

		static void ReadLighting(Map map, IniFile file)
		{
			var lightingTypes = new Dictionary<string, string>()
			{
				{ "Red", "RedTint" },
				{ "Green", "GreenTint" },
				{ "Blue", "BlueTint" },
				{ "Ambient", "Intensity" },
				{ "Level", "HeightStep" },
				{ "Ground", null }
			};

			var lightingSection = file.GetSection("Lighting");
			var parsed = new Dictionary<string, float>();
			var lightingNodes = new List<MiniYamlNode>();

			foreach (var kv in lightingSection)
			{
				if (lightingTypes.ContainsKey(kv.Key))
					parsed[kv.Key] = FieldLoader.GetValue<float>(kv.Key, kv.Value);
				else
					Console.WriteLine($"Ignoring unknown lighting type: `{kv.Key}`");
			}

			// Merge Ground into Ambient
			if (parsed.TryGetValue("Ground", out var ground))
			{
				if (!parsed.ContainsKey("Ambient"))
					parsed["Ambient"] = 1f;
				parsed["Ambient"] -= ground;
			}

			foreach (var node in lightingTypes)
			{
				if (node.Value != null && parsed.TryGetValue(node.Key, out var val) && ((node.Key == "Level" && val != 0) || (node.Key != "Level" && val != 1.0f)))
					lightingNodes.Add(new MiniYamlNode(node.Value, FieldSaver.FormatValue(val)));
			}

			if (lightingNodes.Count > 0)
			{
				map.RuleDefinitions.Nodes.Add(new MiniYamlNode("^BaseWorld", new MiniYaml("", new List<MiniYamlNode>()
				{
					new("TerrainLighting", new MiniYaml("", lightingNodes))
				})));
			}
		}

		static void ReadLamps(Map map, IniFile file)
		{
			var lightingTypes = new Dictionary<string, string>()
			{
				{ "LightIntensity", "Intensity" },
				{ "LightRedTint", "RedTint" },
				{ "LightGreenTint", "GreenTint" },
				{ "LightBlueTint", "BlueTint" },
			};

			foreach (var lamp in LampActors)
			{
				var lightingSection = file.GetSection(lamp, true);
				var lightingNodes = new List<MiniYamlNode>();

				foreach (var kv in lightingSection)
				{
					if (kv.Key == "LightVisibility")
					{
						// Convert leptons to WDist
						var visibility = FieldLoader.GetValue<int>(kv.Key, kv.Value);
						lightingNodes.Add(new MiniYamlNode("Range", FieldSaver.FormatValue(new WDist(visibility * 4))));
					}
					else if (lightingTypes.TryGetValue(kv.Key, out var lightingType))
					{
						// Some maps use "," instead of "."!
						var value = FieldLoader.GetValue<float>(kv.Key, kv.Value.Replace(',', '.'));
						lightingNodes.Add(new MiniYamlNode(lightingType, FieldSaver.FormatValue(value)));
					}
				}

				if (lightingNodes.Count > 0)
				{
					map.RuleDefinitions.Nodes.Add(new MiniYamlNode(lamp, new MiniYaml("", new List<MiniYamlNode>()
					{
						new("TerrainLightSource", new MiniYaml("", lightingNodes))
					})));
				}
			}
		}
	}
}
