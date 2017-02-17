if(Test-Path .\artifacts) { Remove-Item .\artifacts -Force -Recurse }

# Evaluate next version based on AppVeyor build version
$actual_version = "$env:APPVEYOR_BUILD_VERSION"
Write-Host "Set version to $actual_version"

# Set version on project files
ls */*/project.json | foreach { echo $_.FullName} |
foreach {
    $content = get-content "$_"
    $content = $content.Replace("99.99.99", $actual_version)
    set-content "$_" $content -encoding UTF8
}

& dotnet restore
if ($LASTEXITCODE -ne 0)
{
    throw "dotnet restore failed with exit code $LASTEXITCODE"
}

& dotnet test .\test\StrangerData.UnitTests -c Release
if ($LASTEXITCODE -ne 0)
{
    throw "dotnet test failed with exit code $LASTEXITCODE"
}

& dotnet pack .\src\StrangerData -c Release -o .\artifacts
& dotnet pack .\src\StrangerData.SqlServer -c Release -o .\artifacts

# Rollback version on project files
ls */*/project.json | foreach { echo $_.FullName} |
foreach {
    $content = get-content "$_"
    $content = $content.Replace($actual_version, "99.99.99")
    set-content "$_" $content -encoding UTF8
}