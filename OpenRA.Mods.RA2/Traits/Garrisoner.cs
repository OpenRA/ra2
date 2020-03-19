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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Activities;
using OpenRA.Mods.RA2.Orders;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("This actor can enter Garrisonable actors.")]
	public class GarrisonerInfo : ITraitInfo
	{
		public readonly string GarrisonType = null;
		public readonly PipType PipType = PipType.Green;
		public readonly int Weight = 1;

		[Desc("What diplomatic stances can be Garrisoned by this actor.")]
		public readonly Stance TargetStances = Stance.Ally | Stance.Neutral;

		[GrantedConditionReference]
		[Desc("The condition to grant to when this actor is loaded inside any transport.")]
		public readonly string GarrisonCondition = null;

		[Desc("Conditions to grant when this actor is loaded inside specified transport.",
			"A dictionary of [actor id]: [condition].")]
		public readonly Dictionary<string, string> GarrisonConditions = new Dictionary<string, string>();

		[GrantedConditionReference]
		public IEnumerable<string> LinterGarrisonConditions { get { return GarrisonConditions.Values; } }

		[VoiceReference]
		public readonly string Voice = "Action";

		[ConsumedConditionReference]
		[Desc("Boolean expression defining the condition under which the regular (non-force) enter cursor is disabled.")]
		public readonly BooleanExpression RequireForceMoveCondition = null;

		public object Create(ActorInitializer init) { return new Garrisoner(this); }
	}

	public class Garrisoner : INotifyCreated, IIssueOrder, IResolveOrder, IOrderVoice, INotifyRemovedFromWorld, INotifyEnteredGarrison, INotifyExitedGarrison, INotifyKilled, IObservesVariables
	{
		public readonly GarrisonerInfo Info;
		public Actor Transport;
		bool requireForceMove;

		ConditionManager conditionManager;
		int anyGarrisonToken = ConditionManager.InvalidConditionToken;
		int specificGarrisonToken = ConditionManager.InvalidConditionToken;

		public Garrisoner(GarrisonerInfo info)
		{
			Info = info;
		}

		public Garrisonable ReservedGarrison { get; private set; }

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				yield return new EnterGarrisonOrderTargeter<GarrisonableInfo>("EnterGarrison", 5, IsCorrectGarrisonType, CanEnter, Info);
			}
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "EnterGarrison")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		bool IsCorrectGarrisonType(Actor target, TargetModifiers modifiers)
		{
			if (requireForceMove && !modifiers.HasModifier(TargetModifiers.ForceMove))
				return false;

			return IsCorrectGarrisonType(target);
		}

		bool IsCorrectGarrisonType(Actor target)
		{
			var ci = target.Info.TraitInfo<GarrisonableInfo>();
			return ci.Types.Contains(Info.GarrisonType);
		}

		bool CanEnter(Garrisonable garrison)
		{
			return garrison != null && garrison.HasSpace(Info.Weight) && !garrison.IsTraitPaused && !garrison.IsTraitDisabled;
		}

		bool CanEnter(Actor target)
		{
			return CanEnter(target.TraitOrDefault<Garrisonable>());
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != "EnterGarrison")
				return null;

			if (order.Target.Type != TargetType.Actor || !CanEnter(order.Target.Actor))
				return null;

			return Info.Voice;
		}

		void INotifyEnteredGarrison.OnEnteredGarrison(Actor self, Actor garrison)
		{
			string specificGarrisonCondition;
			if (conditionManager != null)
			{
				if (anyGarrisonToken == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(Info.GarrisonCondition))
					anyGarrisonToken = conditionManager.GrantCondition(self, Info.GarrisonCondition);

				if (specificGarrisonToken == ConditionManager.InvalidConditionToken && Info.GarrisonConditions.TryGetValue(garrison.Info.Name, out specificGarrisonCondition))
					specificGarrisonToken = conditionManager.GrantCondition(self, specificGarrisonCondition);
			}

			// Allow scripted / initial actors to move from the unload point back into the cell grid on unload
			// This is handled by the RideTransport activity for player-loaded cargo
			if (self.IsIdle)
			{
				// IMove is not used anywhere else in this trait, there is no benefit to caching it from Created.
				var move = self.TraitOrDefault<IMove>();
				if (move != null)
					self.QueueActivity(move.ReturnToCell(self));
			}
		}

		void INotifyExitedGarrison.OnExitedGarrison(Actor self, Actor garrison)
		{
			if (anyGarrisonToken != ConditionManager.InvalidConditionToken)
				anyGarrisonToken = conditionManager.RevokeCondition(self, anyGarrisonToken);

			if (specificGarrisonToken != ConditionManager.InvalidConditionToken)
				specificGarrisonToken = conditionManager.RevokeCondition(self, specificGarrisonToken);
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "EnterGarrison")
				return;

			if (order.Target.Type == TargetType.Actor)
			{
				var targetActor = order.Target.Actor;
				if (!CanEnter(targetActor))
					return;

				if (!IsCorrectGarrisonType(targetActor))
					return;
			}

			self.QueueActivity(order.Queued, new EnterGarrison(self, order.Target));
			self.ShowTargetLines();
		}

		public bool Reserve(Actor self, Garrisonable garrison)
		{
			if (garrison == ReservedGarrison)
				return true;

			Unreserve(self);
			if (!garrison.ReserveSpace(self))
				return false;

			ReservedGarrison = garrison;
			return true;
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self) { Unreserve(self); }

		public void Unreserve(Actor self)
		{
			if (ReservedGarrison == null)
				return;

			ReservedGarrison.UnreserveSpace(self);
			ReservedGarrison = null;
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (Transport == null)
				return;

			// Something killed us, but it wasn't our transport blowing up. Remove us from the cargo.
			if (!Transport.IsDead)
				self.World.AddFrameEndTask(w => Transport.Trait<Garrisonable>().Unload(Transport, self));
		}

		IEnumerable<VariableObserver> IObservesVariables.GetVariableObservers()
		{
			if (Info.RequireForceMoveCondition != null)
				yield return new VariableObserver(RequireForceMoveConditionChanged, Info.RequireForceMoveCondition.Variables);
		}

		void RequireForceMoveConditionChanged(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			requireForceMove = Info.RequireForceMoveCondition.Evaluate(conditions);
		}
	}
}
