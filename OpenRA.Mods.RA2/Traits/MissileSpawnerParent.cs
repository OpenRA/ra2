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
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("This actor can spawn missile actors.")]
	public class MissileSpawnerParentInfo : BaseSpawnerParentInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant to self right after launching a spawned unit.")]
		public readonly string LaunchingCondition = null;

		[Desc("After this many ticks, we remove the condition.")]
		public readonly int LaunchingTicks = 15;

		[Desc("Pip color for the spawn count.")]
		public readonly PipType PipType = PipType.Green;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while spawned units are loaded.",
			"Condition can stack with multiple spawns.")]
		public readonly string LoadedCondition = null;

		[Desc("Conditions to grant when specified actors are contained inside the transport.",
			"A dictionary of [actor id]: [condition].")]
		public readonly Dictionary<string, string> SpawnContainConditions = new Dictionary<string, string>();

		[GrantedConditionReference]
		public IEnumerable<string> LinterSpawnContainConditions { get { return SpawnContainConditions.Values; } }

		public override object Create(ActorInitializer init) { return new MissileSpawnerParent(init, this); }
	}

	public class MissileSpawnerParent : BaseSpawnerParent, IPips, ITick, INotifyAttack
	{
		readonly Dictionary<string, Stack<int>> spawnContainTokens = new Dictionary<string, Stack<int>>();
		readonly MissileSpawnerParentInfo info;

		readonly Stack<int> loadedTokens = new Stack<int>();

		ConditionManager conditionManager;

		int respawnTicks = 0;

		int launchCondition = ConditionManager.InvalidConditionToken;
		int launchConditionTicks;

		public MissileSpawnerParent(ActorInitializer init, MissileSpawnerParentInfo info)
			: base(init, info)
		{
			this.info = info;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			conditionManager = self.Trait<ConditionManager>();

			// Spawn initial load.
			int burst = Info.InitialActorCount == -1 ? Info.Actors.Length : Info.InitialActorCount;
			for (int i = 0; i < burst; i++)
				Replenish(self, ChildEntries);
		}

		public override void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			// Do nothing, because missiles can't be captured or mind controlled.
			return;
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

		// The rate of fire of the dummy weapon determines the launch cycle as each shot
		// invokes Attacking()
		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (IsTraitDisabled)
				return;

			if (!Info.ArmamentNames.Contains(a.Info.Name))
				return;

			// Issue retarget order for already launched ones
			foreach (var child in ChildEntries)
				if (child.IsValid)
					child.SpawnerChild.Attack(child.Actor, target);

			var childEntry = GetLaunchable();
			if (childEntry == null)
				return;

			foreach (var notify in self.TraitsImplementing<INotifyMissileSpawn>())
				notify.Launching(self, target);

			if (info.LaunchingCondition != null)
			{
				if (launchCondition == ConditionManager.InvalidConditionToken)
					launchCondition = conditionManager.GrantCondition(self, info.LaunchingCondition);

				launchConditionTicks = info.LaunchingTicks;
			}

			// Program the trajectory.
			var missile = childEntry.Actor.Trait<BallisticMissile>();
			missile.Target = Target.FromPos(target.CenterPosition);

			SpawnIntoWorld(self, childEntry.Actor, self.CenterPosition);

			Stack<int> spawnContainToken;
			if (spawnContainTokens.TryGetValue(a.Info.Name, out spawnContainToken) && spawnContainToken.Any())
				conditionManager.RevokeCondition(self, spawnContainToken.Pop());

			if (loadedTokens.Any())
				conditionManager.RevokeCondition(self, loadedTokens.Pop());

			// Queue attack order, too.
			self.World.AddFrameEndTask(w =>
			{
				// invalidate the slave entry so that slave will regen.
				childEntry.Actor = null;
			});

			// Set clock so that regen happens.
			if (respawnTicks <= 0) // Don't interrupt an already running timer!
				respawnTicks = Info.RespawnTicks;
		}

		BaseSpawnerChildEntry GetLaunchable()
		{
			foreach (var childEntry in ChildEntries)
				if (childEntry.IsValid)
					return childEntry;

			return null;
		}

		public IEnumerable<PipType> GetPips(Actor self)
		{
			if (IsTraitDisabled)
				yield break;

			var inside = 0;
			foreach (var childEntry in ChildEntries)
				if (childEntry.IsValid)
					inside++;

			for (var i = 0; i < Info.Actors.Length; i++)
			{
				if (i < inside)
					yield return info.PipType;
				else
					yield return PipType.Transparent;
			}
		}

		public override void Replenish(Actor self, BaseSpawnerChildEntry entry)
		{
			base.Replenish(self, entry);

			string spawnContainCondition;
			if (conditionManager != null)
			{
				if (info.SpawnContainConditions.TryGetValue(entry.Actor.Info.Name, out spawnContainCondition))
					spawnContainTokens.GetOrAdd(entry.Actor.Info.Name).Push(conditionManager.GrantCondition(self, spawnContainCondition));

				if (!string.IsNullOrEmpty(info.LoadedCondition))
					loadedTokens.Push(conditionManager.GrantCondition(self, info.LoadedCondition));
			}
		}

		void ITick.Tick(Actor self)
		{
			if (launchCondition != ConditionManager.InvalidConditionToken && --launchConditionTicks < 0)
				launchCondition = conditionManager.RevokeCondition(self, launchCondition);

			if (respawnTicks > 0)
			{
				respawnTicks--;

				// Time to respawn someting.
				if (respawnTicks <= 0)
				{
					Replenish(self, ChildEntries);

					// If there's something left to spawn, restart the timer.
					if (SelectEntryToSpawn(ChildEntries) != null)
						respawnTicks = Util.ApplyPercentageModifiers(Info.RespawnTicks, reloadModifiers.Select(rm => rm.GetReloadModifier()));
				}
			}
		}
	}
}
