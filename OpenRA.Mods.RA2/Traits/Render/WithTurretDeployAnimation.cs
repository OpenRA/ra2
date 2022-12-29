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

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits.Render
{
	public class WithTurretDeployAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteTurretInfo>
	{
		[Desc("Turret name")]
		public readonly string Turret = "primary";

		public readonly string DeploySequence = "deploy";
		public readonly string UndeploySequence = "undeploy";

		public override object Create(ActorInitializer init)
		{
			return new WithTurretDeployAnimation(init.Self, this);
		}
	}

	public class WithTurretDeployAnimation : ConditionalTrait<WithTurretDeployAnimationInfo>, INotifyDeployTriggered
	{
		readonly WithSpriteTurret wst;

		INotifyDeployComplete[] notifiers;

		public WithTurretDeployAnimation(Actor self, WithTurretDeployAnimationInfo info)
			: base(info)
		{
			wst = self.TraitsImplementing<WithSpriteTurret>()
				.Single(st => st.Info.Turret == info.Turret);
		}

		protected override void Created(Actor self)
		{
			notifiers = self.TraitsImplementing<INotifyDeployComplete>().ToArray();
			base.Created(self);
		}

		void INotifyDeployTriggered.Deploy(Actor self, bool skipMakeAnim)
		{
			wst.PlayCustomAnimation(self, Info.DeploySequence, () =>
			{
				foreach (var n in notifiers)
					n.FinishedDeploy(self);
			});
		}

		void INotifyDeployTriggered.Undeploy(Actor self, bool skipMakeAnim)
		{
			wst.PlayCustomAnimation(self, Info.UndeploySequence, () =>
			{
				foreach (var n in notifiers)
					n.FinishedUndeploy(self);
			});
		}
	}
}
