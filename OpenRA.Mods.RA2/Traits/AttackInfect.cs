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

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("Move onto the target then execute the attack.")]
	public class AttackInfectInfo : AttackFrontalInfo, Requires<MobileInfo>
	{
		[Desc("Range of the final jump of the infector.")]
		public readonly WDist JumpRange = WDist.Zero;

		[Desc("Conditions that last from start of the joust until the attack.")]
		[GrantedConditionReference]
		public readonly string JumpCondition = "jumping";

		[FieldLoader.Require]
		[Desc("How much damage to deal.")]
		public readonly int Damage = 0;

		[FieldLoader.Require]
		[Desc("How often to deal the damage.")]
		public readonly int DamageInterval = 0;

		[Desc("Damage types for the infection damage.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		[Desc("Damage types which allows the infector survive when it's host dies.")]
		public readonly BitSet<DamageType> SurviveHostDamageTypes = default(BitSet<DamageType>);

		public override object Create(ActorInitializer init) { return new AttackInfect(init.Self, this); }
	}

	public class AttackInfect : AttackFrontal
	{
		public readonly AttackInfectInfo InfectInfo;

		ConditionManager conditionManager;
		int joustToken = ConditionManager.InvalidConditionToken;

		public AttackInfect(Actor self, AttackInfectInfo info)
			: base(self, info)
		{
			InfectInfo = info;
		}

		protected override void Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
			base.Created(self);
		}

		protected override bool CanAttack(Actor self, Target target)
		{
			if (target.Type != TargetType.Actor)
				return false;

			if (self.Location == target.Actor.Location && HasAnyValidWeapons(target))
				return true;

			return base.CanAttack(self, target);
		}

		public void GrantJoustCondition(Actor self)
		{
			if (conditionManager != null && !string.IsNullOrEmpty(InfectInfo.JumpCondition))
				joustToken = conditionManager.GrantCondition(self, InfectInfo.JumpCondition);
		}

		public void RevokeJoustCondition(Actor self)
		{
			if (joustToken != ConditionManager.InvalidConditionToken)
				joustToken = conditionManager.RevokeCondition(self, joustToken);
		}

		public override Activity GetAttackActivity(Actor self, AttackSource source, Target newTarget, bool allowMove, bool forceAttack, Color? targetLineColor)
		{
			return new Infect(self, newTarget, this, targetLineColor);
		}
	}
}
