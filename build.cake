// OS X: brew install cake
// Windows: choco install cake.portable
//
// Example:
// cake -auto=true -openra-root=~/projects/openra

var target = Argument("target", "default").ToLowerInvariant();
var configuration = Argument("configuration", "Debug");

var rootDir = Directory(".");
var depsDir = Directory("./OpenRA.Mods.RA2/dependencies");

// TODO: Combine 'depFilenamesInRA2DepsDir' and 'depsInOpenRA' into an array of pairs/structs
var depFilenamesInRA2DepsDir = new[] {
    "Eluant.dll",
    "OpenRA.Game.exe",
    "OpenRA.Mods.Common.dll",
    "OpenRA.Mods.Cnc.dll"
};

var depsInOpenRA = new[] {
    "Eluant.dll",
    "OpenRA.Game.exe",
    "mods/common/OpenRA.Mods.Common.dll",
    "mods/common/OpenRA.Mods.Cnc.dll"
};

// Location on-disk of the OpenRA source code.
var openraRoot = Argument<string>("openra-root", null);

// Should dependencies be automatically copied (if found in openraRoot)?
var auto = Argument<bool>("auto", false);

Task("deps").Does(() => {
    var missingDeps = new List<string>();
    foreach (var dep in depFilenamesInRA2DepsDir)
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
        var depPathInOpenRA = depsInOpenRA.SingleOrDefault(d => d.EndsWith(dep));
        if (depPathInOpenRA == null)
            throw new Exception(nameof(depsInOpenRA) + " does not contain an entry for " + dep);

        var dstPath = System.IO.Path.Combine(depsDir.Path.FullPath, dep);
        var srcPath = System.IO.Path.Combine(openraRoot, depPathInOpenRA);

        if (!System.IO.File.Exists(srcPath))
            Error(string.Format("Expected {0} to exist but it didn't.", srcPath));
        else
        {
            if (!auto)
            {
                Console.Write(string.Format("Would you like to copy {0} to {1}? [Y/n] ", srcPath, dstPath));
                var input = Console.ReadLine().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(input) && input != "y" && input != "yes")
                    continue;
            }

            System.IO.File.Copy(srcPath, dstPath, true);
            if (System.IO.File.Exists(dstPath))
                missingDeps.Remove(dep);
        }
    }

    if (missingDeps.Any())
    {
        var msg = String.format("Missing {0} dependencies." + Environment.NewLine, missingDeps.Count);
        foreach (var md in missingDeps)
            msg += String.format("\t{0}{1}", md, Environment.NewLine);

        throw new Exception(msg);
    }
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
