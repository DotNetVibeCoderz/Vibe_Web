$path = "C:\Users\mifma\Documents\CodeSandbox\WMSNet"
if (Test-Path $path) {
    cd $path
    Write-Host "Changed dir to $path"
    dotnet build
    dotnet ef migrations add InitialCreate
    dotnet ef database update
} else {
    Write-Host "Path $path not found."
    Get-Location
}