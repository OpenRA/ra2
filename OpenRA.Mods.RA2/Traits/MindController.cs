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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("This actor can mind control other actors.")]
	public class MindControllerInfo : PausableConditionalTraitInfo, Requires<ArmamentInfo>, Requires<HealthInfo>
	{
		[Desc("Name of the armaments that grant this condition.")]
		public readonly HashSet<string> ArmamentNames = new HashSet<string>() { "primary" };

		[Desc("Up to how many units can this unit control?",
			"Use 0 or negative numbers for infinite.")]
		public readonly int Capacity = 1;

		[Desc("If the capacity is reached, discard the oldest mind controlled unit and control the new one",
			"If false, controlling new units is forbidden after capacity is reached.")]
		public readonly bool DiscardOldest = true;

		[Desc("Condition to grant to self when controlling actors. Can stack up by the number of enslaved actors. You can use this to forbid firing of the dummy MC weapon.")]
		[GrantedConditionReference]
		[FieldLoader.Require]
		public readonly string ControllingCondition = null;

		[Desc("The sound played when the unit is mindcontrolled.")]
		public readonly string[] Sounds = { };

		[Desc("PipType to use for indicating mindcontrolled units.")]
		public readonly PipType PipType = PipType.Green;

		[Desc("PipType to use for indicating unused mindcontrol slots.")]
		public readonly PipType PipTypeEmpty = PipType.Transparent;

		public override object Create(ActorInitializer init) { return new MindController(init.Self, this); }
	}

	public class MindController : PausableConditionalTrait<MindControllerInfo>, INotifyAttack, IPips, INotifyKilled, INotifyActorDisposing, INotifyCreated
	{
		readonly List<Actor> slaves = new List<Actor>();

		Stack<int> controllingTokens = new Stack<int>();
		ConditionManager conditionManager;

		public IEnumerable<Actor> Slaves { get { return slaves; } }

		public MindController(Actor self, MindControllerInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		void StackControllingCondition(Actor self, string condition)
		{
			if (conditionManager == null)
				return;

			if (string.IsNullOrEmpty(condition))
				return;

			controllingTokens.Push(conditionManager.GrantCondition(self, condition));
		}

		void UnstackControllingCondition(Actor self, string condition)
		{
			if (conditionManager == null)
				return;

			if (string.IsNullOrEmpty(condition))
				return;

			conditionManager.RevokeCondition(self, controllingTokens.Pop());
		}

		public IEnumerable<PipType> GetPips(Actor self)
		{
			if (Info.Capacity > 0)
			{
				for (int i = slaves.Count(); i > 0; i--)
					yield return Info.PipType;

				for (int i = Info.Capacity - slaves.Count(); i > 0; i--)
					yield return Info.PipTypeEmpty;
			}
			else if (slaves.Count() >= -Info.Capacity)
			{
				for (int i = -Info.Capacity; i > 0; i--)
					yield return Info.PipType;
			}
			else
			{
				for (int i = slaves.Count(); i > 0; i--)
					yield return Info.PipType;

				for (int i = -Info.Capacity - slaves.Count(); i > 0; i--)
					yield return Info.PipTypeEmpty;
			}
		}

		public void UnlinkSlave(Actor self, Actor slave)
		{
			if (slaves.Contains(slave))
			{
				slaves.Remove(slave);
				UnstackControllingCondition(self, Info.ControllingCondition);
			}
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (!Info.ArmamentNames.Contains(a.Info.Name))
				return;

			if (target.Actor == null || !target.IsValidFor(self))
				return;

			if (self.Owner.Stances[target.Actor.Owner] == Stance.Ally)
				return;

			var mindControllable = target.Actor.TraitOrDefault<MindControllable>();

			if (mindControllable == null)
			{
				throw new InvalidOperationException(
					"`{0}` tried to mindcontrol `{1}`, but the latter does not have the necessary trait!"
					.F(self.Info.Name, target.Actor.Info.Name));
			}

			if (mindControllable.IsTraitDisabled || mindControllable.IsTraitPaused)
				return;

			if (Info.Capacity > 0 && !Info.DiscardOldest && slaves.Count() >= Info.Capacity)
				return;

			slaves.Add(target.Actor);
			StackControllingCondition(self, Info.ControllingCondition);
			mindControllable.LinkMaster(target.Actor, self);

			if (Info.Sounds.Any())
				Game.Sound.Play(SoundType.World, Info.Sounds.Random(self.World.SharedRandom), self.CenterPosition);

			if (Info.Capacity > 0 && Info.DiscardOldest && slaves.Count() > Info.Capacity)
				slaves[0].Trait<MindControllable>().RevokeMindControl(slaves[0]);
		}

		void ReleaseSlaves(Actor self)
		{
			foreach (var s in slaves)
			{
				if (s.IsDead || s.Disposed)
					continue;

				s.Trait<MindControllable>().RevokeMindControl(s);
			}

			slaves.Clear();
			while (controllingTokens.Any())
				UnstackControllingCondition(self, Info.ControllingCondition);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			ReleaseSlaves(self);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			ReleaseSlaves(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			ReleaseSlaves(self);
		}
	}
}
