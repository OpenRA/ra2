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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("Explodes a weapon at the actor's position when enabled."
		+ "Reload/BurstDelays are used as explosion intervals.")]
	public class PeriodicExplosionInfo : ConditionalTraitInfo, IRulesetLoaded
	{
		[WeaponReference]
		[FieldLoader.Require]
		[Desc("Weapon to be used for explosion.")]
		public readonly string Weapon = null;

		public readonly bool ResetReloadWhenEnabled = true;

		[Desc("Which limited ammo pool should this weapon be assigned to.")]
		public readonly string AmmoPoolName = "";

		public WeaponInfo WeaponInfo { get; private set; }

		[Desc("Explosion offset relative to actor's position.")]
		public readonly WVec LocalOffset = WVec.Zero;

		public override object Create(ActorInitializer init) { return new PeriodicExplosion(init.Self, this); }

		void IRulesetLoaded<ActorInfo>.RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			WeaponInfo weaponInfo;

			var weaponToLower = Weapon.ToLowerInvariant();
			if (!rules.Weapons.TryGetValue(weaponToLower, out weaponInfo))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));

			WeaponInfo = weaponInfo;
		}
	}

	class PeriodicExplosion : ConditionalTrait<PeriodicExplosionInfo>, ITick, INotifyCreated
	{
		readonly PeriodicExplosionInfo info;
		readonly WeaponInfo weapon;
		readonly BodyOrientation body;
		readonly List<Pair<int, Action>> delayedActions = new List<Pair<int, Action>>();

		int fireDelay;
		int burst;
		AmmoPool ammoPool;

		public PeriodicExplosion(Actor self, PeriodicExplosionInfo info)
			: base(info)
		{
			this.info = info;

			weapon = info.WeaponInfo;
			burst = weapon.Burst;
			body = self.TraitOrDefault<BodyOrientation>();
		}

		protected override void Created(Actor self)
		{
			ammoPool = self.TraitsImplementing<AmmoPool>().FirstOrDefault(la => la.Info.Name == Info.AmmoPoolName);
		}

		void ITick.Tick(Actor self)
		{
			for (var i = 0; i < delayedActions.Count; i++)
			{
				var x = delayedActions[i];
				if (--x.First <= 0)
					x.Second();

				delayedActions[i] = x;
			}

			delayedActions.RemoveAll(a => a.First <= 0);

			if (IsTraitDisabled)
				return;

			if (--fireDelay < 0)
			{
				if (ammoPool != null && !ammoPool.TakeAmmo(self, 1))
					return;

				var localoffset = body != null
					? body.LocalToWorld(info.LocalOffset.Rotate(body.QuantizeOrientation(self, self.Orientation)))
					: info.LocalOffset;

				weapon.Impact(Target.FromPos(self.CenterPosition + localoffset), self,
					self.TraitsImplementing<IFirepowerModifier>().Select(a => a.GetFirepowerModifier()).ToArray());

				if (weapon.Report != null && weapon.Report.Any())
					Game.Sound.Play(SoundType.World, weapon.Report.Random(self.World.SharedRandom), self.CenterPosition);

				if (burst == weapon.Burst && weapon.StartBurstReport != null && weapon.StartBurstReport.Any())
					Game.Sound.Play(SoundType.World, weapon.StartBurstReport.Random(self.World.SharedRandom), self.CenterPosition);

				if (--burst > 0)
				{
					if (weapon.BurstDelays.Length == 1)
						fireDelay = weapon.BurstDelays[0];
					else
						fireDelay = weapon.BurstDelays[weapon.Burst - (burst + 1)];
				}
				else
				{
					var modifiers = self.TraitsImplementing<IReloadModifier>()
						.Select(m => m.GetReloadModifier());
					fireDelay = Util.ApplyPercentageModifiers(weapon.ReloadDelay, modifiers);
					burst = weapon.Burst;

					if (weapon.AfterFireSound != null && weapon.AfterFireSound.Any())
					{
						ScheduleDelayedAction(weapon.AfterFireSoundDelay, () =>
							Game.Sound.Play(SoundType.World, weapon.AfterFireSound.Random(self.World.SharedRandom), self.CenterPosition));
					}
				}
			}
		}

		protected override void TraitEnabled(Actor self)
		{
			if (info.ResetReloadWhenEnabled)
			{
				burst = weapon.Burst;
				fireDelay = 0;
			}
		}

		protected void ScheduleDelayedAction(int t, Action a)
		{
			if (t > 0)
				delayedActions.Add(Pair.New(t, a));
			else
				a();
		}
	}
}
