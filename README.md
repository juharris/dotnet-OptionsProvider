# OptionsProvider
Enables loading configurations from files to manage options for experiments or flights.

Features:
* Load files which contain options to override configuration values when processing feature names, flight names, or experiment names in a request.
* Use clear files in the repository.
* Use separate files to keep independent configurations clear and easily maintainable.
* Use the same logic that `ConfigurationBuilder` uses to load files so that it's easy to understand as it's the same as how `appsettings*.json` files are loaded.

# Example
Suppose you have a configuration class that you want to use to configure your logic:
```csharp
internal class MyConfiguration
{
    public string[]? Array { get; set; }
    public MyObject? Object { get; set; }
}
```

Create a new folder for configurations files, for this example, we'll call it "Configurations".

Create some configuration files.

Configurations/feature_A.json:
```json
{
    "metadata": {
        "aliases": [ "a" ],
        "owners": "justin@site.com"
    },
    "options": {
        "config": {
            "array": [
                "example item 1"
            ],
            "object": {
                "one": 1,
                "two": 2
            }
        }
    }
}
```

Configurations/feature_B.json:
```json
{
    "metadata": {
        "aliases": [ "b" ],
        "owners": "justin@site.com"
    },
    "options": {
        "config": {
            "array": [
                "different item 1",
                "item 2"
            ],
            "object": {
                "one": 11,
                "two": 22,
                "three": 3,
            }
        }
    }
}
```

When setting up your `IServiceCollection`, do the following:
```csharp
services
    // ...
    .AddOptionsProvider("Configurations")
    .ConfigureOptions<MyConfiguration>("config")
```

You can the inject `IOptionsProvider` into classes to get options for a given set of features.
Features names are not case-sensitive.

```csharp
using OptionsProvider;

class MyClass(IOptionsProvider optionsProvider)
{
    void DoSomething(...)
    {
        MyConfiguration options = optionsProvider.GetOptions<MyConfiguration>("config", ["A"]);
        // `options` will contain the values from Configurations/feature_A.json
        // because "a" is an alias for feature_A.json and aliases are case-insensitive.
    }
}
```

You can also use `IOptionsSnapshot<MyConfiguration>` and follow [.NET's Options pattern](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options).

When a request starts, set the feature names based on the enabled features in your system (for example, the enabled features could be passed in a request body or from headers):
```csharp
class MyController(IFeaturesContext context)
{
    public void InitializeContext(string[] enabledFeatures)
    {
        context = enabledFeatures;
    }
}
```


```csharp
class MyClass(IOptionsSnapshot<MyConfiguration> options)
{
    void DoSomething(...)
    {
        // Get the right configuration for the current request based on the enabled features.
        MyConfiguration options = options.Value;
    }
}
```

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
