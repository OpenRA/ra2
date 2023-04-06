#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits.Render
{
	[Desc("Creates the illusion of another actor.")]
	class WithMirageSpriteBodyInfo : WithSpriteBodyInfo, Requires<MirageInfo>
	{
		public override object Create(ActorInitializer init) { return new WithMirageSpriteBody(init, this); }
	}

	class WithMirageSpriteBody : WithSpriteBody, ITick
	{
		readonly Mirage mirage;
		readonly RenderSprites renderSprites;
		ActorInfo disguiseActor;
		Player disguisePlayer;
		string disguiseImage;

		public WithMirageSpriteBody(ActorInitializer init, WithMirageSpriteBodyInfo info)
			: base(init, info)
		{
			var self = init.Self;
			renderSprites = self.Trait<RenderSprites>();
			mirage = self.Trait<Mirage>();
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused || mirage.ActorType == null)
				return;

			if (mirage.ActorType != disguiseActor || mirage.Owner != disguisePlayer)
			{
				disguiseActor = mirage.ActorType;
				disguisePlayer = mirage.Owner;
				disguiseImage = null;

				if (disguisePlayer != null)
				{
					var renderSprites = disguiseActor.TraitInfoOrDefault<RenderSpritesInfo>();
					if (renderSprites != null)
						disguiseImage = renderSprites.GetImage(disguiseActor, disguisePlayer.Faction.InternalName);
				}

				var withSpriteBody = disguiseActor.TraitInfoOrDefault<WithSpriteBodyInfo>();
				if (withSpriteBody != null && disguiseImage != null)
				{
					DefaultAnimation.PlayRepeating(NormalizeSequence(self, withSpriteBody.Sequence));
					DefaultAnimation.ChangeImage(disguiseImage, withSpriteBody.Sequence);
				}

				renderSprites.UpdatePalette();
			}
		}
	}
}
