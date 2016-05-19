function FindMSBuild
{
	$msBuildVersions = @("4.0")
	foreach ($msBuildVersion in $msBuildVersions)
	{
		$key = "HKLM:\SOFTWARE\Microsoft\MSBuild\ToolsVersions\{0}" -f $msBuildVersion
		$property = Get-ItemProperty $key -ErrorAction SilentlyContinue
		if ($property -eq $null -or $property.MSBuildToolsPath -eq $null)
		{
			continue
		}
		$path = Join-Path $property.MSBuildToolsPath -ChildPath "MSBuild.exe"
		if (Test-Path $path)
		{
			return $path
		}
	}
	return $null
}

if ($args.Length -eq 0)
{
	echo "Command list:"
	echo ""
	echo "  all             Builds the mod dll."
	echo "  dependencies    Copies the mod's dependencies into the"
	echo "                  'OpenRA.Mods.RA2/dependencies' folder."
	echo ""
	$command = (Read-Host "Enter command").Split(' ', 2)
}
else
{
	$command = $args
}

if ($command -eq "all")
{
	$msBuild = FindMSBuild
	$msBuildArguments = "/t:Rebuild /nr:false"
	if ($msBuild -eq $null)
	{
		echo "Unable to locate an appropriate version of MSBuild."
	}
	else
	{
		cd OpenRA.Mods.RA2
		$proc = Start-Process $msBuild $msBuildArguments -NoNewWindow -PassThru -Wait
		if ($proc.ExitCode -ne 0)
		{
			echo "Build failed. If just the development tools failed to build, try installing Visual Studio. You may also still be able to run the game."
		}
		else
		{
			echo "Build succeeded."
		}
		cd ..
	}
}
elseif ($command -eq "dependencies")
{
	cd OpenRA.Mods.RA2\dependencies
	$targetDir = $PWD
	if ($args.Length -eq 1)
	{
		$OpenRADir = Read-Host "Enter the path to your OpenRA install"
	}
	else
	{
		$OpenRADir = $command[1]
	}
	if (!(Test-Path $OpenRADir))
	{
		echo "Given directory does not exist!"
	}
	else
	{
		cd $OpenRADir

		if (!(Test-Path OpenRA.Game.exe))
		{
			echo "Unable to find 'OpenRA.Game.exe'!"
			echo "You need to build OpenRA first."
		}
		else
		{
			cp OpenRA.Game.exe $targetDir
		}

		if (!(Test-Path mods/common/OpenRA.Mods.Common.dll))
		{
			echo "Unable to find the 'common' mod (dll)!"
		}
		else
		{
			cp mods/common/OpenRA.Mods.Common.dll $targetDir
		}

		if (!(Test-Path mods/ra/OpenRA.Mods.RA.dll))
		{
			echo "Unable to find the Red Alert mod (dll)!"
		}
		else
		{
			cp mods/ra/OpenRA.Mods.RA.dll $targetDir
		}

		if (!(Test-Path mods/ts/OpenRA.Mods.TS.dll))
		{
			echo "Unable to find the Tiberian Sun mod (dll)!"
		}
		else
		{
			cp mods/ts/OpenRA.Mods.TS.dll $targetDir
		}

		if (!(Test-Path Eluant.dll))
		{
			echo "Unable to find the Eluant dll!"
		}
		else
		{
			cp Eluant.dll $targetDir
		}

		cd $targetDir
		cd ../..
		echo "Dependencies copied."
	}
}
else
{
	echo ("Invalid command '{0}'" -f $command)
}

if ($args.Length -eq 0)
{
	echo "Press enter to continue."
	while ($true)
	{
		if ([System.Console]::KeyAvailable)
		{
			break
		}
		Start-Sleep -Milliseconds 50
	}
}
