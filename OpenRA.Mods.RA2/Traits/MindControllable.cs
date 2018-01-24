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
	[Desc("This actor can be mind controlled by other actors.")]
	public class MindControllableInfo : ConditionalTraitInfo
	{
		[Desc("Condition to grant when under mind control")]
		[GrantedConditionReference]
		public readonly string Condition = null;

		[Desc("The sound played when the unit is unmind controlled.")]
		public readonly string[] UnMindControlSounds = { };

		public override object Create(ActorInitializer init) { return new MindControllable(init.Self, this); }
	}

	class MindControllable : ConditionalTrait<MindControllableInfo>, INotifyKilled, INotifyActorDisposing, INotifyCreated
	{
		readonly MindControllableInfo info;

		Actor master;
		Player creatorOwner;

		ConditionManager conditionManager;
		int token = ConditionManager.InvalidConditionToken;

		public Actor Master { get { return master; } }

		public MindControllable(Actor self, MindControllableInfo info)
			: base(info)
		{
			this.info = info;
		}

		protected override void Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		public void LinkMaster(Actor self, Actor master)
		{
			self.CancelActivity();

			if (this.master == null)
				creatorOwner = self.Owner;

			var oldOwner = self.Owner;
			self.ChangeOwner(master.Owner);

			UnlinkMaster(self, this.master);
			this.master = master;

			if (conditionManager != null && token == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(Info.Condition))
				token = conditionManager.GrantCondition(self, Info.Condition);

			if (master.Owner == creatorOwner)
				UnlinkMaster(self, master);
		}

		public void UnlinkMaster(Actor self, Actor master)
		{
			if (master == null)
				return;

			master.Trait<MindController>().UnlinkSlave(master, self);

			this.master = null;

			if (conditionManager != null && token != ConditionManager.InvalidConditionToken)
				token = conditionManager.RevokeCondition(self, token);
		}

		public void UnMindControl(Actor self, Player oldOwner)
		{
			self.CancelActivity();

			if (creatorOwner.WinState == WinState.Lost)
				self.ChangeOwner(self.World.WorldActor.Owner);
			else
				self.ChangeOwner(creatorOwner);

			UnlinkMaster(self, master);

			if (info.UnMindControlSounds.Any())
				Game.Sound.Play(SoundType.World, info.UnMindControlSounds.Random(self.World.SharedRandom), self.CenterPosition);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			UnlinkMaster(self, master);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			UnlinkMaster(self, master);
		}
	}
}