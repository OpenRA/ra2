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

using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[RequireExplicitImplementation]
	public interface INotifyEnteredGarrison { void OnEnteredGarrison(Actor self, Actor garrison); }

	[RequireExplicitImplementation]
	public interface INotifyExitedGarrison { void OnExitedGarrison(Actor self, Actor garrison); }

	[RequireExplicitImplementation]
	public interface INotifyGarrisonerEntered { void OnGarrisonerEntered(Actor self, Actor garrisoner); }

	[RequireExplicitImplementation]
	public interface INotifyGarrisonerExited { void OnGarrisonerExited(Actor self, Actor garrisoner); }
}
