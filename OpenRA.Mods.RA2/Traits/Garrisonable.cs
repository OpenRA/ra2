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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.RA2.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("This actor can store Garrisoner actors.")]
	public class GarrisonableInfo : PausableConditionalTraitInfo, ITraitInfo, Requires<IOccupySpaceInfo>
	{
		[Desc("The maximum sum of Garrisoner.Weight that this actor can support.")]
		public readonly int MaxWeight = 0;

		[Desc("Number of pips to display when this actor is selected.")]
		public readonly int PipCount = 0;

		[Desc("`Garrisoner.GarrisonType`s that can be loaded into this actor.")]
		public readonly HashSet<string> Types = new HashSet<string>();

		[Desc("A list of actor types that are initially spawned into this actor.")]
		public readonly string[] InitialUnits = { };

		[Desc("When this actor is sold should all of its garrisoners be unloaded?")]
		public readonly bool EjectOnSell = true;

		[Desc("When this actor dies should all of its garrisoners be unloaded?")]
		public readonly bool EjectOnDeath = false;

		[Desc("Terrain types that this actor is allowed to eject actors onto. Leave empty for all terrain types.")]
		public readonly HashSet<string> UnloadTerrainTypes = new HashSet<string>();

		[VoiceReference]
		[Desc("Voice to play when ordered to unload the garrisoners.")]
		public readonly string UnloadVoice = "Action";

		[Desc("Radius to search for a load/unload location if the ordered cell is blocked.")]
		public readonly WDist LoadRange = WDist.FromCells(5);

		[Desc("Which direction the garrisoner will face (relative to the transport) when unloading.")]
		public readonly int GarrisonerFacing = 128;

		[Desc("Delay (in ticks) before continuing after loading a passenger.")]
		public readonly int AfterLoadDelay = 8;

		[Desc("Delay (in ticks) before unloading the first passenger.")]
		public readonly int BeforeUnloadDelay = 8;

		[Desc("Delay (in ticks) before continuing after unloading a passenger.")]
		public readonly int AfterUnloadDelay = 25;

		[Desc("Cursor to display when able to unload the garrisoners.")]
		public readonly string UnloadCursor = "deploy";

		[Desc("Cursor to display when unable to unload the garrisoners.")]
		public readonly string UnloadBlockedCursor = "deploy-blocked";

		[GrantedConditionReference]
		[Desc("The condition to grant to self while waiting for garrisonable to load.")]
		public readonly string LoadingCondition = null;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while garrisoners are loaded.",
			"Condition can stack with multiple garrisoners.")]
		public readonly string LoadedCondition = null;

		[Desc("Conditions to grant when specified actors are loaded inside the transport.",
			"A dictionary of [actor id]: [condition].")]
		public readonly Dictionary<string, string> GarrisonerConditions = new Dictionary<string, string>();

		[GrantedConditionReference]
		public IEnumerable<string> LinterGarrisonerConditions { get { return GarrisonerConditions.Values; } }

		public override object Create(ActorInitializer init) { return new Garrisonable(init, this); }
	}

	public class Garrisonable : PausableConditionalTrait<GarrisonableInfo>, IPips, IIssueOrder, IResolveOrder, IOrderVoice, INotifyCreated, INotifyKilled,
		INotifyOwnerChanged, INotifySold, INotifyActorDisposing, IIssueDeployOrder,
		ITransformActorInitModifier
	{
		readonly Actor self;
		readonly List<Actor> garrisonable = new List<Actor>();
		readonly HashSet<Actor> reserves = new HashSet<Actor>();
		readonly Dictionary<string, Stack<int>> garrisonerTokens = new Dictionary<string, Stack<int>>();
		readonly Lazy<IFacing> facing;
		readonly bool checkTerrainType;

		int totalWeight = 0;
		int reservedWeight = 0;
		Aircraft aircraft;
		ConditionManager conditionManager;
		int loadingToken = ConditionManager.InvalidConditionToken;
		Stack<int> loadedTokens = new Stack<int>();
		bool takeOffAfterLoad;
		bool initialised;

		readonly CachedTransform<CPos, IEnumerable<CPos>> currentAdjacentCells;
		public IEnumerable<CPos> CurrentAdjacentCells
		{
			get { return currentAdjacentCells.Update(self.Location); }
		}

		public IEnumerable<Actor> Garrisoners { get { return garrisonable; } }
		public int GarrisonerCount { get { return garrisonable.Count; } }

		enum State { Free, Locked }
		State state = State.Free;

		public Garrisonable(ActorInitializer init, GarrisonableInfo info)
			: base(info)
		{
			self = init.Self;
			checkTerrainType = info.UnloadTerrainTypes.Count > 0;

			currentAdjacentCells = new CachedTransform<CPos, IEnumerable<CPos>>(loc =>
			{
				return Util.AdjacentCells(self.World, Target.FromActor(self)).Where(c => loc != c);
			});

			if (init.Contains<RuntimeGarrisonInit>())
			{
				garrisonable = new List<Actor>(init.Get<RuntimeCargoInit, Actor[]>());
				totalWeight = garrisonable.Sum(c => GetWeight(c));
			}
			else if (init.Contains<GarrisonInit>())
			{
				foreach (var u in init.Get<GarrisonInit, string[]>())
				{
					var unit = self.World.CreateActor(false, u.ToLowerInvariant(),
						new TypeDictionary { new OwnerInit(self.Owner) });

					garrisonable.Add(unit);
				}

				totalWeight = garrisonable.Sum(c => GetWeight(c));
			}
			else
			{
				foreach (var u in info.InitialUnits)
				{
					var unit = self.World.CreateActor(false, u.ToLowerInvariant(),
						new TypeDictionary { new OwnerInit(self.Owner) });

					garrisonable.Add(unit);
				}

				totalWeight = garrisonable.Sum(c => GetWeight(c));
			}

			facing = Exts.Lazy(self.TraitOrDefault<IFacing>);
		}

		void INotifyCreated.Created(Actor self)
		{
			aircraft = self.TraitOrDefault<Aircraft>();
			conditionManager = self.TraitOrDefault<ConditionManager>();

			if (conditionManager != null && garrisonable.Any())
			{
				foreach (var c in garrisonable)
				{
					string garrisonerCondition;
					if (Info.GarrisonerConditions.TryGetValue(c.Info.Name, out garrisonerCondition))
						garrisonerTokens.GetOrAdd(c.Info.Name).Push(conditionManager.GrantCondition(self, garrisonerCondition));
				}

				if (!string.IsNullOrEmpty(Info.LoadedCondition))
					loadedTokens.Push(conditionManager.GrantCondition(self, Info.LoadedCondition));
			}

			// Defer notifications until we are certain all traits on the transport are initialised
			self.World.AddFrameEndTask(w =>
			{
				foreach (var c in garrisonable)
				{
					c.Trait<Passenger>().Transport = self;

					foreach (var nec in c.TraitsImplementing<INotifyEnteredGarrison>())
						nec.OnEnteredGarrison(c, self);

					foreach (var npe in self.TraitsImplementing<INotifyGarrisonerEntered>())
						npe.OnGarrisonerEntered(self, c);
				}

				initialised = true;
			});
		}

		static int GetWeight(Actor a) { return a.Info.TraitInfo<GarrisonerInfo>().Weight; }

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new DeployOrderTargeter("Unload", 10,
			  () => CanUnload() ? Info.UnloadCursor : Info.UnloadBlockedCursor);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "Unload")
				return new Order(order.OrderID, self, queued);

			return null;
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			return new Order("Unload", self, queued);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self) { return true; }

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Unload")
			{
				if (!order.Queued && !CanUnload())
					return;

				self.QueueActivity(new UnloadGarrison(self, Info.LoadRange));
			}
		}

		public bool CanUnload(bool check = false)
		{
			if (checkTerrainType)
			{
				var terrainType = self.World.Map.GetTerrainInfo(self.Location).Type;

				if (!Info.UnloadTerrainTypes.Contains(terrainType))
					return false;
			}

			return !IsEmpty(self) && (aircraft == null || aircraft.CanLand(self.Location, blockedByMobile: false))
				&& CurrentAdjacentCells != null && CurrentAdjacentCells.Any(c => Garrisoners.Any(p => !p.IsDead && p.Trait<IPositionable>().CanEnterCell(c, null, check)));
		}

		public bool CanLoad(Actor self, Actor a)
		{
			return reserves.Contains(a) || HasSpace(GetWeight(a));
		}

		internal bool ReserveSpace(Actor a)
		{
			if (reserves.Contains(a))
				return true;

			var w = GetWeight(a);
			if (!HasSpace(w))
				return false;

			if (conditionManager != null && loadingToken == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(Info.LoadingCondition))
				loadingToken = conditionManager.GrantCondition(self, Info.LoadingCondition);

			reserves.Add(a);
			reservedWeight += w;
			LockForPickup(self);

			return true;
		}

		internal void UnreserveSpace(Actor a)
		{
			if (!reserves.Contains(a) || self.IsDead)
				return;

			reservedWeight -= GetWeight(a);
			reserves.Remove(a);
			ReleaseLock(self);

			if (loadingToken != ConditionManager.InvalidConditionToken)
				loadingToken = conditionManager.RevokeCondition(self, loadingToken);
		}

		// Prepare for transport pickup
		void LockForPickup(Actor self)
		{
			if (state == State.Locked)
				return;

			state = State.Locked;

			self.CancelActivity();

			var air = self.TraitOrDefault<Aircraft>();
			if (air != null && !air.AtLandAltitude)
			{
				takeOffAfterLoad = true;
				self.QueueActivity(new Land(self));
			}

			self.QueueActivity(new WaitFor(() => state != State.Locked, false));
		}

		void ReleaseLock(Actor self)
		{
			if (reservedWeight != 0)
				return;

			state = State.Free;

			self.QueueActivity(new Wait(Info.AfterLoadDelay, false));
			if (takeOffAfterLoad)
				self.QueueActivity(new TakeOff(self));

			takeOffAfterLoad = false;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != "Unload" || IsEmpty(self) || !self.HasVoice(Info.UnloadVoice))
				return null;

			return Info.UnloadVoice;
		}

		public bool HasSpace(int weight) { return totalWeight + reservedWeight + weight <= Info.MaxWeight; }
		public bool IsEmpty(Actor self) { return garrisonable.Count == 0; }

		public Actor Peek(Actor self) { return garrisonable.Last(); }

		public Actor Unload(Actor self, Actor passenger = null)
		{
			passenger = passenger ?? garrisonable.Last();
			if (!garrisonable.Remove(passenger))
				throw new ArgumentException("Attempted to ungarrison an actor that is not a garrisoner.");

			totalWeight -= GetWeight(passenger);

			SetGarrisonerFacing(passenger);

			foreach (var npe in self.TraitsImplementing<INotifyGarrisonerExited>())
				npe.OnGarrisonerExited(self, passenger);

			foreach (var nec in passenger.TraitsImplementing<INotifyExitedGarrison>())
				nec.OnExitedGarrison(passenger, self);

			var p = passenger.Trait<Garrisoner>();
			p.Transport = null;

			Stack<int> garrisonerToken;
			if (garrisonerTokens.TryGetValue(passenger.Info.Name, out garrisonerToken) && garrisonerToken.Any())
				conditionManager.RevokeCondition(self, garrisonerToken.Pop());

			if (loadedTokens.Any())
				conditionManager.RevokeCondition(self, loadedTokens.Pop());

			return passenger;
		}

		void SetGarrisonerFacing(Actor garrisoner)
		{
			if (facing.Value == null)
				return;

			var garrisonerFacing = garrisoner.TraitOrDefault<IFacing>();
			if (garrisonerFacing != null)
				garrisonerFacing.Facing = facing.Value.Facing + Info.GarrisonerFacing;

			foreach (var t in garrisoner.TraitsImplementing<Turreted>())
				t.TurretFacing = facing.Value.Facing + Info.GarrisonerFacing;
		}

		public int DamageVersus(Actor victim, Dictionary<string, int> versus)
		{
			// If no Versus values are defined, DamageVersus would return 100 anyway, so we might as well do that early.
			if (versus.Count == 0)
				return 100;

			var armor = victim.TraitsImplementing<Armor>()
				.Where(a => !a.IsTraitDisabled && a.Info.Type != null && versus.ContainsKey(a.Info.Type))
				.Select(a => versus[a.Info.Type]);

			return Util.ApplyPercentageModifiers(100, armor);
		}

		public void DamagePassengers(int damage, Actor attacker, int amount, Dictionary<string, int> versus, BitSet<DamageType> damageTypes, IEnumerable<int> damageModifiers)
		{
			var passengersToDamage = amount > 0 && amount < garrisonable.Count() ? garrisonable.Shuffle(self.World.SharedRandom).Take(amount) : garrisonable;
			foreach (var passenger in passengersToDamage)
			{
				var d = Util.ApplyPercentageModifiers(damage, damageModifiers.Append(DamageVersus(passenger, versus)));
				passenger.InflictDamage(attacker, new Damage(d, damageTypes));
			}
		}

		public IEnumerable<PipType> GetPips(Actor self)
		{
			var numPips = Info.PipCount;

			for (var i = 0; i < numPips; i++)
				yield return GetPipAt(i);
		}

		PipType GetPipAt(int i)
		{
			var n = i * Info.MaxWeight / Info.PipCount;

			foreach (var c in garrisonable)
			{
				var pi = c.Info.TraitInfo<GarrisonerInfo>();
				if (n < pi.Weight)
					return pi.PipType;
				else
					n -= pi.Weight;
			}

			return PipType.Transparent;
		}

		public void Load(Actor self, Actor a)
		{
			garrisonable.Add(a);
			var w = GetWeight(a);
			totalWeight += w;
			if (reserves.Contains(a))
			{
				reservedWeight -= w;
				reserves.Remove(a);
				ReleaseLock(self);

				if (loadingToken != ConditionManager.InvalidConditionToken)
					loadingToken = conditionManager.RevokeCondition(self, loadingToken);
			}

			// Don't initialise (effectively twice) if this runs before the FrameEndTask from Created
			if (initialised)
			{
				a.Trait<Garrisoner>().Transport = self;

				foreach (var nec in a.TraitsImplementing<INotifyEnteredGarrison>())
					nec.OnEnteredGarrison(a, self);

				foreach (var npe in self.TraitsImplementing<INotifyGarrisonerEntered>())
					npe.OnGarrisonerEntered(self, a);
			}

			string garrisonerCondition;
			if (conditionManager != null && Info.GarrisonerConditions.TryGetValue(a.Info.Name, out garrisonerCondition))
				garrisonerTokens.GetOrAdd(a.Info.Name).Push(conditionManager.GrantCondition(self, garrisonerCondition));

			if (conditionManager != null && !string.IsNullOrEmpty(Info.LoadedCondition))
				loadedTokens.Push(conditionManager.GrantCondition(self, Info.LoadedCondition));
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (Info.EjectOnDeath)
				while (!IsEmpty(self) && CanUnload(true))
				{
					var garrisoner = Unload(self);
					var cp = self.CenterPosition;
					var inAir = self.World.Map.DistanceAboveTerrain(cp).Length != 0;
					var positionable = garrisoner.Trait<IPositionable>();
					positionable.SetPosition(garrisoner, self.Location);

					if (!inAir && positionable.CanEnterCell(self.Location, self, false))
					{
						self.World.AddFrameEndTask(w => w.Add(garrisoner));
						var nbms = garrisoner.TraitsImplementing<INotifyBlockingMove>();
						foreach (var nbm in nbms)
							nbm.OnNotifyBlockingMove(garrisoner, garrisoner);
					}
					else
						garrisoner.Kill(e.Attacker);
				}

			foreach (var c in garrisonable)
				c.Kill(e.Attacker);

			garrisonable.Clear();
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			foreach (var c in garrisonable)
				c.Dispose();

			garrisonable.Clear();
		}

		void INotifySold.Selling(Actor self) { }
		void INotifySold.Sold(Actor self)
		{
			if (!Info.EjectOnSell || garrisonable == null)
				return;

			while (!IsEmpty(self))
				SpawnGarrisoner(Unload(self));
		}

		void SpawnGarrisoner(Actor garrisoner)
		{
			self.World.AddFrameEndTask(w =>
			{
				w.Add(garrisoner);
				garrisoner.Trait<IPositionable>().SetPosition(garrisoner, self.Location);

				// TODO: this won't work well for >1 actor as they should move towards the next enterable (sub) cell instead
			});
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (garrisonable == null)
				return;

			foreach (var p in Garrisoners)
				p.ChangeOwner(newOwner);
		}

		void ITransformActorInitModifier.ModifyTransformActorInit(Actor self, TypeDictionary init)
		{
			init.Add(new RuntimeGarrisonInit(Garrisoners.ToArray()));
		}

		protected override void TraitDisabled(Actor self)
		{
			if (!CanUnload())
				return;

			self.CancelActivity();
			self.QueueActivity(new UnloadGarrison(self, Info.LoadRange));
		}
	}

	public class RuntimeGarrisonInit : IActorInit<Actor[]>, ISuppressInitExport
	{
		[FieldFromYamlKey]
		readonly Actor[] value = { };
		public RuntimeGarrisonInit() { }
		public RuntimeGarrisonInit(Actor[] init) { value = init; }
		public Actor[] Value(World world) { return value; }
	}

	public class GarrisonInit : IActorInit<string[]>
	{
		[FieldFromYamlKey]
		readonly string[] value = { };
		public GarrisonInit() { }
		public GarrisonInit(string[] init) { value = init; }
		public string[] Value(World world) { return value; }
	}
}
