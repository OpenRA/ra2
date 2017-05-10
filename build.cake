// OS X: brew install cake
// Windows: choco install cake.portable
//
// Example:
// cake -auto=true

using System.Text.RegularExpressions;

var target = Argument("target", "default").ToLowerInvariant();
var configuration = Argument("configuration", "Debug");

// Location on-disk of the OpenRA source code.
string GetEngineSourceRootPath(string filename = ".env") {
    if (string.IsNullOrWhiteSpace(filename))
        return null;

    var envVal = Environment.GetEnvironmentVariable("OPENRA_ROOT");
    if (!string.IsNullOrWhiteSpace(envVal))
        return envVal;

    var dotEnvPath = Directory(".") + File(filename);
    if (!FileExists(dotEnvPath))
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
            throw new Exception(string.Format("Could not find key on line {0} of {1}", i, dotEnvPath));
        else if (val == null)
            throw new Exception(string.Format("Could not find value on line {0} of {1}", i, dotEnvPath));

        if (key == "OPENRA_ROOT")
            return val;
    }

    return null;
}

Task("deps").Does(() => {
    var engineRootPath = GetEngineSourceRootPath();
    if (engineRootPath == null)
        Error("Failed to get engine root path (OPENRA_ROOT).");

    var dependencyFileNames = new[] {
        "Eluant.dll",
        "OpenRA.Game.exe",
        "OpenRA.Mods.Common.dll",
        "OpenRA.Mods.Cnc.dll"
    };

    var dependencyFilePathsInEngineSource = new[] {
        "Eluant.dll",
        "OpenRA.Game.exe",
        "mods/common/OpenRA.Mods.Common.dll",
        "mods/common/OpenRA.Mods.Cnc.dll"
    };

    var destinationDependencyDirectory = Directory("./OpenRA.Mods.RA2/dependencies");

    var missingDependencyFileNames = new List<string>();
    foreach (var dependencyFileName in dependencyFileNames)
        if (!FileExists(destinationDependencyDirectory + File(dependencyFileName))
            missingDependencyFileNames.Add(dependencyFileName);

    if (!missingDependencyFileNames.Any())
        return;

    if (string.IsNullOrWhiteSpace(engineRootPath))
        Error("Failed to find path to the OpenRA engine source.");

    var missingDependencyFileNamesCopy = missingDependencyFileNames.ToArray();
    foreach (var missingDependencyFileName in missingDependencyFileNamesCopy)
    {
        var dependencyPathInEngineSource = dependencyFilePathsInEngineSource.SingleOrDefault(fp => fp.EndsWith(missingDependencyFileName));
        if (dependencyPathInEngineSource == null)
            Error(string.Format("dependencyFilePathsInEngineSource does not contain an entry for '{0}'", missingDependencyFileName));

        var absoluteDependencyPathInEngineSource = engineRootPath + File(dependencyPathInEngineSource);
        var absoluteDependencyPathInRa2 = destinationDependencyDirectory + File(missingDependencyFileName);

        if (!FileExists(absoluteDependencyPathInEngineSource))
            Error(string.Format("Could not automatically resolve missing dependency '{0}'.", absoluteDependencyPathInEngineSource));
        else
        {
            CopyFile(absoluteDependencyPathInEngineSource, absoluteDependencyPathInRa2);

            if (FileExists(absoluteDependencyPathInRa2))
                missingDependencyFileNames.Remove(missingDependencyFileName);
            else
                Error(string.Format("Failed to copy '{0}' to '{1}'", absoluteDependencyPathInEngineSource, absoluteDependencyPathInRa2));
        }
    }

    if (missingDependencyFileNames.Any())
        Error(string.Format("Missing {0} dependencies.", missingDependencyFileNames.Count));
});

Task("default").IsDependentOn("deps").Does(() => {
    if (IsRunningOnWindows())
        MSBuild("./OpenRA.Mods.RA2/OpenRA.Mods.RA2.sln", settings => settings.SetConfiguration(configuration));
    else
        XBuild("./OpenRA.Mods.RA2/OpenRA.Mods.RA2.sln", settings => settings.SetConfiguration(configuration));

    CopyFile("./OpenRA.Mods.RA2/bin/Debug/OpenRA.Mods.RA2.dll", "./OpenRA.Mods.RA2.dll");
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

string GetGitHashOfDirectory(string directoryPath) {
    try {
        var root = Directory(directoryPath);
        var gitDir = root + Directory(".git");
        var gitHeadFile = gitDir + File("HEAD");
        var gitHeadFileContents = System.IO.File.ReadAllText(gitHeadFile);
        var split = gitHeadFileContents.Split(new[] { ':' }, 2);
        var refFileStr = split[1].Trim();
        var gitRefFile = gitDir + Directory(refFileStr);
        var hash = System.IO.File.ReadAllText(gitRefFile);
        return "git-" + hash.Substring(0, 9);
    } catch { }

    return null;
}

Task("version").Does(() => {
    var engineRootPath = GetEngineSourceRootPath();
    if (engineRootPath == null)
        Error("Failed to get engine root path (OPENRA_ROOT).");

    var modRootDir = Directory(".");
    var manifestPath = modRootDir + File("mod.yaml");
    var manifestContents = System.IO.File.ReadAllText(manifestPath);

    var modHash = GetGitHashOfDirectory(".");
    if (modHash == null)
        Error("Failed to get hash of the RA2 mod");

    var engineHash = GetGitHashOfDirectory(engineRootPath);
    if (engineHash == null)
        Error("Failed to get hash of the OpenRA engine.");

    var newManifestContents = Regex.Replace(manifestContents, "\tVersion:.*\n", "\tVersion: " + modHash + "\n", RegexOptions.IgnoreCase);
    newManifestContents = Regex.Replace(newManifestContents, "\tmodchooser:.*\n", "\tmodchooser: " + engineHash + "\n", RegexOptions.IgnoreCase);
    newManifestContents = Regex.Replace(newManifestContents, "\tcnc:.*\n", "\tcnc: " + engineHash + "\n", RegexOptions.IgnoreCase);
    newManifestContents = Regex.Replace(newManifestContents, "\tcommon:.*\n", "\tcommon: " + engineHash + "\n", RegexOptions.IgnoreCase);

    System.IO.File.WriteAllText(manifestPath, newManifestContents);
});

RunTarget(target);
