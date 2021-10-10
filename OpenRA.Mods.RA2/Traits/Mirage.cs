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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("Tag trait for trees used as mirage.")]
	public class MirageTargetInfo : TraitInfo<MirageTarget> { }
	public class MirageTarget { }

	[Desc("Overrides the default Tooltip to aid in deceiving enemy players.")]
	class MirageTooltipInfo : TooltipInfo, Requires<MirageInfo>
	{
		public override object Create(ActorInitializer init) { return new MirageTooltip(init.Self, this); }
	}

	class MirageTooltip : ConditionalTrait<MirageTooltipInfo>, ITooltip
	{
		readonly Actor self;
		readonly Mirage mirage;

		public MirageTooltip(Actor self, MirageTooltipInfo info)
			: base(info)
		{
			this.self = self;
			mirage = self.Trait<Mirage>();
		}

		public ITooltipInfo TooltipInfo { get { return Info; } }

		public Player Owner
		{
			get
			{
				if (!mirage.IsMirage || self.Owner.IsAlliedWith(self.World.RenderPlayer))
					return self.Owner;

				return self.World.Players.First(p => p.InternalName == mirage.Info.EffectiveOwner);
			}
		}
	}

	[Flags]
	public enum MirageRevealType
	{
		None = 0,
		Attack = 1,
		Move = 2,
		Unload = 4,
		Infiltrate = 8,
		Demolish = 16,
		Damage = 32,
		Heal = 64,
		SelfHeal = 128,
		Dock = 256
	}

	[Desc("This actor can appear as a different actor in specific situations.")]
	public class MirageInfo : PausableConditionalTraitInfo
	{
		[Desc("Measured in game ticks.")]
		public readonly int InitialDelay = 25;

		[Desc("Measured in game ticks.")]
		public readonly int RevealDelay = 25;

		[Desc("Events leading to the actor getting revealed. Possible values are: Attack, Move, Unload, Infiltrate, Demolish, Dock, Damage, Heal and SelfHeal.")]
		public readonly MirageRevealType RevealOn = MirageRevealType.Attack
			| MirageRevealType.Unload | MirageRevealType.Infiltrate | MirageRevealType.Demolish | MirageRevealType.Dock;

		[Desc("Map player to use as an effective owner when actor is a mirage.")]
		public readonly string EffectiveOwner = "Neutral";

		[GrantedConditionReference]
		[Desc("Granted when the mirage is active.")]
		public readonly string MirageCondition = null;

		[Desc("Backfall to these actors if none exist in the map.")]
		[ActorReference]
		public readonly string[] DefaultTargetTypes = null;

		public override object Create(ActorInitializer init) { return new Mirage(init, this); }
	}

	public class Mirage : PausableConditionalTrait<MirageInfo>, INotifyDamage, IEffectiveOwner, INotifyUnload, INotifyDemolition, INotifyInfiltration,
		INotifyAttack, ITick, INotifyCreated, INotifyHarvesterAction
	{
		[Sync]
		private int remainingTime;

		Actor self;

		bool isDocking;

		ActorInfo[] targetTypes;

		CPos? lastPos;
		bool wasMirage = false;
		int mirageToken = Actor.InvalidConditionToken;

		public bool Disguised { get { return IsMirage; } }

		public ActorInfo ActorType { get; private set; }
		public Player Owner { get { return IsMirage ? self.World.Players.First(p => p.InternalName == Info.EffectiveOwner) : null; } }

		public Mirage(ActorInitializer init, MirageInfo info)
			: base(info)
		{
			self = init.Self;
			remainingTime = info.InitialDelay;

			var targets = self.World.ActorsWithTrait<MirageTarget>().Distinct();
			targetTypes = targets.Select(a => a.Actor.Info).ToArray();

			if (!targetTypes.Any() && info.DefaultTargetTypes != null)
				targetTypes = self.World.Map.Rules.Actors.Where(a => info.DefaultTargetTypes.Contains(a.Key)).Select(a => a.Value).ToArray();

			ActorType = targetTypes.RandomOrDefault(self.World.SharedRandom);
		}

		protected override void Created(Actor self)
		{
			if (IsMirage)
			{
				wasMirage = true;
				if (mirageToken == Actor.InvalidConditionToken && !string.IsNullOrEmpty(Info.MirageCondition))
					mirageToken = self.GrantCondition(Info.MirageCondition);
			}

			base.Created(self);
		}

		public bool IsMirage { get { return !IsTraitDisabled && !IsTraitPaused && remainingTime <= 0; } }

		public void Reveal() { Reveal(Info.RevealDelay); }

		public void Reveal(int time)
		{
			remainingTime = Math.Max(remainingTime, time);
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel) { if (Info.RevealOn.HasFlag(MirageRevealType.Attack)) Reveal(); }

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage.Value == 0)
				return;

			var type = e.Damage.Value < 0
				? (e.Attacker == self ? MirageRevealType.SelfHeal : MirageRevealType.Heal)
				: MirageRevealType.Damage;
			if (Info.RevealOn.HasFlag(type))
				Reveal();
		}

		void ITick.Tick(Actor self)
		{
			if (!IsTraitDisabled && !IsTraitPaused)
			{
				if (remainingTime > 0 && !isDocking)
					remainingTime--;

				if (Info.RevealOn.HasFlag(MirageRevealType.Move) && (lastPos == null || lastPos.Value != self.Location))
				{
					Reveal();
					lastPos = self.Location;
				}
			}

			var isMirage = IsMirage;
			if (isMirage && !wasMirage)
			{
				if (mirageToken == Actor.InvalidConditionToken && !string.IsNullOrEmpty(Info.MirageCondition))
					mirageToken = self.GrantCondition(Info.MirageCondition);
			}
			else if (!isMirage && wasMirage)
			{
				if (mirageToken != Actor.InvalidConditionToken)
					mirageToken = self.RevokeCondition(mirageToken);
			}

			wasMirage = isMirage;
		}

		protected override void TraitEnabled(Actor self)
		{
			remainingTime = Info.InitialDelay;
		}

		protected override void TraitDisabled(Actor self) { Reveal(); }

		void INotifyHarvesterAction.MovingToResources(Actor self, CPos targetCell) { }

		void INotifyHarvesterAction.MovingToRefinery(Actor self, Actor refineryActor) { }

		void INotifyHarvesterAction.MovementCancelled(Actor self) { }

		void INotifyHarvesterAction.Harvested(Actor self, string resourceType) { }

		void INotifyHarvesterAction.Docked()
		{
			if (Info.RevealOn.HasFlag(MirageRevealType.Dock))
			{
				isDocking = true;
				Reveal();
			}
		}

		void INotifyHarvesterAction.Undocked()
		{
			isDocking = false;
		}

		void INotifyUnload.Unloading(Actor self)
		{
			if (Info.RevealOn.HasFlag(MirageRevealType.Unload))
				Reveal();
		}

		void INotifyDemolition.Demolishing(Actor self)
		{
			if (Info.RevealOn.HasFlag(MirageRevealType.Demolish))
				Reveal();
		}

		void INotifyInfiltration.Infiltrating(Actor self)
		{
			if (Info.RevealOn.HasFlag(MirageRevealType.Infiltrate))
				Reveal();
		}
	}
}
