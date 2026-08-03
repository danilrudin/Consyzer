[![Build Status](https://github.com/danilrudin/Consyzer/workflows/Build/badge.svg)](https://github.com/danilrudin/Consyzer/actions/workflows/build.yml) [![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=danilrudin_Consyzer&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=danilrudin_Consyzer) [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=danilrudin_Consyzer&metric=coverage)](https://sonarcloud.io/summary/new_code?id=danilrudin_Consyzer) [![GitHub license](https://img.shields.io/github/license/danilrudin/Consyzer)](https://github.com/danilrudin/Consyzer/blob/master/LICENSE)

This README is also available in the following languages:

- [Русский](./Docs/README-RU.md)

## Overview

**Consyzer** is a CLI utility created to prevent CIL module consistency issues when using P/Invoke mechanisms to call methods implemented outside the managed CLR environment.

## Purpose

In CIL application development, it is not uncommon to need access to methods implemented outside the managed .NET ecosystem. In the source code of a CIL module, such calls are described using **DllImport** or **LibraryImport** attributes and are stored in the module metadata after compilation, indicating which exact unmanaged (native) library should be accessed at runtime and which function should be called from it.

A key feature of such calls is that
the code of the function called from the unmanaged library is not linked directly with the source code of the CIL module;
instead, the module metadata stores information about the function being called, including a reference to the expected location of the unmanaged library containing the implementation of that function in the system.

```csharp
// In this example, "foo.dll" is a reference to an unmanaged library containing the implementation of the HelloWorld function:

// Classic P/Invoke
[DllImport("foo.dll")]
public static extern void HelloWorld();

// or

// Source-generated P/Invoke (.NET 7+)
[LibraryImport("foo.dll")]
public static partial void HelloWorld();
```

The application functions correctly without violating system integrity and security when all unmanaged libraries are located in the places described in the metadata;
however, if even one of the libraries is missing, the application will not only crash but may also compromise the security of the entire system.

Consyzer was created to ensure that such situations do not come as a surprise.

### Supported Platforms

At this time, Consyzer supports checking the presence of native libraries in the system on the following platforms:

- Windows
- Linux

## How it works

1. Consyzer selects files for analysis based on the specified directory and search patterns;
2. Consyzer logs and excludes from analysis files that are not ECMA-355 assemblies;
3. Consyzer analyzes the remaining ECMA assemblies for the presence of P/Invoke methods;
4. Consyzer analyzes each found P/Invoke method and checks whether the corresponding native libraries exist in the system;
5. Consyzer generates a report based on the analysis results in one or more formats depending on the configuration;
6. Consyzer returns an exit code indicating the final analysis result, which also allows you to handle analysis incidents individually according to your requirements.

> ⚠️ The analysis is based on the metadata of CIL assemblies and does not check the correctness of marshaling between managed and native code.

## Library Search Model

**Consyzer** uses a strict library analysis model: only a library whose location has been explicitly determined through supported search mechanisms is considered found.

The result of checking the presence of each native library may have one of the following states:

| State          | Analysis meaning                                                                                                                 |
| -------------- | -------------------------------------------------------------------------------------------------------------------------------- |
| `Resolved`     | The library was found through a supported search mechanism                                                                       |
| `Missing`      | The library was not found, and Consyzer does not know of unsupported mechanisms that could change the result                     |
| `Inconclusive` | The library was not found, but the result cannot be considered final because Consyzer cannot simulate part of the OS mechanisms. |

Consyzer also indicates the mechanism through which the presence of the library in the system was detected:

| Mechanism                | Analysis meaning                                                   |
| ------------------------ | ------------------------------------------------------------------ |
| `ExplicitPath`           | The library was found at the path specified in its import          |
| `AssemblyDirectory`      | The library was found next to the assembly declaring the P/Invoke |
| `DefaultSystemLocations` | The library was found in standard OS directories                   |
| `EnvironmentOverride`    | The library was found through an environment variable              |
| `CurrentDirectory`       | The library was found in the current working directory (Windows)   |

Some existing OS loading mechanisms are either not fully modeled and will be added in future versions, or will not be added at all because they cannot be reproduced by static analysis.
For example, on Windows these may include `KnownDLLs`, `SxS`, DLL redirections, and process search directory settings,
and on Linux — `RPATH`, `RUNPATH`, `ld.so.cache`, `ld.so.conf`, and other secure-execution specifics.

If the library was not found, but there are non-simulated mechanisms capable of affecting the result,
Consyzer will register such a library as `Inconclusive` instead of presenting an assumption as a guaranteed result.

Consyzer may also show heuristic matches — for example, a library in the analysis root that is not next to a nested target assembly.
Such matches are also registered separately and do not turn the strict check result into a successful one.

## Analysis Results

**Consyzer** presents analysis results in the form of reports.
The following report formats are supported:

1. `Console`
2. `Json`
3. `Csv`
4. `Xml`

### Example report (Console)

```
[Analysis]
    Platform: Windows
[AssemblyMetadataList]
    [0]
        File: Foo.dll
        Version: 1.0.0.0
        CreationDateUtc: 2025-06-21T12:00:00.0000000Z
        Sha256: ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890
    [1]
        File: Bar.dll
        Version: 2.1.3.0
        CreationDateUtc: 2025-06-22T15:30:00.0000000Z
        Sha256: 1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF
    [2]
        File: Baz.dll
        Version: 1.2.0.0
        CreationDateUtc: 2025-06-23T10:45:00.0000000Z
        Sha256: FEDCBA0987654321FEDCBA0987654321FEDCBA0987654321FEDCBA0987654321
[PInvokeMethodGroups]
    [0]
        File: Foo.dll, Found: 2
        [0]
            Signature: 'Int32 static Native.Foo.DОСtuff()'
            ImportName: 'existentlib.dll'
            ImportFlags: 'CallingConventionCDecl'
        [1]
            Signature: 'Void static Native.Foo.FailStuff(String)'
            ImportName: 'missinglib.dll'
            ImportFlags: 'CallingConventionStdCall'
    [1]
        File: Baz.dll, Found: 1
        [0]
            Signature: 'Boolean static .Baz.CheckSomething(Int32)'
            ImportName: 'anotherlib.dll'
            ImportFlags: 'CallingConventionStdCall'
[LibraryResolutions]
    [0]
        TargetPath: C:\Modules\Foo.dll
        LibraryName: existentlib.dll
        ResolutionState: Resolved
        ResolvedPath: C:\Windows\System32\existentlib.dll
        MechanismKind: DefaultSystemLocations
        HeuristicCandidates: []
        NotSimulated: None
    [1]
        TargetPath: C:\Modules\Foo.dll
        LibraryName: missinglib.dll
        ResolutionState: Inconclusive
        ResolvedPath: null
        MechanismKind: null
        HeuristicCandidates: []
        NotSimulated: WindowsSxS, WindowsKnownDlls, WindowsDllRedirection
    [2]
        TargetPath: C:\Modules\Baz.dll
        LibraryName: anotherlib.dll
        ResolutionState: Resolved
        ResolvedPath: C:\EnvPath\anotherlib.dll
        MechanismKind: EnvironmentOverride
        HeuristicCandidates: []
        NotSimulated: None
[Summary]
    TotalFiles: 3
    EcmaAssemblies: 3
    AssembliesWithPInvoke: 2
    TotalPInvokeMethods: 3
    ResolvedLibraries: 2
    MissingLibraries: 0
    InconclusiveLibraries: 1
```

## Exit Codes

**Consyzer** returns a specific exit code depending on the final analysis state:

| Code | Analysis meaning                                                                                                    |
| ---- | ------------------------------------------------------------------------------------------------------------------- |
| 0    | All libraries were found through supported search mechanisms                                                        |
| 1    | One or more libraries are missing                                                                                   |
| 2    | One or more libraries were not found by checked mechanisms, but may be found by non-simulated OS loading mechanisms |
| 3    | Input parameter error                                                                                               |
| 4    | Utility execution error                                                                                             |

> If there is at least one `Missing` among the results, code `1` is returned.
> If there is no `Missing`, but there is at least one `Inconclusive`, code `2` is returned.
> Code `0` is returned only when all found P/Invoke dependencies have the `Resolved` state.

### Usage

**Consyzer** is run from the command line (CLI) and requires two mandatory parameters:

1. `--AnalysisDirectory` — specifies the directory containing CIL modules to analyze;
2. `--SearchPatterns` — specifies the search patterns for CIL modules to analyze.

You can also specify two optional parameters:

1. `--RecursiveSearch` — indicates whether to search for CIL modules in nested directories. Default: `false`.
2. `--ReportFormats` — specifies the report output formats (`Console`, `Json`, `Csv`, `Xml`) as a comma-separated list. Default: `Console`.

### General usage pattern

Windows:

```powershell
Consyzer.exe --AnalysisDirectory <path_to_directory> --SearchPatterns <search_patterns> [--RecursiveSearch true|false] [--ReportFormats Console,Json,Csv,Xml]
```

Linux:

```bash
./Consyzer --AnalysisDirectory <path_to_directory> --SearchPatterns <search_patterns> [--RecursiveSearch true|false] [--ReportFormats Console,Json,Csv,Xml]
```

### Example

```powershell
Consyzer.exe --AnalysisDirectory C:\Modules --SearchPatterns "*.dll,*.exe" --RecursiveSearch true --ReportFormats Console,Json
```

```bash
./Consyzer --AnalysisDirectory ./modules --SearchPatterns "*.dll,*.exe" --RecursiveSearch true --ReportFormats Console,Json
```

## Analyzing multiple projects in a solution

You can use [this](./DevOps/Scripts/SolutionAnalyzer.ps1) PowerShell script to analyze the output artifacts of all projects in a solution.
This script can also be used in a **CI/CD pipeline**.
