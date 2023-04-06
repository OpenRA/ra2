####### The starting point for the script is the bottom #######

###############################################################
########################## FUNCTIONS ##########################
###############################################################
function All-Command
{
	If (!(Test-Path "*.sln"))
	{
		Write-Host "No custom solution file found. Aborting." -ForegroundColor Red
		return
	}

	if ((CheckForDotnet) -eq 1)
	{
		return
	}

	Write-Host "Building $modID in" $configuration "configuration..." -ForegroundColor Cyan
	dotnet build -c $configuration --nologo -p:TargetPlatform=win-x64

	if ($lastexitcode -ne 0)
	{
		Write-Host "Build failed. If just the development tools failed to build, try installing Visual Studio. You may also still be able to run the game." -ForegroundColor Red
	}
	else
	{
		Write-Host "Build succeeded." -ForegroundColor Green
	}
}

function Clean-Command
{
	If (!(Test-Path "*.sln"))
	{
		Write-Host "No custom solution file found - nothing to clean. Aborting." -ForegroundColor Red
		return
	}

	if ((CheckForDotnet) -eq 1)
	{
		return
	}

	Write-Host "Cleaning $modID..." -ForegroundColor Cyan

	dotnet clean /nologo
	Remove-Item ./*/obj -Recurse -ErrorAction Ignore
	Remove-Item env:ENGINE_DIRECTORY/bin -Recurse -ErrorAction Ignore
	Remove-Item env:ENGINE_DIRECTORY/*/obj -Recurse -ErrorAction Ignore

	Write-Host "Clean complete." -ForegroundColor Green
}

function Version-Command
{
	if ($command.Length -gt 1)
	{
		$version = $command[1]
	}
	elseif (Get-Command 'git' -ErrorAction SilentlyContinue)
	{
		$gitRepo = git rev-parse --is-inside-work-tree
		if ($gitRepo)
		{
			$version = git name-rev --name-only --tags --no-undefined HEAD 2>$null
			if ($version -eq $null)
			{
				$version = "git-" + (git rev-parse --short HEAD)
			}
		}
		else
		{
			Write-Host "Not a git repository. The version will remain unchanged." -ForegroundColor Red
		}
	}
	else
	{
		Write-Host "Unable to locate Git. The version will remain unchanged." -ForegroundColor Red
	}

	if ($version -ne $null)
	{
		$mod = "mods/" + $modID + "/mod.yaml"
		$replacement = (gc $mod) -Replace "Version:.*", ("Version: {0}" -f $version)
		sc $mod $replacement

		$prefix = $(gc $mod) | Where { $_.ToString().EndsWith(": User") }
		if ($prefix -and $prefix.LastIndexOf("/") -ne -1)
		{
			$prefix = $prefix.Substring(0, $prefix.LastIndexOf("/"))
		}
		$replacement = (gc $mod) -Replace ".*: User", ("{0}/{1}: User" -f $prefix, $version)
		sc $mod $replacement

		Write-Host ("Version strings set to '{0}'." -f $version)
	}
}

function Test-Command
{
	if ((CheckForUtility) -eq 1)
	{
		return
	}

	Write-Host "Testing $modID mod MiniYAML..." -ForegroundColor Cyan
	InvokeCommand "$utilityPath $modID --check-yaml"
}

function Check-Command
{
	If (!(Test-Path "*.sln"))
	{
		Write-Host "No custom solution file found. Skipping static code checks." -ForegroundColor Cyan
		return
	}

	Write-Host "Compiling $modID in Debug configuration..." -ForegroundColor Cyan

	# Enabling EnforceCodeStyleInBuild and GenerateDocumentationFile as a workaround for some code style rules (in particular IDE0005) being bugged and not reporting warnings/errors otherwise.
	dotnet build -c Debug --nologo -warnaserror -p:TargetPlatform=win-x64 -p:EnforceCodeStyleInBuild=true -p:GenerateDocumentationFile=true
	if ($lastexitcode -ne 0)
	{
		Write-Host "Build failed." -ForegroundColor Red
	}

	if ((CheckForUtility) -eq 0)
	{
		Write-Host "Checking $modID for explicit interface violations..." -ForegroundColor Cyan
		InvokeCommand "$utilityPath $modID --check-explicit-interfaces"

		Write-Host "Checking $modID for incorrect conditional trait interface overrides..." -ForegroundColor Cyan
		InvokeCommand "$utilityPath $modID --check-conditional-trait-interface-overrides"
	}
}

function Check-Scripts-Command
{
	if ((Get-Command "luac.exe" -ErrorAction SilentlyContinue) -ne $null)
	{
		Write-Host "Testing Lua scripts..." -ForegroundColor Cyan
		foreach ($script in ls "mods/*/maps/*/*.lua")
		{
			luac -p $script
		}
		Write-Host "Check completed!" -ForegroundColor Green
	}
	else
	{
		Write-Host "luac.exe could not be found. Please install Lua." -ForegroundColor Red
	}
}

function CheckForUtility
{
	if (Test-Path $utilityPath)
	{
		return 0
	}

	Write-Host "OpenRA.Utility.exe could not be found. Build the project first using the `"all`" command." -ForegroundColor Red
	return 1
}

function CheckForDotnet
{
	if ((Get-Command "dotnet" -ErrorAction SilentlyContinue) -eq $null)
	{
		Write-Host "The 'dotnet' tool is required to compile OpenRA. Please install the .NET 6.0 SDK and try again. https://dotnet.microsoft.com/download/dotnet/6.0" -ForegroundColor Red
		return 1
	}

	return 0
}

function WaitForInput
{
	Write-Host "Press enter to continue."
	while ($true)
	{
		if ([System.Console]::KeyAvailable)
		{
			exit
		}
		Start-Sleep -Milliseconds 50
	}
}

function ReadConfigLine($line, $name)
{
	$prefix = $name + '='
	if ($line.StartsWith($prefix))
	{
		[Environment]::SetEnvironmentVariable($name, $line.Replace($prefix, '').Replace('"', ''))
	}
}

function ParseConfigFile($fileName)
{
	$names = @("MOD_ID", "ENGINE_VERSION", "AUTOMATIC_ENGINE_MANAGEMENT", "AUTOMATIC_ENGINE_SOURCE",
		"AUTOMATIC_ENGINE_EXTRACT_DIRECTORY", "AUTOMATIC_ENGINE_TEMP_ARCHIVE_NAME", "ENGINE_DIRECTORY")

	$reader = [System.IO.File]::OpenText($fileName)
	while($null -ne ($line = $reader.ReadLine()))
	{
		foreach ($name in $names)
		{
			ReadConfigLine $line $name
		}
	}
	$reader.Close()

	$missing = @()
	foreach ($name in $names)
	{
		if (!([System.Environment]::GetEnvironmentVariable($name)))
		{
			$missing += $name
		}
	}

	if ($missing)
	{
		Write-Host "Required mod.config variables are missing:"
		foreach ($m in $missing)
		{
			Write-Host "   $m"
		}
		Write-Host "Repair your mod.config (or user.config) and try again."
		WaitForInput
		exit
	}
}

function InvokeCommand
{
	param($expression)
	# $? is the return value of the called expression
	# Invoke-Expression itself will always succeed, even if the invoked expression fails
	# So temporarily store the return value in $success
	$expression += '; $success = $?'
	Invoke-Expression $expression
	if ($success -eq $False)
	{
		exit 1
	}
}

###############################################################
############################ Main #############################
###############################################################
if ($PSVersionTable.PSVersion.Major -clt 3)
{
    Write-Host "The makefile requires PowerShell version 3 or higher." -ForegroundColor Red
    Write-Host "Please download and install the latest Windows Management Framework version from Microsoft." -ForegroundColor Red
    WaitForInput
}

if ($args.Length -eq 0)
{
	Write-Host "Command list:"
	Write-Host ""
	Write-Host "  all             Builds the game, its development tools and the mod dlls."
	Write-Host "  version         Sets the version strings for all mods to the latest"
	Write-Host "                  version for the current Git branch."
	Write-Host "  clean           Removes all built and copied files."
	Write-Host "                  from the mods and the engine directories."
	Write-Host "  test            Tests the mod's MiniYAML for errors."
	Write-Host "  check           Checks .cs files for StyleCop violations."
	Write-Host "  check-scripts   Checks .lua files for syntax errors."
	Write-Host ""
	$command = (Read-Host "Enter command").Split(' ', 2)
}
else
{
	$command = $args
}

# Set the working directory for our IO methods
$templateDir = $pwd.Path
[System.IO.Directory]::SetCurrentDirectory($templateDir)

# Load the environment variables from the config file
# and get the mod ID from the local environment variable
ParseConfigFile "mod.config"

if (Test-Path "user.config")
{
	ParseConfigFile "user.config"
}

$modID = $env:MOD_ID

$env:MOD_SEARCH_PATHS = "./mods,$env:ENGINE_DIRECTORY/mods"
$env:ENGINE_DIR = ".." # Set to potentially be used by the Utility and different than $env:ENGINE_DIRECTORY, which is for the script.

# Fetch the engine if required
if ($command -eq "all" -or $command -eq "clean" -or $command -eq "check")
{
	$versionFile = $env:ENGINE_DIRECTORY + "/VERSION"
	$currentEngine = ""
	if (Test-Path $versionFile)
	{
		$reader = [System.IO.File]::OpenText($versionFile)
		$currentEngine = $reader.ReadLine()
		$reader.Close()
	}

	if ($currentEngine -ne "" -and $currentEngine -eq $env:ENGINE_VERSION)
	{
		cd $env:ENGINE_DIRECTORY
		Invoke-Expression ".\make.cmd $command"
		Write-Host ""
		cd $templateDir
	}
	elseif ($env:AUTOMATIC_ENGINE_MANAGEMENT -ne "True")
	{
		Write-Host "Automatic engine management is disabled."
		Write-Host "Please manually update the engine to version $env:ENGINE_VERSION."
		WaitForInput
	}
	else
	{
		Write-Host "OpenRA engine version $env:ENGINE_VERSION is required."

		if (Test-Path $env:ENGINE_DIRECTORY)
		{
			if ($currentEngine -ne "")
			{
				Write-Host "Deleting engine version $currentEngine."
			}
			else
			{
				Write-Host "Deleting existing engine (unknown version)."
			}

			rm $env:ENGINE_DIRECTORY -r
		}

		Write-Host "Downloading engine..."

		if (Test-Path $env:AUTOMATIC_ENGINE_EXTRACT_DIRECTORY)
		{
			rm $env:AUTOMATIC_ENGINE_EXTRACT_DIRECTORY -r
		}

		$url = $env:AUTOMATIC_ENGINE_SOURCE
		$url = $url.Replace("$", "").Replace("{ENGINE_VERSION}", $env:ENGINE_VERSION)

		mkdir $env:AUTOMATIC_ENGINE_EXTRACT_DIRECTORY > $null
		$dlPath = Join-Path $pwd (Split-Path -leaf $env:AUTOMATIC_ENGINE_EXTRACT_DIRECTORY)
		$dlPath = Join-Path $dlPath (Split-Path -leaf $env:AUTOMATIC_ENGINE_TEMP_ARCHIVE_NAME)

		$client = new-object System.Net.WebClient
		[Net.ServicePointManager]::SecurityProtocol = 'Tls12'
		$client.DownloadFile($url, $dlPath)

		Add-Type -assembly "system.io.compression.filesystem"
		[io.compression.zipfile]::ExtractToDirectory($dlPath, $env:AUTOMATIC_ENGINE_EXTRACT_DIRECTORY)
		rm $dlPath

		$extractedDir = Get-ChildItem $env:AUTOMATIC_ENGINE_EXTRACT_DIRECTORY -Recurse | ?{ $_.PSIsContainer } | Select-Object -First 1
		Move-Item $extractedDir.FullName -Destination $templateDir
		Rename-Item $extractedDir.Name (Split-Path -leaf $env:ENGINE_DIRECTORY)

		rm $env:AUTOMATIC_ENGINE_EXTRACT_DIRECTORY -r

		cd $env:ENGINE_DIRECTORY
		Invoke-Expression ".\make.cmd version $env:ENGINE_VERSION"
		Invoke-Expression ".\make.cmd $command"
		Write-Host ""
		cd $templateDir
	}
}

$utilityPath = $env:ENGINE_DIRECTORY + "/bin/OpenRA.Utility.exe"

$configuration = "Release"
if ($args.Contains("CONFIGURATION=Debug"))
{
	$configuration = "Debug"
}

$execute = $command
if ($command.Length -gt 1)
{
	$execute = $command[0]
}

switch ($execute)
{
	"all" { All-Command }
	"version" { Version-Command }
	"clean" { Clean-Command }
	"test" { Test-Command }
	"check" { Check-Command }
	"check-scripts" { Check-Scripts-Command }
	Default { Write-Host ("Invalid command '{0}'" -f $command) }
}

# In case the script was called without any parameters we keep the window open
if ($args.Length -eq 0)
{
	WaitForInput
}
