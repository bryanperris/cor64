using System.IO;
using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Paket.PaketTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public Build() {
        NoLogo = true;
    }

    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' or 'Release'")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Test filter")]
    readonly string filter;

    [Parameter("Debug tests")]
    readonly bool debugTest;

    [Solution] readonly Solution Solution;
    // [GitRepository] readonly GitRepository GitRepository;
    // [GitVersion] readonly GitVersion GitVersion;
    Project RunN64 => Solution.GetProject("RunN64");
    Project RdpTests => Solution.GetProject("RdpTests");
    Project Tests => Solution.GetProject("Tests");

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath SourceTestRomsDirectory => RunN64.Directory / "TestRoms";
    AbsolutePath BinDirectory => RootDirectory / "bin";
    AbsolutePath BinTestRomsDirectory => BinDirectory / "TestRoms";
    AbsolutePath PackagesDirectory = RootDirectory / "packages";
    AbsolutePath ScriptsDirectory = RootDirectory / "src" / "RunN64" / "Scripts";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            RootDirectory.GlobDirectories("bin").ForEach(DeleteDirectory);
            DeleteDirectory(PackagesDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            PaketRestore(s => s
                .SetProcessToolPath(RootDirectory / ".paket/paket.exe")
                // .SetArgumentConfigurator(_ => new Arguments().Add("install"))
                .SetProcessWorkingDirectory(RootDirectory));

            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    // Target DeployTestRoms => _ => _
    //     .Executes(() =>
    //     {
    //         DeleteDirectory(BinTestRomsDirectory);
    //         CopyDirectoryRecursively(SourceTestRomsDirectory, BinTestRomsDirectory);
    //     });

    Target Scripts => _ => _
        .Executes(() => {
            var scriptsDir = BinDirectory / "Scripts";
            DeleteDirectory(scriptsDir);
            CopyDirectoryRecursively(ScriptsDirectory, scriptsDir);
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .AddDefines(new Defines())
                // .SetAssemblyVersion(GitVersion.AssemblySemVer)
                // .SetFileVersion(GitVersion.AssemblySemFileVer)
                // .SetInformationalVersion(GitVersion.InformationalVersion)
                .EnableNoRestore());
        })
        .Triggers(Scripts);

    Target CompileForTesting => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            Defines defines = new Defines();
            defines.EnableTestingMode();

            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .AddDefines(defines)
                // .SetAssemblyVersion(GitVersion.AssemblySemVer)
                // .SetFileVersion(GitVersion.AssemblySemFileVer)
                // .SetInformationalVersion(GitVersion.InformationalVersion)
                .EnableNoRestore());
        })
        .Triggers(Scripts);

    Target FullBuild => _ => _
        .DependsOn(Compile);
        // .DependsOn(DeployTestRoms);

    Target Run => _ => _
        .DependsOn(Compile)
        // .DependsOn(DeployTestRoms)
        .Executes(() => {
            // DotNetLogger = (o, log) => {
            //     if (o == OutputType.Std) {
            //         Console.WriteLine(log);
            //     }
            //     else {
            //         Console.Error.WriteLine(log);
            //     }
            // };

            DotNetRun(s => s
                .SetProjectFile(RunN64)
                .SetConfiguration(Configuration)
                .SetProcessWorkingDirectory(BinDirectory)
                .SetNoRestore(true)
                .EnableNoBuild()
            );
        });
    Target RdpTesting => _ => _
        .DependsOn(Compile)
        // .DependsOn(DeployTestRoms)
        .Executes(() => {
            DotNetRun(s => s
                .SetProjectFile(RdpTests)
                .SetConfiguration(Configuration)
                .SetProcessWorkingDirectory(BinDirectory)
                .EnableNoBuild()
            );
        });

    Target Test => _ => _
        .DependsOn(CompileForTesting)
        .Executes(() => {
            DotNetTest(s => s
                .SetFilter(filter ?? "")
                .SetVerbosity(DotNetVerbosity.Normal)
                .SetProjectFile(Tests)
                .SetConfiguration(Configuration)
                .SetProcessWorkingDirectory(BinDirectory)
                .SetProcessEnvironmentVariable("VSTEST_HOST_DEBUG", debugTest ? "1" : "0")
                .EnableNoBuild());
        });
}
