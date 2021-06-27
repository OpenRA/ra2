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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Activities
{
	class Infect : Enter
	{
		readonly AttackInfect attackInfect;
		readonly Target target;

		bool isPlayingInfectAnimation;

		public Infect(Actor self, Target target, AttackInfect attackInfect, Color? targetLineColor)
			: base(self, target, targetLineColor)
		{
			this.target = target;
			this.attackInfect = attackInfect;
		}

		protected override void OnFirstRun(Actor self)
		{
			attackInfect.IsAiming = true;
		}

		protected override void OnLastRun(Actor self)
		{
			attackInfect.IsAiming = false;
		}

		protected override void OnEnterComplete(Actor self, Actor targetActor)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead || attackInfect.IsTraitDisabled)
					return;

				if (isPlayingInfectAnimation)
				{
					attackInfect.RevokeJoustCondition(self);
					isPlayingInfectAnimation = false;
				}

				attackInfect.DoAttack(self, target);

				var infectable = targetActor.TraitOrDefault<Infectable>();
				if (infectable == null || infectable.IsTraitDisabled || infectable.Infector != null)
					return;

				w.Remove(self);

				infectable.Infector = self;
				infectable.AttackInfect = attackInfect;

				infectable.FirepowerMultipliers = self.TraitsImplementing<IFirepowerModifier>()
					.Select(a => a.GetFirepowerModifier()).ToArray();

				var info = attackInfect.InfectInfo;
				infectable.Ticks = info.DamageInterval;
				infectable.GrantCondition(targetActor);
				infectable.RevokeCondition(targetActor, self);
			});
		}

		void CancelInfection(Actor self)
		{
			if (isPlayingInfectAnimation)
			{
				attackInfect.RevokeJoustCondition(self);
				isPlayingInfectAnimation = false;
			}

			if (target.Type != TargetType.Actor)
				return;

			if (target.Actor.IsDead)
				return;

			var infectable = target.Actor.TraitOrDefault<Infectable>();
			if (infectable == null || infectable.IsTraitDisabled || infectable.Infector != null)
				return;

			infectable.RevokeCondition(target.Actor, self);
		}

		bool IsValidInfection(Actor self, Actor targetActor)
		{
			if (attackInfect.IsTraitDisabled)
				return false;

			if (targetActor.IsDead)
				return false;

			if (!target.IsValidFor(self) || !attackInfect.HasAnyValidWeapons(target))
				return false;

			var infectable = targetActor.TraitOrDefault<Infectable>();
			if (infectable == null || infectable.IsTraitDisabled || infectable.Infector != null)
				return false;

			return true;
		}

		bool CanStartInfect(Actor self, Actor targetActor)
		{
			if (!IsValidInfection(self, targetActor))
				return false;

			// IsValidInfection validated the lookup, no need to check here.
			var infectable = targetActor.Trait<Infectable>();
			return infectable.TryStartInfecting(targetActor, self);
		}

		protected override bool TryStartEnter(Actor self, Actor targetActor)
		{
			var canStartInfect = CanStartInfect(self, targetActor);
			if (!canStartInfect)
			{
				CancelInfection(self);
				Cancel(self, true);
			}

			// Can't leap yet
			if (attackInfect.Armaments.All(a => a.IsReloading))
				return false;

			return true;
		}

		protected override void TickInner(Actor self, Target target, bool targetIsDeadOrHiddenActor)
		{
			if (target.Type != TargetType.Actor || !IsValidInfection(self, target.Actor))
			{
				CancelInfection(self);
				Cancel(self, true);
				return;
			}

			var info = attackInfect.InfectInfo;
			if (!isPlayingInfectAnimation && !IsCanceling && (self.CenterPosition - target.CenterPosition).Length < info.JumpRange.Length)
			{
				isPlayingInfectAnimation = true;
				attackInfect.GrantJoustCondition(self);
				IsInterruptible = false;
			}
		}
	}
}
