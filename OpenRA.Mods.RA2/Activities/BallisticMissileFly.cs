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

using System;
using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.RA2.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Activities
{
	public class BallisticMissileFly : Activity
	{
		readonly BallisticMissile missile;
		readonly WPos initPos;
		readonly WPos targetPos;
		readonly int facing;

		int length;
		int ticks;

		public BallisticMissileFly(Actor self, Target target, BallisticMissile missile)
		{
			this.missile = missile;
			initPos = self.CenterPosition;
			targetPos = target.CenterPosition;
			length = Math.Max((targetPos - initPos).Length / missile.MovementSpeed, 1);
			facing = (targetPos - initPos).Yaw.Facing;
			missile.Facing = facing;
		}

		public override bool Tick(Actor self)
		{
			var delta = targetPos - self.CenterPosition;
			var move = missile.FlyStep(missile.Facing);

			if (delta.HorizontalLengthSquared < move.HorizontalLengthSquared)
			{
				// Snap to the target position to prevent overshooting.
				missile.SetPosition(self, targetPos);
				Queue(new CallFunc(() => self.Kill(self, missile.Info.DamageTypes)));
				return true;
			}

			length = Math.Max((targetPos - initPos).Length / missile.MovementSpeed, 1);
			var pos = WPos.LerpQuadratic(initPos, targetPos, missile.Info.LaunchAngle, ticks, length);
			missile.SetPosition(self, pos);

			ticks++;
			return false;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(targetPos);
		}
	}
}
