﻿#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("This actor can spawn actors.")]
	public class CarrierMasterInfo : BaseSpawnerMasterInfo
	{
		[Desc("Spawn is a missile that dies and not return.")]
		public readonly bool SpawnIsMissile = false;

		[Desc("Spawn rearm delay, in ticks")]
		public readonly int RearmTicks = 150;

		[GrantedConditionReference]
		[Desc("The condition to grant to self right after launching a spawned unit. (Used by V3 to make immobile.)")]
		public readonly string LaunchingCondition = null;

		[Desc("After this many ticks, we remove the condition.")]
		public readonly int LaunchingTicks = 15;

		[Desc("Pip color for the spawn count.")]
		public readonly PipType PipType = PipType.Yellow;

		[Desc("Insta-repair spawners when they return?")]
		public readonly bool InstaRepair = true;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while spawned units are loaded.",
			"Condition can stack with multiple spawns.")]
		public readonly string LoadedCondition = null;

		[Desc("Conditions to grant when specified actors are contained inside the transport.",
			"A dictionary of [actor id]: [condition].")]
		public readonly Dictionary<string, string> SpawnContainConditions = new Dictionary<string, string>();

		[GrantedConditionReference]
		public IEnumerable<string> LinterSpawnContainConditions { get { return SpawnContainConditions.Values; } }

		public override object Create(ActorInitializer init) { return new CarrierMaster(init, this); }
	}

	public class CarrierMaster : BaseSpawnerMaster, IPips, ITick, INotifyAttack, INotifyBecomingIdle
	{
		class CarrierSlaveEntry : BaseSpawnerSlaveEntry
		{
			public int RearmTicks = 0;
			public bool IsLaunched = false;
			public new CarrierSlave SpawnerSlave;
		}

		readonly Dictionary<string, Stack<int>> spawnContainTokens = new Dictionary<string, Stack<int>>();

		public new CarrierMasterInfo Info { get; private set; }

		CarrierSlaveEntry[] slaveEntries;
		ConditionManager conditionManager;

		Stack<int> loadedTokens = new Stack<int>();

		int respawnTicks = 0;

		public CarrierMaster(ActorInitializer init, CarrierMasterInfo info) : base(init, info)
		{
			Info = info;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			conditionManager = self.Trait<ConditionManager>();
		}

		public override BaseSpawnerSlaveEntry[] CreateSlaveEntries(BaseSpawnerMasterInfo info)
		{
			slaveEntries = new CarrierSlaveEntry[info.Actors.Length]; // For this class to use

			for (int i = 0; i < slaveEntries.Length; i++)
				slaveEntries[i] = new CarrierSlaveEntry();

			return slaveEntries; // For the base class to use
		}

		public override void InitializeSlaveEntry(Actor slave, BaseSpawnerSlaveEntry entry)
		{
			var carrierSlaveEntry = entry as CarrierSlaveEntry;
			base.InitializeSlaveEntry(slave, carrierSlaveEntry);

			carrierSlaveEntry.RearmTicks = 0;
			carrierSlaveEntry.IsLaunched = false;
			carrierSlaveEntry.SpawnerSlave = slave.Trait<CarrierSlave>();
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

		// The rate of fire of the dummy weapon determines the launch cycle as each shot
		// invokes Attacking()
		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (IsTraitDisabled)
				return;

			if (a.Info.Name != Info.SpawnerArmamentName)
				return;

			// Issue retarget order for already launched ones
			foreach (var slave in slaveEntries)
				if (slave.IsLaunched && slave.IsValid)
					slave.SpawnerSlave.Attack(slave.Actor, target);

			var carrierSlaveEntry = GetLaunchable();
			if (carrierSlaveEntry == null)
				return;

			carrierSlaveEntry.IsLaunched = true; // mark as launched

			// Launching condition is timed, so not saving the token.
			if (Info.LaunchingCondition != null)
				conditionManager.GrantCondition(self, Info.LaunchingCondition); // TODO removed Info.LaunchingTicks

			SpawnIntoWorld(self, carrierSlaveEntry.Actor, self.CenterPosition);

			// Queue attack order, too.
			self.World.AddFrameEndTask(w =>
			{
				// The actor might had been trying to do something before entering the carrier.
				// Cancel whatever it was trying to do.
				carrierSlaveEntry.SpawnerSlave.Stop(carrierSlaveEntry.Actor);

				carrierSlaveEntry.SpawnerSlave.Attack(carrierSlaveEntry.Actor, target);
			});
		}

		public virtual void OnBecomingIdle(Actor self)
		{
			Recall(self);
		}

		void Recall(Actor self)
		{
			// Tell launched slaves to come back and enter me.
			foreach (var carrierSlaveEntry in slaveEntries)
				if (carrierSlaveEntry.IsLaunched && carrierSlaveEntry.IsValid)
					carrierSlaveEntry.SpawnerSlave.EnterSpawner(carrierSlaveEntry.Actor);
		}

		public override void OnSlaveKilled(Actor self, Actor slave)
		{
			// Set clock so that regen happens.
			if (respawnTicks <= 0) // Don't interrupt an already running timer!
				respawnTicks = Info.RespawnTicks;
		}

		CarrierSlaveEntry GetLaunchable()
		{
			foreach (var carrierSlaveEntry in slaveEntries)
				if (carrierSlaveEntry.RearmTicks <= 0 && !carrierSlaveEntry.IsLaunched && carrierSlaveEntry.IsValid)
					return carrierSlaveEntry;

			return null;
		}

		public IEnumerable<PipType> GetPips(Actor self)
		{
			if (IsTraitDisabled)
				yield break;

			int inside = 0;
			foreach (var carrierSlaveEntry in slaveEntries)
				if (carrierSlaveEntry.IsValid && !carrierSlaveEntry.IsLaunched)
					inside++;

			for (var i = 0; i < Info.Actors.Length; i++)
			{
				if (i < inside)
					yield return Info.PipType;
				else
					yield return PipType.Transparent;
			}
		}

		public void PickupSlave(Actor self, Actor a)
		{
			CarrierSlaveEntry slaveEntry = null;
			foreach (var carrierSlaveEntry in slaveEntries)
				if (carrierSlaveEntry.Actor == a)
				{
					slaveEntry = carrierSlaveEntry;
					break;
				}

			if (slaveEntry == null)
				throw new InvalidOperationException("An actor that isn't my slave entered me?");

			slaveEntry.IsLaunched = false;

			// setup rearm
			slaveEntry.RearmTicks = Info.RearmTicks;

			string spawnContainCondition;
			if (conditionManager != null && Info.SpawnContainConditions.TryGetValue(a.Info.Name, out spawnContainCondition))
				spawnContainTokens.GetOrAdd(a.Info.Name).Push(conditionManager.GrantCondition(self, spawnContainCondition));

			if (conditionManager != null && !string.IsNullOrEmpty(Info.LoadedCondition))
				loadedTokens.Push(conditionManager.GrantCondition(self, Info.LoadedCondition));
		}

		public void Tick(Actor self)
		{
			if (respawnTicks > 0)
			{
				respawnTicks--;

				// Time to respawn someting.
				if (respawnTicks <= 0)
				{
					Replenish(self, slaveEntries);

					// If there's something left to spawn, restart the timer.
					if (SelectEntryToSpawn(slaveEntries) != null)
						respawnTicks = Info.RespawnTicks;
				}
			}

			// Rearm
			foreach (var carrierSlaveEntry in slaveEntries)
			{
				if (carrierSlaveEntry.RearmTicks > 0)
					carrierSlaveEntry.RearmTicks--;
			}
		}
	}
}