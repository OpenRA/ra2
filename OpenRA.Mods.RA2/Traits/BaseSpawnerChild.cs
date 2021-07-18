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
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("Can be bound to a SpawnerParent.")]
	public class BaseSpawnerChildInfo : TraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant to childs when the parent actor is killed.")]
		public readonly string ParentDeadCondition = null;

		[Desc("Can these actors be mind controlled or captured?")]
		public readonly bool AllowOwnerChange = false;

		[Desc("Types of damage this actor explodes with due to an unallowed child action. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		public override object Create(ActorInitializer init) { return new BaseSpawnerChild(init, this); }
	}

	public class BaseSpawnerChild : INotifyCreated, INotifyKilled, INotifyOwnerChanged
	{
		protected AttackBase[] attackBases;
		protected ConditionManager conditionManager;

		readonly BaseSpawnerChildInfo info;

		int parentDeadToken = ConditionManager.InvalidConditionToken;
		BaseSpawnerParent spawnerParent = null;

		public Actor Parent { get; private set; }

		Target lastTarget;

		public BaseSpawnerChild(ActorInitializer init, BaseSpawnerChildInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			Created(self);
		}

		protected virtual void Created(Actor self)
		{
			attackBases = self.TraitsImplementing<AttackBase>().ToArray();
			conditionManager = self.Trait<ConditionManager>();
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (Parent == null || Parent.IsDead)
				return;

			spawnerParent.OnChildKilled(Parent, self);
		}

		public virtual void LinkParent(Actor self, Actor parent, BaseSpawnerParent spawnerParent)
		{
			Parent = parent;
			this.spawnerParent = spawnerParent;
		}

		bool TargetSwitched(Target lastTarget, Target newTarget)
		{
			if (newTarget.Type != lastTarget.Type)
				return true;

			if (newTarget.Type == TargetType.Terrain)
				return newTarget.CenterPosition != lastTarget.CenterPosition;

			if (newTarget.Type == TargetType.Actor)
				return lastTarget.Actor != newTarget.Actor;

			return false;
		}

		public virtual void Stop(Actor self)
		{
			// Drop the target so that Attack() feels the need to assign target for this child.
			lastTarget = Target.Invalid;

			self.CancelActivity();
		}

		public virtual void Attack(Actor self, Target target)
		{
			// Don't have to change target or alter current activity.
			if (!TargetSwitched(lastTarget, target))
				return;

			if (!target.IsValidFor(self))
			{
				Stop(self);
				return;
			}

			lastTarget = target;

			foreach (var ab in attackBases)
			{
				if (ab.IsTraitDisabled)
					continue;

				ab.AttackTarget(target, AttackSource.Default, false, true, true);
			}
		}

		public virtual void OnParentKilled(Actor self, Actor attacker, SpawnerChildDisposal disposal)
		{
			if (conditionManager != null && !string.IsNullOrEmpty(info.ParentDeadCondition))
				parentDeadToken = conditionManager.GrantCondition(self, info.ParentDeadCondition);

			switch (disposal)
			{
				case SpawnerChildDisposal.KillChildren:
					self.Kill(attacker, info.DamageTypes);
					break;
				case SpawnerChildDisposal.GiveChildrenToAttacker:
					self.CancelActivity();
					self.ChangeOwner(attacker.Owner);
					break;
				case SpawnerChildDisposal.DoNothing:
				default:
					break;
			}
		}

		public virtual void OnParentOwnerChanged(Actor self, Player oldOwner, Player newOwner, SpawnerChildDisposal disposal)
		{
			switch (disposal)
			{
				case SpawnerChildDisposal.KillChildren:
					self.Kill(self, info.DamageTypes);
					break;
				case SpawnerChildDisposal.GiveChildrenToAttacker:
					self.CancelActivity();
					self.ChangeOwner(newOwner);
					break;
				case SpawnerChildDisposal.DoNothing:
				default:
					break;
			}
		}

		// Childs aren't good without parent so, kill it.
		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			// In this case, the child will be disposed, one way or other.
			if (Parent == null || Parent.IsDead)
				return;

			// This function got triggered because the parent got mind controlled and
			// thus triggered child.ChangeOwner().
			// In this case, do nothing.
			if (Parent.Owner == newOwner)
				return;

			if (info.AllowOwnerChange)
				return;

			self.Kill(self, info.DamageTypes);
		}
	}
}
