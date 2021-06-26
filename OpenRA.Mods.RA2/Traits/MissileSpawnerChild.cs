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

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("This unit is launched by a missile spawner parent.")]
	public class MissileSpawnerChildInfo : BaseSpawnerChildInfo
	{
		public override object Create(ActorInitializer init) { return new MissileSpawnerChild(init, this); }
	}

	public class MissileSpawnerChild : BaseSpawnerChild
	{
		public MissileSpawnerChild(ActorInitializer init, MissileSpawnerChildInfo info)
			: base(init, info) { }
	}
}
