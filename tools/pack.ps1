$csproj = "$PSScriptRoot\..\Photino.Blazor\PhotinoX.Blazor.csproj"
$Configuration = "Release"
$outDir = $PSScriptRoot

dotnet clean $csproj -c $Configuration
dotnet build $csproj -c $Configuration
dotnet pack $csproj -c $Configuration -o $outDir