param(
	[Parameter(Mandatory = $true, HelpMessage = "Path to Consyzer executable.")]
	[ValidateScript({ Test-Path $_ -PathType Leaf })]
	[string]$ConsyzerPath,

	[Parameter(Mandatory = $true, HelpMessage = "Path to the solution for analysis.")]
	[ValidateScript({ Test-Path $_ -PathType Container })]
	[string]$SolutionPath,

	[Parameter(HelpMessage = "Build configuration to use. Default is 'Release'.")]
	[string]$BuildConfiguration = "Release",

	[Parameter(HelpMessage = "File extensions to scan for. Default is '*.exe, *.dll'.")]
	[string]$SearchPatterns = "*.exe, *.dll",

	[Parameter(HelpMessage = "Recursive search for CIL modules. Default is false.")]
	[bool]$RecursiveSearch = $false,

	[Parameter(HelpMessage = "Report output formats. Default is 'Console'.")]
	[string]$ReportFormats = "Console"
)

Set-Location $SolutionPath

# Construct platform-independent regex to match bin/<Configuration> folders
$regex = "bin[\\/]" + [Regex]::Escape($BuildConfiguration) + "[\\/][^\\/]+$"

$analysisFolders = Get-ChildItem -Path . -Recurse -Directory |
	Where-Object { $_.FullName -match $regex } |
	Select-Object -ExpandProperty FullName -Unique

if (-not $analysisFolders) {
	Write-Warning "No build output folders found for analysis."
	Exit 3
}

$knownExitCodes = @(0, 1, 2, 3, 4)
$exitCodes = @()
$results = @()
$index = 0

foreach ($folder in $analysisFolders) {
	Write-Output ("[{0}] Analyzing:`n`t{1}" -f $index, $folder)
	Write-Output ""

	& $ConsyzerPath `
		--AnalysisDirectory $folder `
		--SearchPatterns $SearchPatterns `
		--RecursiveSearch $RecursiveSearch `
		--ReportFormats $ReportFormats

	$exitCode = $LASTEXITCODE
	$effectiveExitCode = $exitCode

	if ($exitCode -notin $knownExitCodes) {
		$effectiveExitCode = 4
	}

	$exitCodes += $effectiveExitCode

	$message = switch ($effectiveExitCode) {
		0 { "Success: All libraries were found through supported search mechanisms." }
		1 { "Error: One or more libraries are missing." }
		2 { "Warning: One or more libraries were not found by checked mechanisms, but may be found by non-simulated OS loading mechanisms." }
		3 { "Error: Input parameter error." }
		4 {
			if ($exitCode -in $knownExitCodes) {
				"Error: Utility execution error."
			}
			else {
				"Error: Unexpected Consyzer exit code ($exitCode)."
			}
		}
	}

	$results += [PSCustomObject]@{
		Index = $index
		Path = $folder
		ExitCode = $exitCode
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

if ($exitCodes -contains 4) {
	$finalExitCode = 4
}
elseif ($exitCodes -contains 3) {
	$finalExitCode = 3
}
elseif ($exitCodes -contains 1) {
	$finalExitCode = 1
}
elseif ($exitCodes -contains 2) {
	$finalExitCode = 2
}
else {
	$finalExitCode = 0
}

Write-Output ("Final exit code: {0}" -f $finalExitCode)
Exit $finalExitCode
