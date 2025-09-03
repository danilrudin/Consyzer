param(
	[Parameter(Mandatory = $true, HelpMessage = "Path to Consyzer executable.")]
	[ValidateScript({ Test-Path $_ -PathType Leaf })]
	[string]$pathToConsyzer,

	[Parameter(Mandatory = $true, HelpMessage = "Path to the solution for analysis.")]
	[ValidateScript({ Test-Path $_ -PathType Container })]
	[string]$solutionForAnalysis,

	[Parameter(HelpMessage = "File extensions to scan for. Default is '*.exe, *.dll'.")]
	[string]$searchPatterns = "*.exe, *.dll",

	[Parameter(HelpMessage = "Build configuration to use. Default is 'Release'.")]
	[string]$buildConfiguration = "Release",

	[Parameter(HelpMessage = "Report output formats. Default is 'Console'.")]
	[string]$reportFormats = "Console",

	[Parameter(HelpMessage = "Recursive search for CIL modules. Default is false.")]
	[bool]$recursiveSearch = $false
)

Set-Location $solutionForAnalysis

# Construct platform-independent regex to match bin/<Configuration> folders
$regex = "bin[\\/]" + [Regex]::Escape($buildConfiguration) + "[\\/][^\\/]+$"

$analysisFolders = Get-ChildItem -Path . -Recurse -Directory |
	Where-Object { $_.FullName -match $regex } |
	Select-Object -ExpandProperty FullName -Unique

if (-not $analysisFolders) {
	Write-Warning "No build output folders found for analysis."
	# Exit with -3 (NoFilesFound) to match Consyzer's exit codes,
	# since no valid output folders (bin/<Configuration>) were found to analyze
	Exit -3
}

$finalExitCode = -5
$results = @()
$index = 0

foreach ($folder in $analysisFolders) {
    Write-Output ("[{0}] Analyzing:`n`t{1}" -f $index, $folder)
    Write-Output ""

    & $pathToConsyzer `
        --AnalysisDirectory $folder `
        --SearchPatterns $searchPatterns `
        --RecursiveSearch $recursiveSearch `
        --ReportFormats $reportFormats

    if ($LASTEXITCODE -gt $finalExitCode) {
        $finalExitCode = $LASTEXITCODE
    }

    $message = switch ($LASTEXITCODE) {
        -5 { "Error: No P/Invoke methods were found in the assemblies." }
        -4 { "Error: No valid files were found for analysis." }
        -3 { "Error: No files were found in the directory matching the search patterns." }
        -2 { "Error: No file search patterns were specified." }
        -1 { "Error: No analysis directory was specified." }
        0 { "Success: All libraries were found in the analyzed directory." }
        1 { "Warning: One or more libraries were found in the system directory." }
        2 { "Warning: One or more libraries were found via the PATH environment variable." }
        3 { "Warning: One or more libraries were found by absolute path." }
        4 { "Warning: One or more libraries were found by relative path." }
        5 { "Error: One or more libraries were not found in the system." }
        default { "Error: unexpected exit code ($LASTEXITCODE)." }
    }

    $results += [PSCustomObject]@{
        Index = $index
        Path = $folder
        ExitCode = $LASTEXITCODE
        Message = $message
    }

    Write-Output ""
    $index++
}

# Output analysis summary in YAML format for easier parsing in CI/CD pipelines
Write-Output "AnalysisSummary:"

foreach ($r in $results) {
    if ($r.Message -match "^(Error|Warning|Success): (.+)$") {
        $status = $matches[1]
        $msg = $matches[2]
    } else {
        $status = "Unknown"
        $msg = $r.Message
    }

    Write-Output ("  - Index: {0}" -f $r.index)
    Write-Output ("    ExitCode: {0}" -f $r.ExitCode)
    Write-Output ("    Path: {0}" -f $r.Path)
    Write-Output ("    Status: {0}" -f $status)
    Write-Output ("    Message: {0}" -f $msg)
    Write-Output ""
}

Write-Output ("Final exit code: {0}" -f $finalExitCode)
Exit $finalExitCode
