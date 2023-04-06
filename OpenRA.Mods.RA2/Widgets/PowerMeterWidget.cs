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

using System;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA2.Widgets.Logic
{
	public class PowerMeterWidget : Widget
	{
		Widget sidebarProduction;
		int lastMeterCheck;
		int barHeight;
		bool bypassAnimation;
		int warningFlash;
		int lastTotalPowerDisplay;

		protected readonly World World;

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
		public PowerMeterWidget(World world)
		{
			World = world;
		}

		public void CalculateMeterBarDimensions()
		{
			// Height of power meter in pixels
			var newBarHeight = 0;
			foreach (var child in sidebarProduction.Children)
				if (child.Id == MeterAlongside)
					newBarHeight += child.Bounds.Height;

			if (newBarHeight != barHeight)
			{
				barHeight = newBarHeight;

				// Don't animate the meter after changing sidebars
				bypassAnimation = true;
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
			var meterDistance = MeterHeight;
			var numberOfBars = decimal.Floor(barHeight / meterDistance);

			if (Children.Count == numberOfBars)
				return;

			Children.Clear();

			// Create a list of new bars
			for (var i = 0; i < numberOfBars; i++)
			{
				var newPower = new ImageWidget
				{
					ImageCollection = ImageCollection,
					ImageName = NoPowerImage
				};

				// AddFactionSuffixLogic could be added here
				newPower.Bounds.Y = -(i * meterDistance) + barHeight + Bounds.Y;
				newPower.Bounds.X = Bounds.X;
				newPower.GetImageName = () => newPower.ImageName;
				Children.Add(newPower);
			}
		}

		public void CheckFlash(PowerManager powerManager, int totalPowerDisplay)
		{
			var startWarningFlash = powerManager.PowerState != PowerState.Normal;

			if (lastTotalPowerDisplay != totalPowerDisplay)
			{
				startWarningFlash = true;
				lastTotalPowerDisplay = totalPowerDisplay;
			}

			if (startWarningFlash && warningFlash <= 0)
				warningFlash = 10;
		}

		public override void Tick()
		{
			if (GetSidebar() == null)
				return;

			CalculateMeterBarDimensions();
			CheckBarNumber();

			// If just changed power level or low power, flash the last bar meter
			lastMeterCheck++;
			if (lastMeterCheck < TickWait)
				return;

			lastMeterCheck = 0;

			// Number of power units represent each bar
			var stepSize = PowerUnitsPerBar;

			var powerManager = World.LocalPlayer.PlayerActor.Trait<PowerManager>();
			var totalPowerDisplay = Math.Max(powerManager.PowerProvided, powerManager.PowerDrained);

			var totalPowerStep = decimal.Floor(totalPowerDisplay / stepSize);
			var powerUsedStep = decimal.Floor(powerManager.PowerDrained / stepSize);
			var powerAvailableStep = decimal.Floor(powerManager.PowerProvided / stepSize);

			// Display a percentage if the bar is maxed out
			if (totalPowerStep > Children.Count)
			{
				var powerFraction = (float)Children.Count / (float)totalPowerStep;
				totalPowerDisplay = (int)((float)totalPowerDisplay * powerFraction);
				totalPowerStep = (int)((float)totalPowerStep * powerFraction);
				powerUsedStep = (int)((float)powerUsedStep * powerFraction);
				powerAvailableStep = (int)((float)powerAvailableStep * powerFraction);
			}

			CheckFlash(powerManager, totalPowerDisplay);

			for (var i = 0; i < Children.Count; i++)
			{
				var image = Children[i] as ImageWidget;
				if (image == null)
					continue;

				if (i > totalPowerStep || totalPowerStep == 0)
				{
					image.ImageName = NoPowerImage;
					continue;
				}

				var targetIcon = AvailablePowerImage;

				if (i < powerUsedStep)
					targetIcon = UsedPowerImage;

				if (i > powerAvailableStep)
					targetIcon = OverUsedPowerImage;

				if (i == totalPowerStep && powerManager.PowerState == PowerState.Low)
					targetIcon = OverUsedPowerImage;

				// Flash the top bar if something is wrong
				if (i == totalPowerStep)
				{
					if (warningFlash % 2 != 0)
						targetIcon = FlashPowerImage;
					if (warningFlash > 0)
						warningFlash--;
				}

				// We exit if updating a bar meter. This gives a nice animation effect
				if (image.ImageName != targetIcon)
				{
					image.ImageName = targetIcon;
					if (!bypassAnimation)
						return;
				}
			}

			bypassAnimation = false;
		}
	}
}
