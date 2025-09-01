param(
    [Parameter(Mandatory = $true, HelpMessage = "Path to Consyzer executable.")]
    [ValidateScript({ Test-Path $_ -PathType Leaf })]
    [string]$pathToConsyzer,

    [Parameter(Mandatory = $true, HelpMessage = "Path to the solution for analysis.")]
    [ValidateScript({ Test-Path $_ -PathType Container })]
    [string]$solutionForAnalysis,

    [Parameter(HelpMessage = "File extensions to scan for. Default is '*.exe, *.dll'.")]
    [string]$searchPattern = "*.exe, *.dll",

    [Parameter(HelpMessage = "Build configuration to use. Default is 'Release'.")]
    [string]$buildConfiguration = "Release",

    [Parameter(HelpMessage = "Report output format. Default is 'Console'.")]
    [string]$outputFormat = "Console",

    [Parameter(HelpMessage = "Recursive search for CIL modules. Default is false.")]
    [bool]$recursiveSearch = $false
)

Set-Location $solutionForAnalysis

# Construct platform-independent regex to match bin/<Configuration> folders
$regex = [Regex]::Escape("bin/$buildConfiguration").Replace("/", "[/\\\\]")

$analysisFolders = Get-ChildItem -Path . -Recurse -Directory |
    Where-Object {
        $_.FullName -match $regex
    } |
    Select-Object -ExpandProperty FullName -Unique

if ($analysisFolders.Length -eq 0) {
    Write-Warning "No binary folders were found for analysis."
    # Exit with -3 to match Consyzer's code for "no files matched the search pattern",
    # since no valid output folders (bin/Release) were found to analyze
    Exit -3
}

$finalExitCode = -4
$messageBuilder = New-Object System.Text.StringBuilder

foreach ($folder in $analysisFolders) {
    & $pathToConsyzer `
        --AnalysisDirectory $folder `
        --SearchPattern     $searchPattern `
        --RecursiveSearch   $recursiveSearch `
        --OutputFormat      $outputFormat

    if ($LASTEXITCODE -ge $finalExitCode) {
        $finalExitCode = $LASTEXITCODE
    }

    $message = switch ($LASTEXITCODE) {
        -5 { "Error: $folder → no P/Invoke methods were found in the assemblies." }
        -4 { "Error: $folder → none of the files were valid for analysis." }
        -3 { "Error: $folder → no files matched the search pattern." }
        -2 { "Error: $folder → no search pattern was specified." }
        -1 { "Error: $folder → no analysis directory was specified." }
         0 { "Success: $folder → all libraries found in the analysis directory." }
         1 { "Warning: $folder → one or more libraries were found in the system directory." }
         2 { "Warning: $folder → one or more libraries were found via PATH." }
         3 { "Warning: $folder → one or more libraries were found via absolute path." }
         4 { "Warning: $folder → one or more libraries were found via relative path." }
         5 { "Error: $folder → one or more libraries are missing from the system." }
        default { "Unknown exit code ($LASTEXITCODE) for $folder." }
    }

    $messageBuilder.AppendLine($message)
    Write-Output "`nConsyzer exit code: $LASTEXITCODE`n"
}

Write-Output $messageBuilder.ToString()
Exit $finalExitCode