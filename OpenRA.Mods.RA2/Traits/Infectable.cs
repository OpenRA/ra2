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

using System.Collections.Generic;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("Handle infection by infector units.")]
	public class InfectableInfo : ConditionalTraitInfo, Requires<HealthInfo>
	{
		[Desc("Damage types that removes the infector.")]
		public readonly BitSet<DamageType> RemoveInfectorDamageTypes = default(BitSet<DamageType>);

		[Desc("Damage types that kills the infector.")]
		public readonly BitSet<DamageType> KillInfectorDamageTypes = default(BitSet<DamageType>);

		[GrantedConditionReference]
		[Desc("The condition to grant to self while infected by any actor.")]
		public readonly string InfectedCondition = null;

		[GrantedConditionReference]
		[Desc("Condition granted when being infected by another actor.")]
		public readonly string BeingInfectedCondition = null;

		[Desc("Conditions to grant when infected by specified actors.",
			"A dictionary of [actor id]: [condition].")]
		public readonly Dictionary<string, string> InfectedByConditions = new Dictionary<string, string>();

		[GrantedConditionReference]
		public IEnumerable<string> LinterConditions { get { return InfectedByConditions.Values; } }

		public override object Create(ActorInitializer init) { return new Infectable(init.Self, this); }
	}

	public class Infectable : ConditionalTrait<InfectableInfo>, ISync, ITick, INotifyCreated, INotifyDamage, INotifyKilled, IRemoveInfector
	{
		readonly Health health;

		public Actor Infector;
		public AttackInfect AttackInfect;

		public int[] FirepowerMultipliers = new int[] { };

		[Sync]
		public int Ticks;

		int beingInfectedToken = Actor.InvalidConditionToken;
		Actor enteringInfector;
		int infectedToken = Actor.InvalidConditionToken;
		int infectedByToken = Actor.InvalidConditionToken;

		public Infectable(Actor self, InfectableInfo info)
			: base(info)
		{
			health = self.Trait<Health>();
		}

		public bool TryStartInfecting(Actor self, Actor infector)
		{
			if (infector != null)
			{
				if (enteringInfector == null)
				{
					enteringInfector = infector;

					if (beingInfectedToken == Actor.InvalidConditionToken && !string.IsNullOrEmpty(Info.BeingInfectedCondition))
						beingInfectedToken = self.GrantCondition(Info.BeingInfectedCondition);

					return true;
				}
			}

			return false;
		}

		public void GrantCondition(Actor self)
		{
			if (infectedToken == Actor.InvalidConditionToken && !string.IsNullOrEmpty(Info.InfectedCondition))
				infectedToken = self.GrantCondition(Info.InfectedCondition);

			string infectedByCondition;
			if (Info.InfectedByConditions.TryGetValue(Infector.Info.Name, out infectedByCondition))
				infectedByToken = self.GrantCondition(infectedByCondition);
		}

		public void RevokeCondition(Actor self, Actor infector = null)
		{
			if (infector != null)
			{
				if (enteringInfector == infector)
				{
					enteringInfector = null;

					if (beingInfectedToken != Actor.InvalidConditionToken)
						beingInfectedToken = self.RevokeCondition(beingInfectedToken);
				}
			}
			else
			{
				if (infectedToken != Actor.InvalidConditionToken)
					infectedToken = self.RevokeCondition(infectedToken);

				if (infectedByToken != Actor.InvalidConditionToken)
					infectedByToken = self.RevokeCondition(infectedByToken);
			}
		}

		void RemoveInfector(Actor self, bool kill, AttackInfo info)
		{
			if (Infector != null && !Infector.IsDead)
			{
				var positionable = Infector.TraitOrDefault<IPositionable>();
				if (positionable != null)
					positionable.SetPosition(Infector, self.CenterPosition);

				self.World.AddFrameEndTask(w =>
				{
					if (Infector == null || Infector.IsDead)
						return;

					w.Add(Infector);

					if (kill)
					{
						if (info != null)
							Infector.Kill(info.Attacker, info.Damage.DamageTypes);
						else
							Infector.Kill(self);
					}
					else
					{
						var mobile = Infector.TraitOrDefault<Mobile>();
						if (mobile != null)
							mobile.Nudge(Infector);
					}

					RevokeCondition(self);
					Infector = null;
					FirepowerMultipliers = new int[] { };
				});
			}
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (Infector != null)
			{
				var info = AttackInfect.InfectInfo;
				if (e.Damage.DamageTypes.Overlaps(Info.KillInfectorDamageTypes))
					RemoveInfector(self, true, e);
				else if (e.Damage.DamageTypes.Overlaps(Info.RemoveInfectorDamageTypes))
					RemoveInfector(self, false, e);
			}
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (Infector != null)
			{
				var info = AttackInfect.InfectInfo;
				var kill = !info.SurviveHostDamageTypes.Overlaps(e.Damage.DamageTypes);
				RemoveInfector(self, kill, e);
			}
		}

		void ITick.Tick(Actor self)
		{
			if (!IsTraitDisabled && Infector != null)
			{
				if (--Ticks < 0)
				{
					var info = AttackInfect.InfectInfo;
					var damage = Util.ApplyPercentageModifiers(info.Damage, FirepowerMultipliers);
					health.InflictDamage(self, Infector, new Damage(damage, info.DamageTypes), false);

					Ticks = info.DamageInterval;
				}
			}
		}

		void IRemoveInfector.RemoveInfector(Actor self, bool kill, AttackInfo e)
		{
			RemoveInfector(self, kill, e);
		}
	}
}
