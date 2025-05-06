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
using OpenRA.Mods.Cnc.UtilityCommands;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.UtilityCommands
{
	sealed class ImportRA2MapCommand : ImportGen2MapCommand, IUtilityCommand
	{
		string IUtilityCommand.Name => "--import-ra2-map";

		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length >= 2;

		[Desc("FILENAME", "Convert a Red Alert 2 map to the OpenRA format.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Run(utility, args);
		}

		#region Mod-specific data

		protected override Dictionary<byte, string> OverlayToActor { get; } = new()
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

		protected override Dictionary<byte, Size> OverlayShapes { get; } = new()
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

		protected override Dictionary<byte, DamageState> OverlayToHealth { get; } = new()
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

		protected override Dictionary<byte, byte[]> ResourceFromOverlay { get; } = new()
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

		// Dogs obviously don't deploy into different dogs, but there used to be a "ReplaceActors"
		// because in OG RA2 there were two separate dog actors and we just use faction-specific sprites.
		// Since what this does is replace actor A with actor B
		// and add a DeployStateInit and that init doesn't hurt,
		// we piggy-back off of it to translate to the proper actor time,
		// even if with a redundant DeployStateInit.
		protected override Dictionary<string, string> DeployableActors { get; } = new()
		{
			{ "adog", "dog" }
		};

		protected override string[] LampActors { get; } =
		{
			"GALITE", "INGALITE", "NEGLAMP", "REDLAMP", "NEGRED", "GRENLAMP", "BLUELAMP", "YELWLAMP",
			"INYELWLAMP", "PURPLAMP", "INPURPLAMP", "INORANLAMP", "INGRNLMP", "INREDLMP", "INBLULMP"
		};

		protected override string[] CreepActors { get; } = Array.Empty<string>();

		#endregion
	}
}
