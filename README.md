[![Build Status](https://github.com/danilrudin/Consyzer/workflows/Build/badge.svg)](https://github.com/danilrudin/Consyzer/actions/workflows/build.yml) [![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=danilrudin_Consyzer&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=danilrudin_Consyzer) [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=danilrudin_Consyzer&metric=coverage)](https://sonarcloud.io/summary/new_code?id=danilrudin_Consyzer) [![GitHub license](https://img.shields.io/github/license/danilrudin/Consyzer)](https://github.com/danilrudin/Consyzer/blob/master/LICENSE)

This README is also available in the following languages:

- [Русский](./Docs/README-RU.md)

## Overview

**Consyzer** is a CLI utility designed to prevent CIL module consistency issues when using P/Invoke mechanisms to call methods implemented outside the managed CLR environment.

## Purpose

In CIL application development, it is not uncommon to need access to methods implemented outside the managed .NET ecosystem. In the source code of a CIL module, such calls are described using **DllImport** or **LibraryImport** attributes and are stored in the module's metadata after compilation, indicating which unmanaged (native) library should be loaded at runtime and which function should be called from it.

A key feature of such calls is that the function code from the unmanaged library is not linked directly with the CIL module's code;
instead, the module's metadata stores information about the function being called, including a reference to the expected location of the unmanaged library containing the implementation of that function on the system.

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

The application functions correctly without compromising system integrity and security as long as all unmanaged libraries are present at the locations described in the metadata;
however, if even one of the libraries is missing, the application will not only crash but may also pose a security risk.

Consyzer was created to ensure that such situations do not come as a surprise.

## How it works

1. Consyzer selects files for analysis based on the specified directory and search patterns;
2. Consyzer logs and excludes any files that are not ECMA-355 assemblies from the analysis;
3. Consyzer analyzes the remaining ECMA assemblies for the presence of P/Invoke methods;
4. Consyzer analyzes each found P/Invoke method and checks whether the corresponding native libraries exist in the system;
5. Consyzer generates a report based on the analysis results in one or more formats depending on the configuration;
6. Consyzer returns an exit code indicating the specific analysis result, which also enables you to handle analysis incidents in accordance with your requirements.

> ⚠️ The analysis is based on the metadata stored in CIL assemblies and does not evaluate the correctness of marshaling between managed and unmanaged code.

## Analysis Results

**Consyzer** presents analysis results in the form of reports.  
The following report formats are supported:

1. `Console`
2. `Json`
3. `Csv`
4. `Xml`

### Example report (Console)

```
[AssemblyMetadataList]
    [0]
        File: Foo.dll
        Version: 1.0.0.0
        CreationDateUtc: 21.06.2025 12:00:00
        Sha256: ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890
    [1]
        File: Bar.dll
        Version: 2.1.3.0
        CreationDateUtc: 22.06.2025 15:30:00
        Sha256: 1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF
    [2]
        File: Baz.dll
        Version: 1.2.0.0
        CreationDateUtc: 23.06.2025 10:45:00
        Sha256: FEDCBA0987654321FEDCBA0987654321FEDCBA0987654321FEDCBA0987654321
[PInvokeMethodGroups]
    [0] File: Foo.dll, Found: 2
        [0]
            Signature: 'Int32 static Native.Foo.DoStuff()'
            ImportName: 'existentlib.dll'
            ImportFlags: 'CallingConventionCDecl'
        [1]
            Signature: 'Void static Native.Foo.FailStuff(String)'
            ImportName: 'missinglib.dll'
            ImportFlags: 'CallingConventionStdCall'
    [1] File: Baz.dll, Found: 1
        [0]
            Signature: 'Boolean static .Baz.CheckSomething(Int32)'
            ImportName: 'anotherlib.dll'
            ImportFlags: 'CallingConventionStdCall'
[LibraryPresences]
    [0]
        LibraryName: existentlib.dll
        ResolvedPath: C:\Windows\System32\existentlib.dll
        LocationKind: InSystemDirectory
    [1]
        LibraryName: missinglib.dll
        ResolvedPath: null
        LocationKind: Missing
    [2]
        LibraryName: anotherlib.dll
        ResolvedPath: C:\EnvPath\anotherlib.dll
        LocationKind: InEnvironmentPath
[Summary]
    TotalFiles: 3
    EcmaAssemblies: 3
    AssembliesWithPInvoke: 2
    TotalPInvokeMethods: 3
    MissingLibraries: 1
```

### Exit Codes

**Consyzer** returns a specific exit code depending on where the native libraries specified in P/Invoke attributes were or were not found:

| Code | Meaning                                                                |
| ---- | ---------------------------------------------------------------------- |
| 0    | All libraries were found in the analyzed directory                     |
| 1    | One or more libraries were found in the system directory               |
| 2    | One or more libraries were found via the **PATH** environment variable |
| 3    | One or more libraries were found by absolute path                      |
| 4    | One or more libraries were found by relative path                      |
| 5    | One or more libraries were not found in the system                     |

> The analysis codes match the order in which **Consyzer** searches for libraries in the system.  
> If libraries are found in different locations, the highest corresponding code is returned.  
> Only the last code means that at least one library was not found.

---

| Code | Configuration or execution error                                  |
| ---- | ----------------------------------------------------------------- |
| -1   | No analysis directory was specified                               |
| -2   | No file search patterns were specified                            |
| -3   | No files were found in the directory matching the search patterns |
| -4   | No valid files were found for analysis                            |
| -5   | No P/Invoke methods were found in the assemblies                  |

> Negative codes signal configuration errors or failures that occurred during the utility’s execution.  
> The lower the code, the **further the utility progressed before the error occurred**.

## Usage

**Consyzer** is run from the command line (CLI) and requires two mandatory parameters:

1. `--AnalysisDirectory` — specifies the directory containing CIL modules to analyze;
2. `--SearchPatterns` — specifies the search patterns for CIL modules to analyze.

You can also specify two optional parameters:

1. `--RecursiveSearch` — specifies whether to search for CIL modules in subdirectories. Default: `false`.
2. `--OutputFormat` — specifies the report output format (`Console`, `Json`, `Csv`, `Xml`). Multiple values supported via comma. Default: `Console`.

### General usage pattern

```
Consyzer.exe --AnalysisDirectory <path_to_directory> --SearchPatterns <search_patterns> [--RecursiveSearch true|false] [--OutputFormat Console, Json, Csv, Xml]
```

### Example

```
Consyzer.exe --AnalysisDirectory C:\Modules --SearchPatterns "*.dll, *.exe" --RecursiveSearch true --OutputFormat Console, Json
```

## Analyzing multiple projects in a solution

You can use [this](./DevOps/Scripts/SolutionAnalyzer.ps1) PowerShell script to analyze the output artifacts of all projects in a solution.

This script can also be used in a **CI/CD pipeline**.
