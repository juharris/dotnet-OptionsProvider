# OptionsProvider
Enables loading configurations from files to manage options for experiments or flights.

Features:
* Load files which contain options to override configuration values when processing feature names, flight names, or experiment names in a request.
* Use clear files in the repository.
* Use separate files to keep independent configurations clear and easily maintainable.
* Use the same logic that `ConfigurationBuilder` uses to load files so that it's easy to understand as it's the same as how `appsettings*.json` files are loaded.

<!-- TODO Add examples, DI examples, notes about disposal for the IMemoryCache. -->

# Example
Create a new folder for configurations files, for this example, we'll call it "Configurations".

<!-- TODO Add more details. -->

When setting up your `IServiceCollection`, do the following:

```csharp
services
    // ...
    .AddOptionsProvider("Configurations")
```
<!-- TODO Add details. -->

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
