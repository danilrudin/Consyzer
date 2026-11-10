# DevOps solutions

## Analyze all projects in a solution

Use [the PowerShell script](Scripts/SolutionAnalyzer.ps1) to run **Consyzer** on all built projects in a solution.  
This is especially useful in **CI/CD pipelines**.

### Example output

The script prints the analysis summary in **YAML format** for easier parsing in CI/CD pipelines:

```yaml
AnalysisSummary:
  - Index: 0
    ExitCode: 1
    Path: C:\Path\To\RepoRoot\Foo\bin\Release\net8.0
    Status: Error
    Message: One or more libraries are missing.

  - Index: 1
    ExitCode: 2
    Path: C:\Path\To\RepoRoot\Baz\bin\Release\net8.0
    Status: Warning
    Message: One or more libraries were not found by checked mechanisms, but may be found by non-simulated OS loading mechanisms.
```

> The output can be parsed with `ConvertFrom-Yaml` in PowerShell or any standard YAML parser.

### Exit code aggregation

The script runs **Consyzer** separately for each discovered build output folder and then returns a single final exit code.

Final exit code priority:

1. `4` — utility execution error;
2. `3` — input parameter error;
3. `1` — one or more libraries are missing;
4. `2` — no missing libraries, but one or more results are inconclusive;
5. `0` — all libraries were found through supported search mechanisms.

This priority keeps the script aligned with **Consyzer** analysis semantics: `Missing` has priority over `Inconclusive`, even though its numeric code is lower.

### Usage

```powershell
.\Scripts\SolutionAnalyzer.ps1 `
  -ConsyzerPath "C:\Tools\Consyzer.exe" `
  -SolutionPath "C:\Path\To\RepoRoot" `
  -BuildConfiguration "Release" `
  -SearchPatterns "*.exe, *.dll" `
  -RecursiveSearch $false `
  -ReportFormats "Console"
```

Arguments:

- `-ConsyzerPath` – Full path to the **Consyzer** executable
- `-SolutionPath` – Path to the folder containing the built solution (e.g. the repository root)
- `-BuildConfiguration` – Build configuration folder to target. Default: `"Release"`
- `-SearchPatterns` – Comma-separated list of file patterns to scan. Default: `"*.exe, *.dll"`
- `-RecursiveSearch` – Whether to search subdirectories for matching files. Default: `false`
- `-ReportFormats` – Comma-separated list of report output formats. Default: `"Console"`

### Azure Pipelines

To use this in Azure Pipelines, add the following step **after building the solution**:

```yaml
- task: PowerShell@2
  inputs:
    targetType: "filePath"
    filePath: "C:/tools/Consyzer/Scripts/SolutionAnalyzer.ps1"
    arguments: >
      -ConsyzerPath "C:/tools/Consyzer/Consyzer.exe"
      -SolutionPath "$(Build.SourcesDirectory)"
      -BuildConfiguration "Release"
      -SearchPatterns "*.exe, *.dll"
      -RecursiveSearch $false
      -ReportFormats "Console"
  displayName: "Run Consyzer on all solution outputs"
```
