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

using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;
using OpenRA.Graphics;
using System;

namespace OpenRA.Mods.RA2.Widgets.Logic
{

	public class PowerMeterWidget : Widget
	{
		Widget sidebarProduction;

		int lastMeterCheck;

		int barheight = 0;

		bool bypassanimation = false;

		int warningflash = 0;

		int lasttotalpowerdisplay;

		protected readonly World world;


		[Desc("The name of the Container Widget to tie the Y axis to")]
		[FieldLoader.Require]
		public readonly string MeterAlongside = "";

		[Desc("The name of the Container with the items to get the height from")]
		[FieldLoader.Require]
		public readonly string ParentContainer = "";

		[Desc("Height of each meter bar")]
		[FieldLoader.Require]
		public readonly int MeterHeight = 3;

		[Desc("How many units of power each bar represents")]
		[FieldLoader.Require]
		public readonly int PowerUnitsPerBar = 25;

		[Desc("How many Ticks to wait before animating the bar")]
		[FieldLoader.Require]
		public readonly int TickWait = 4;

		[Desc("Blank Image for the meter bar")]
		[FieldLoader.Require]
		public readonly string NoPowerImage = "";

		[Desc("When you have access power to use")]
		[FieldLoader.Require]
		public readonly string AvailablePowerImage = "";

		[Desc("Used power image")]
		[FieldLoader.Require]
		public readonly string UsedPowerImage = "";

		[Desc("Too much poer used meter image")]
		[FieldLoader.Require]
		public readonly string OverUsedPowerImage = "";

		[Desc("How many Ticks to wait before animating the bar")]
		[FieldLoader.Require]
		public readonly string FlashPowerImage = "";

		[Desc("The collection of images to get the meter images from")]
		[FieldLoader.Require]
		public readonly string ImageCollection = "";

		[ObjectCreator.UseCtor]
		public PowerMeterWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
		}

		public void CalculateMeterBarDimensions()
		{
			// height of power meter in pixels
			var newbarheight = 0;
			foreach (var child in sidebarProduction.Children)
			{
				if (child.Id == MeterAlongside)
					newbarheight += child.Bounds.Height;
			}

			if (newbarheight != barheight)
			{
				barheight = newbarheight;

				// don't animate the meter after changing sidebars
				bypassanimation = true;
			}
		}

		public Widget GetSidebar()
		{
			if (Parent == null)
				return null;

			if (sidebarProduction != null)
				return sidebarProduction;

			sidebarProduction = Parent.GetOrNull(ParentContainer);
			return sidebarProduction;
		}

		public void CheckBarNumber()
		{
			var meterdistance = MeterHeight;
			var numberofbars = Decimal.Floor(barheight / meterdistance);

			if (Children.Count == numberofbars)
				return;

			Children.Clear();

			// create a list of new health bars
			for (int i = 0; i < numberofbars; i++)
			{
				var newpower = new ImageWidget();
				newpower.ImageCollection = ImageCollection;
				newpower.ImageName = NoPowerImage;

				// you could add AddFactionSuffixLogic here
				newpower.Bounds.Y = -(i * meterdistance) + barheight + Bounds.Y;
				newpower.Bounds.X = Bounds.X;
				newpower.GetImageName = () => newpower.ImageName;
				Children.Add(newpower);
			}
		}

		public void CheckFlash(PowerManager powerManager, int totalpowerdisplay)
		{
			var startwarningflash = false;

			if (powerManager.PowerState == PowerState.Low)
				startwarningflash = true;

			if (powerManager.PowerState == PowerState.Critical)
				startwarningflash = true;

			if (lasttotalpowerdisplay != totalpowerdisplay)
			{
				startwarningflash = true;
				lasttotalpowerdisplay = totalpowerdisplay;
			}

			if (startwarningflash && warningflash <= 0)
				warningflash = 10;
		}

		public override void Tick()
		{
			if (GetSidebar() == null)
				return;

			CalculateMeterBarDimensions();
			CheckBarNumber();

			// if just changed power level or low power, flash the last bar meter
			lastMeterCheck++;
			if (lastMeterCheck < TickWait)
				return;
			lastMeterCheck = 0;

			// number of power units represent each bar
			var stepsize = PowerUnitsPerBar;

			var powerManager = world.LocalPlayer.PlayerActor.Trait<PowerManager>();
			var totalpowerdisplay = Math.Max(powerManager.PowerProvided, powerManager.PowerDrained);

			var totalpowerstep = Decimal.Floor(totalpowerdisplay / stepsize);
			var powerusedstep = Decimal.Floor(powerManager.PowerDrained / stepsize);
			var poweravailabletep = Decimal.Floor(powerManager.PowerProvided / stepsize);

			// maxed out the bar. instead we'll display a percent
			if (totalpowerstep > Children.Count)
			{
				var powerfraction = (float)Children.Count / (float)totalpowerstep;
				totalpowerdisplay = (int)((float)totalpowerdisplay * powerfraction);
				totalpowerstep = (int)((float)totalpowerstep * powerfraction);
				powerusedstep = (int)((float)powerusedstep * powerfraction);
				poweravailabletep = (int)((float)poweravailabletep * powerfraction);
			}

			// should i start flashing the top bar?
			CheckFlash(powerManager, totalpowerdisplay);

			// if maxed out bar size, work on percents
			for (int i = 0; i < Children.Count; i++)
			{
				if (Children[i].GetType().Name != "ImageWidget")
					continue;

				var myimage = (ImageWidget)Children[i];

				// unpowered amounts 
				if (i > totalpowerstep || totalpowerstep == 0)
				{
					myimage.ImageName = NoPowerImage;
					continue;
				}
				var targeticon = AvailablePowerImage;

				if (i < powerusedstep)
					targeticon = UsedPowerImage;

				if (i > poweravailabletep)
					targeticon = OverUsedPowerImage;

				if (i == totalpowerstep && powerManager.PowerState == PowerState.Low)
					targeticon = OverUsedPowerImage;

				// flash the top bar if something is wrong
				if (i == totalpowerstep)
				{
					if (warningflash % 2 != 0)
						targeticon = FlashPowerImage;
					if (warningflash > 0)
						warningflash--;
				}

				// we exit if updating a bar meter. This gives a nice animation effect
				if (myimage.ImageName != targeticon)
				{
					myimage.ImageName = targeticon;
					if (!bypassanimation)
						return;
				}
			}

			bypassanimation = false;
			// end Tick
		}
	}

}

