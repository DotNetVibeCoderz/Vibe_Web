
if (Test-Path "WMSNet.csproj") {
    Write-Host "Found csproj in current dir"
} elseif (Test-Path "WMSNet/WMSNet.csproj") {
    Write-Host "Found csproj in subfolder, changing dir"
    cd WMSNet
} else {
    Write-Host "Csproj not found!"
    Get-ChildItem
}

dotnet build
dotnet ef migrations add InitialCreate
dotnet ef database update