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
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Orders
{
	public class EnterGarrisonOrderTargeter<GarrisonableInfo> : UnitOrderTargeter where GarrisonableInfo : ITraitInfo
	{
		readonly Func<Actor, TargetModifiers, bool> canTarget;
		readonly Func<Actor, bool> useEnterCursor;
		GarrisonerInfo garrisonerInfo;

		public EnterGarrisonOrderTargeter(string order, int priority,
			Func<Actor, TargetModifiers, bool> canTarget, Func<Actor, bool> useEnterCursor, GarrisonerInfo garrisonerInfo)
			: base(order, priority, "enter", true, true)
		{
			this.canTarget = canTarget;
			this.useEnterCursor = useEnterCursor;
			this.garrisonerInfo = garrisonerInfo;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			if (garrisonerInfo.TargetStances.HasStance(self.Owner.Stances[target.Owner]) && target.Info.HasTraitInfo<GarrisonableInfo>() && canTarget(target, modifiers))
			{
				cursor = useEnterCursor(target) ? "enter" : "enter-blocked";
				return true;
			}
			else
				return false;
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			if (target.Info.HasTraitInfo<GarrisonableInfo>())
				return true;

			return false;
		}
	}
}
