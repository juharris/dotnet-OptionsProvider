# OptionsProvider
Enables loading configurations from files to manage options for experiments or flights.

# Development
## Code Formatting
CI enforces:
```bash
dotnet format --verify-no-changes --severity info --no-restore src/*/*.sln
```

To automatically format code, run:
```bash
dotnet format --severity info --no-restore src/*/*.sln
```

## Publishing
From the dotnet folder in the root of the repo, run:
```bash
$api_key=<your NuGet API key>
cd src/OptionsProvider
dotnet pack --configuration Release
dotnet nuget push OptionsProvider/bin/Release/OptionsProvider.*.nupkg  --source https://api.nuget.org/v3/index.json -k $api_key --skip-duplicate
```
