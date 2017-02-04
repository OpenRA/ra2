// OS X: brew install cake
// Windows: choco install cake.portable
//
// Example:
// cake -auto=true -openra-root=~/projects/openra

var target = Argument("target", "default").ToLowerInvariant();
var configuration = Argument("configuration", "Debug");

var rootDir = Directory(".");
var depsDir = Directory("./OpenRA.Mods.RA2/dependencies");

// TODO: Combine 'deps' and 'depsInOpenRA' into an array of pairs/structs
var deps = new[] {
    "Eluant.dll",
    "OpenRA.Game.exe",
    "OpenRA.Mods.Common.dll",
    "OpenRA.Mods.RA.dll",
    "OpenRA.Mods.Cnc.dll"
};

var depsInOpenRA = new[] {
    "Eluant.dll",
    "OpenRA.Game.exe",
    "mods/common/OpenRA.Mods.Common.dll",
    "mods/ra/OpenRA.Mods.RA.dll",
    "mods/cnc/OpenRA.Mods.Cnc.dll"
};

// Location on-disk of the OpenRA source code.
var openraRoot = Argument<string>("openra-root", null);

// Should dependencies be automatically copied (if found in openraRoot)?
var auto = Argument<bool>("auto", false);

Task("deps").Does(() => {
    var missingDeps = new List<string>();
    foreach (var dep in deps)
    {
        var fullPath = System.IO.Path.Combine(depsDir.Path.FullPath, dep);
        if (!System.IO.File.Exists(fullPath))
            missingDeps.Add(dep);
    }

    if (!missingDeps.Any())
    {
        Information("All dependencies accounted for. Aborting 'deps' task.");
        return;
    }

    // Steps to resolving dependency location:
    //   1) environment variable OPENRA_ROOT
    //   2) -openra-root=<path> command-line argument
    //   3) Ask the user for the path (only if `auto` is false!)

    if (string.IsNullOrWhiteSpace(openraRoot))
        openraRoot = Environment.GetEnvironmentVariable("OPENRA_ROOT");

    if (!auto && string.IsNullOrWhiteSpace(openraRoot))
    {
        Console.Write("Please enter the path to the OpenRA root: ");
        openraRoot = Console.ReadLine();
    }

    if (openraRoot.StartsWith("~"))
        openraRoot = openraRoot.Replace("~", IsRunningOnWindows() ?
            Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%") :
            Environment.GetEnvironmentVariable("HOME"));

    var missingDepsCopy = missingDeps.ToArray();
    for (var i = 0; i < missingDepsCopy.Length; i++)
    {
        var dep = missingDepsCopy[i];
        var depPathInOpenRA = depsInOpenRA[i];

        var depPath = System.IO.Path.Combine(depsDir.Path.FullPath, dep);
        var oraPath = System.IO.Path.Combine(openraRoot, depPathInOpenRA);

        if (!System.IO.File.Exists(oraPath))
            Error(string.Format("Could not automatically resolve missing dependency '{0}'.", dep));
        else
        {
            if (!auto)
            {
                Console.Write(string.Format("Would you like to copy {0} to {1}? [Y/n] ", oraPath, depPath));
                var input = Console.ReadLine().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(input) && input != "y" && input != "yes")
                    continue;
            }

            System.IO.File.Copy(oraPath, depPath, true);
            if (System.IO.File.Exists(depPath))
                missingDeps.Remove(dep);
        }
    }

    if (missingDeps.Any())
        throw new Exception(string.Format("Missing {0} dependencies.", missingDeps.Count));
});

Task("default")
    .IsDependentOn("deps")
    .Does(() => {
        if (IsRunningOnWindows())
            MSBuild("./OpenRA.Mods.RA2/OpenRA.Mods.RA2.sln", settings => settings.SetConfiguration(configuration));
        else
            XBuild("./OpenRA.Mods.RA2/OpenRA.Mods.RA2.sln", settings => settings.SetConfiguration(configuration));

        System.IO.File.Copy("./OpenRA.Mods.RA2/bin/Debug/OpenRA.Mods.RA2.dll", "./OpenRA.Mods.RA2.dll", true);
});

Task("clean").Does(() => {
    DeleteFiles("./OpenRA.Mods.RA2/bin/*/*.dll");
    DeleteFiles("./OpenRA.Mods.RA2/bin/*/*.exe");
    DeleteFiles("./OpenRA.Mods.RA2/obj/*/*.dll");
    DeleteFiles("./OpenRA.Mods.RA2/obj/*/*.exe");
    DeleteFiles("./OpenRA.Mods.RA2/dependencies/*.exe");
    DeleteFiles("./OpenRA.Mods.RA2/dependencies/*.dll");

    if (FileExists("./OpenRA.Mods.RA2.dll"))
        DeleteFile("./OpenRA.Mods.RA2.dll");
});

RunTarget(target);
