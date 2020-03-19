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

using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	public abstract class ChangeOwnerInfo : ITraitInfo
	{
		public abstract object Create(ActorInitializer init);
	}

	public abstract class ChangeOwner
	{
		protected void NeedChangeOwner(Actor self, Actor actor, Player newOwner)
		{
			var oldOwner = self.Owner;
			self.ChangeOwner(newOwner);
		}
	}
}
