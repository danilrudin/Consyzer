# DevOps solutions

## Analyze all projects in a solution

Use [the PowerShell script](Scripts/SolutionAnalyzer.ps1) to run **Consyzer** on all built projects in a solution.  
This is especially useful in **CI/CD pipelines**.

### Example output

The script prints the analysis summary in **YAML format** for easier parsing in CI/CD pipelines:

```yaml
AnalysisSummary:
  - Index: 0
    ExitCode: 5
    Path: C:\Path\To\RepoRoot\Foo\bin\Release\net8.0
    Status: Error
    Message: One or more libraries were not found in the system.

  - Index: 1
    ExitCode: 2
    Path: C:\Path\To\RepoRoot\Baz\bin\Release\net8.0
    Status: Warning
    Message: One or more libraries were found via the PATH environment variable.
```

> The output can be parsed with `ConvertFrom-Yaml` in PowerShell or any standard YAML parser.

### Usage

```powershell
.\Scripts\SolutionAnalyzer.ps1 `
  -pathToConsyzer "C:\Tools\Consyzer.exe" `
  -solutionForAnalysis "C:\Path\To\RepoRoot" `
  -searchPatterns "*.exe, *.dll" `
  -recursiveSearch $false `
  -outputFormats "Console" `
  -buildConfiguration "Release"
```

Arguments:

- `-pathToConsyzer` – Full path to the **Consyzer** executable.
- `-solutionForAnalysis` – Path to the folder containing the built solution (e.g. the repository root).
- `-searchPatterns` – Comma-separated list of file patterns to scan. Default: `"*.exe, *.dll"`
- `-recursiveSearch` – Whether to search subdirectories for matching files. Default: `false`
- `-outputFormats` – Comma-separated list of report output formats. Default: `"Console"`
- `-buildConfiguration` – Build configuration folder to target. Default: `"Release"`

### Azure Pipelines

To use this in Azure Pipelines:

```yaml
- task: PowerShell@2
  inputs:
    targetType: "filePath"
    filePath: "$(Build.SourcesDirectory)/DevOps/Scripts/SolutionAnalyzer.ps1"
    arguments: >
      -pathToConsyzer "$(Build.BinariesDirectory)/Consyzer.exe"
      -solutionForAnalysis "$(Build.SourcesDirectory)"
      -searchPatterns "*.exe, *.dll"
      -recursiveSearch $false
      -outputFormats "Console"
      -buildConfiguration "Release"
  displayName: "Run Consyzer on all solution outputs"
```
