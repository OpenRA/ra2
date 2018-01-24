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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("This actor mind control other actors")]
	public class MindControllerInfo : ConditionalTraitInfo, Requires<ArmamentInfo>, Requires<HealthInfo>
	{
		[Desc("The name of the weapon, one of its armament. Must be specified with \"Name:\" field.",
			"To limit mind controllable targets, adjust the weapon's valid target filter.")]
		public readonly string Name = "primary";

		[Desc("Up to how many units can this unit control?",
			"Use 0 or negative numbers for infinite.")]
		public readonly int Capacity = 1;

		[Desc("If the capacity is reached, discard the oldest mind controlled unit and control the new one",
			"If false controlling new units is forbidden after capacity is reached.")]
		public readonly bool DiscardOldest = true;

		[Desc("Condition to grant to self when controlling actors. Can stack up by the number of enslaved actors. You can use this to forbid firing of the dummy MC weapon.")]
		[GrantedConditionReference]
		public readonly string ControllingCondition;

		[Desc("The sound played when the unit is mind controlled.")]
		public readonly string[] Sounds = { };

		[Desc("PipType to use for indicating MC'ed units")]
		public readonly PipType PipType = PipType.Green;

		[Desc("PipType to use for indicating left over MC capacity")]
		public readonly PipType PipTypeEmpty = PipType.Transparent;

		public override object Create(ActorInitializer init) { return new MindController(init.Self, this); }
	}

	class MindController : ConditionalTrait<MindControllerInfo>, INotifyAttack, IPips, INotifyKilled, INotifyActorDisposing, INotifyCreated
	{
		readonly MindControllerInfo info;
		readonly List<Actor> slaves = new List<Actor>();

		Stack<int> controllingTokens = new Stack<int>();
		ConditionManager conditionManager;

		public IEnumerable<Actor> Slaves { get { return slaves; } }

		public MindController(Actor self, MindControllerInfo info)
			: base(info)
		{
			this.info = info;

			var armaments = self.TraitsImplementing<Armament>().Where(a => a.Info.Name == info.Name).ToArray();
			System.Diagnostics.Debug.Assert(armaments.Length == 1, "Multiple armaments with given name detected: " + info.Name);
		}

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
			if (info.Capacity > 0)
			{
				for (int i = slaves.Count(); i > 0; i--)
					yield return info.PipType;

				for (int i = info.Capacity - slaves.Count(); i > 0; i--)
					yield return info.PipTypeEmpty;
			}
			else if (slaves.Count() >= -info.Capacity)
			{
				for (int i = -info.Capacity; i > 0; i--)
					yield return info.PipType;
			}
			else
			{
				for (int i = slaves.Count(); i > 0; i--)
					yield return info.PipType;

				for (int i = -info.Capacity - slaves.Count(); i > 0; i--)
					yield return info.PipTypeEmpty;
			}
		}

		public void UnlinkSlave(Actor self, Actor slave)
		{
			if (slaves.Contains(slave))
			{
				slaves.Remove(slave);
				UnstackControllingCondition(self, info.ControllingCondition);
			}
		}

		public void PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

		public void Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (info.Name != a.Info.Name)
				return;

			if (target.Actor == null || !target.IsValidFor(self))
				return;

			if (self.Owner.Stances[target.Actor.Owner] == Stance.Ally)
				return;

			var mcable = target.Actor.TraitOrDefault<MindControllable>();

			if (mcable == null)
			{
				Game.Debug("Warning: mind control weapon targetable unit doesn't actually have mindcontrallable trait");
				return;
			}

			if (info.Capacity > 0 && !info.DiscardOldest && slaves.Count() >= info.Capacity)
				return;

			slaves.Add(target.Actor);
			StackControllingCondition(self, info.ControllingCondition);
			mcable.LinkMaster(target.Actor, self);

			if (info.Sounds.Any())
				Game.Sound.Play(SoundType.World, info.Sounds.Random(self.World.SharedRandom), self.CenterPosition);

			if (info.Capacity > 0 && info.DiscardOldest && slaves.Count() > info.Capacity)
				slaves[0].Trait<MindControllable>().UnMindControl(slaves[0], self.Owner);

		}

		void ReleaseSlaves(Actor self)
		{
			var toUnMC = slaves.ToArray();
			foreach (var s in toUnMC)
			{
				if (s.IsDead || s.Disposed)
					continue;

				s.Trait<MindControllable>().UnMindControl(s, self.Owner);
			}
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			ReleaseSlaves(self);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			ReleaseSlaves(self);
		}
	}
}