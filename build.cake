// OS X: brew install cake
// Windows: choco install cake.portable
//
// Example:
// cake -auto=true

var target = Argument("target", "default").ToLowerInvariant();
var configuration = Argument("configuration", "Debug");

var depsDir = Directory("./OpenRA.Mods.RA2/dependencies");

// TODO: Combine 'deps' and 'depsInOpenRA' into an array of pairs/structs
var deps = new[] {
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
var openraRoot = Argument<string>("openra-root", GetEngineSourceRootPath());

// Should dependencies be automatically copied (if found in openraRoot)?
var auto = Argument<bool>("auto", false);

string GetEngineSourceRootPath(string filename = ".env") {
    var envVal = Environment.GetEnvironmentVariable("OPENRA_ROOT");
    if (!string.IsNullOrWhiteSpace(envVal))
        return envVal;

    var dotEnvPath = Directory(".") + File(filename);
    if (!System.IO.File.Exists(dotEnvPath))
        return null;

    var i = 0;
    foreach (var l in System.IO.File.ReadLines(dotEnvPath))
    {
        i++;
        var line = l.Trim();
        if (line.StartsWith("#"))
            continue;

        var split = line.Split(new char[] { '=' }, 2);
        var key = split?[0]?.Trim();
        var val = split?[1]?.Trim();

        if (string.IsNullOrWhiteSpace(key))
            throw new Exception($"Could not find key on line {i} of {dotEnvPath}");
        else if (val == null)
            throw new Exception($"Could not find value on line {i} of {dotEnvPath}");

        if (key == "OPENRA_ROOT")
            return val;
    }

    return null;
}

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
    //   2) .env file
    //   3) -openra-root=<path> command-line argument
    //   4) Ask the user for the path

    if (string.IsNullOrWhiteSpace(openraRoot))
        openraRoot = GetEngineSourceRootPath();

    if (string.IsNullOrWhiteSpace(openraRoot))
        Error("Failed to find path to the OpenRA engine source");

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
        Error(string.Format("Missing {0} dependencies.", missingDeps.Count));
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
