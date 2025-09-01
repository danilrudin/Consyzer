# DevOps solutions

## Analyze all projects in a solution

Use [the PowerShell script](Scripts/SolutionAnalyzer.ps1) to run **Consyzer** on all built projects in a solution.  
This is especially useful in **CI/CD pipelines**.

### Usage

```powershell
.\Scripts\SolutionAnalyzer.ps1 `
  -pathToConsyzer "C:\Tools\Consyzer.exe" `
  -solutionForAnalysis "C:\Path\To\RepoRoot" `
  -searchPatterns "*.exe, *.dll" `
  -recursiveSearch $false `
  -outputFormat "Console" `
  -buildConfiguration "Release"
```

Arguments:

- `-pathToConsyzer` – Full path to the **Consyzer** executable.
- `-solutionForAnalysis` – Path to the folder containing the built solution (e.g. the repository root).
- `-searchPatterns` – Comma-separated list of file patterns to scan. Default: `"*.exe, *.dll"`
- `-recursiveSearch` – Whether to search subdirectories for matching files. Default: `false`
- `-outputFormat` – Output format(s) for reports. Supports multiple values separated by commas. Default: `"Console"`
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
      -outputFormat "Console"
      -buildConfiguration "Release"
  displayName: "Run Consyzer on all solution outputs"
```
