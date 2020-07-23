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
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	public class GrantTimedConditionOnDeployInfo : PausableConditionalTraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition granted during deploying.")]
		public readonly string DeployingCondition = null;

		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition granted after deploying.")]
		public readonly string DeployedCondition = null;

		[Desc("Cooldown in ticks until the unit can deploy.")]
		public readonly int CooldownTicks;

		[Desc("The deployed state's length in ticks.")]
		public readonly int DeployedTicks;

		[Desc("Cursor to display when able to (un)deploy the actor.")]
		public readonly string DeployCursor = "deploy";

		[Desc("Cursor to display when unable to (un)deploy the actor.")]
		public readonly string DeployBlockedCursor = "deploy-blocked";

		[SequenceReference]
		[Desc("Animation to play for deploying.")]
		public readonly string DeployAnimation = null;

		[SequenceReference]
		[Desc("Animation to play for undeploying.")]
		public readonly string UndeployAnimation = null;

		[Desc("Apply (un)deploy animations to sprite bodies with these names.")]
		public readonly string[] BodyNames = { "body" };

		[Desc("Facing that the actor must face before deploying. Set to -1 to deploy regardless of facing.")]
		public readonly int Facing = -1;

		[Desc("Sound to play when deploying.")]
		public readonly string DeploySound = null;

		[Desc("Sound to play when undeploying.")]
		public readonly string UndeploySound = null;

		public readonly bool StartsFullyCharged = false;

		[VoiceReference]
		public readonly string Voice = "Action";

		public readonly bool ShowSelectionBar = true;
		public readonly Color ChargingColor = Color.DarkRed;
		public readonly Color DischargingColor = Color.DarkMagenta;

		public override object Create(ActorInitializer init) { return new GrantTimedConditionOnDeploy(init, this); }
	}

	public enum TimedDeployState { Charging, Ready, Active, Deploying, Undeploying }

	public class GrantTimedConditionOnDeploy : PausableConditionalTrait<GrantTimedConditionOnDeployInfo>,
		IResolveOrder, IIssueOrder, INotifyCreated, ISelectionBar, IOrderVoice, ISync, ITick, IIssueDeployOrder
	{
		readonly Actor self;
		readonly bool canTurn;
		int deployedToken = ConditionManager.InvalidConditionToken;
		int deployingToken = ConditionManager.InvalidConditionToken;

		WithSpriteBody[] wsbs;
		ConditionManager manager;
		TimedDeployState deployState;

		[Sync]
		int ticks;

		public GrantTimedConditionOnDeploy(ActorInitializer init, GrantTimedConditionOnDeployInfo info)
			: base(info)
		{
			self = init.Self;
			canTurn = self.Info.HasTraitInfo<IFacingInfo>();
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			manager = self.Trait<ConditionManager>();
			wsbs = self.TraitsImplementing<WithSpriteBody>().Where(w => Info.BodyNames.Contains(w.Info.Name)).ToArray();

			if (Info.StartsFullyCharged)
			{
				ticks = Info.DeployedTicks;
				deployState = TimedDeployState.Ready;
			}
			else
			{
				ticks = Info.CooldownTicks;
				deployState = TimedDeployState.Charging;
			}
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			return new Order("GrantTimedConditionOnDeploy", self, queued);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) { return !IsTraitPaused && !IsTraitDisabled; }

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (!IsTraitDisabled)
					yield return new DeployOrderTargeter("GrantTimedConditionOnDeploy", 5,
						() => IsCursorBlocked() ? Info.DeployBlockedCursor : Info.DeployCursor);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "GrantTimedConditionOnDeploy")
				return new Order(order.OrderID, self, queued);

			return null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "GrantTimedConditionOnDeploy" || deployState != TimedDeployState.Ready)
				return;

			if (!order.Queued)
				self.CancelActivity();

			// Turn to the required facing.
			if (Info.Facing != -1 && canTurn)
				self.QueueActivity(new Turn(self, Info.Facing));

			self.QueueActivity(new CallFunc(Deploy));
		}

		bool IsCursorBlocked()
		{
			return deployState != TimedDeployState.Ready;
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "GrantTimedConditionOnDeploy" && deployState == TimedDeployState.Ready ? Info.Voice : null;
		}

		void Deploy()
		{
			// Something went wrong, most likely due to deploy order spam and the fact that this is a delayed action.
			if (deployState != TimedDeployState.Ready)
				return;

			deployState = TimedDeployState.Deploying;

			if (!string.IsNullOrEmpty(Info.DeploySound))
				Game.Sound.Play(SoundType.World, Info.DeploySound, self.CenterPosition);

			var wsb = wsbs.FirstEnabledTraitOrDefault();

			// If there is no animation to play just grant the upgrades that are used while deployed.
			// Alternatively, play the deploy animation and then grant the upgrades.
			if (string.IsNullOrEmpty(Info.DeployAnimation) || wsb == null)
				OnDeployCompleted();
			else
			{
				if (manager != null && !string.IsNullOrEmpty(Info.DeployingCondition) && deployingToken == ConditionManager.InvalidConditionToken)
					deployingToken = manager.GrantCondition(self, Info.DeployingCondition);
				wsb.PlayCustomAnimation(self, Info.DeployAnimation, OnDeployCompleted);
			}
		}

		void OnDeployCompleted()
		{
			if (manager != null && !string.IsNullOrEmpty(Info.DeployedCondition) && deployedToken == ConditionManager.InvalidConditionToken)
				deployedToken = manager.GrantCondition(self, Info.DeployedCondition);

			if (deployingToken != ConditionManager.InvalidConditionToken)
				deployingToken = manager.RevokeCondition(self, deployingToken);

			deployState = TimedDeployState.Active;
		}

		void RevokeDeploy()
		{
			deployState = TimedDeployState.Undeploying;

			if (!string.IsNullOrEmpty(Info.UndeploySound))
				Game.Sound.Play(SoundType.World, Info.UndeploySound, self.CenterPosition);

			var wsb = wsbs.FirstEnabledTraitOrDefault();

			if (string.IsNullOrEmpty(Info.UndeployAnimation) || wsb == null)
				OnUndeployCompleted();
			else
			{
				if (manager != null && !string.IsNullOrEmpty(Info.DeployingCondition) && deployingToken == ConditionManager.InvalidConditionToken)
					deployingToken = manager.GrantCondition(self, Info.DeployingCondition);
				wsb.PlayCustomAnimation(self, Info.UndeployAnimation, OnUndeployCompleted);
			}
		}

		void OnUndeployCompleted()
		{
			if (deployedToken != ConditionManager.InvalidConditionToken)
				deployedToken = manager.RevokeCondition(self, deployedToken);

			if (deployingToken != ConditionManager.InvalidConditionToken)
				deployingToken = manager.RevokeCondition(self, deployingToken);

			deployState = TimedDeployState.Charging;
			ticks = Info.CooldownTicks;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitPaused || IsTraitDisabled)
				return;

			if (deployState == TimedDeployState.Ready || deployState == TimedDeployState.Deploying || deployState == TimedDeployState.Undeploying)
				return;

			if (--ticks < 0)
			{
				if (deployState == TimedDeployState.Charging)
				{
					ticks = Info.DeployedTicks;
					deployState = TimedDeployState.Ready;
				}
				else
				{
					RevokeDeploy();
				}
			}
		}

		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled || !Info.ShowSelectionBar || deployState == TimedDeployState.Undeploying)
				return 0f;

			if (deployState == TimedDeployState.Deploying || deployState == TimedDeployState.Ready)
				return 1f;

			return deployState == TimedDeployState.Charging
				? (float)(Info.CooldownTicks - ticks) / Info.CooldownTicks
				: (float)ticks / Info.DeployedTicks;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return !IsTraitDisabled && Info.ShowSelectionBar; } }

		Color ISelectionBar.GetColor() { return deployState == TimedDeployState.Charging ? Info.ChargingColor : Info.DischargingColor; }
	}
}
