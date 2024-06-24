# OptionsProvider
Enables loading configurations from files to manage options for experiments or flights.

Features:
* **Each *feature flag* is represented by a JSON or YAML file** which contains options to override default configuration values when processing feature names, flight names, or experiment names in a request.
Note that YAML support is still experimental and parsing may change.
* **Reads separate files in parallel** to keep independent configurations clear and easily maintainable.
* Supports clear file names and **aliases** for feature names.
* Uses the same logic that `ConfigurationBuilder` uses to load files so that it's easy to understand as it's the same as how `appsettings*.json` files are loaded.
* **Caching**: Built configuration objects are cached by default in `IMemoryCache` to avoiding rebuilding the same objects for the same feature names.

# Installation
```
dotnet add package OptionsProvider
```

See in [NuGet](https://www.nuget.org/packages/OptionsProvider#readme-body-tab).

# Example
Suppose you have a class that you want to use to configure your logic:
```csharp
internal class MyConfiguration
{
    public string[]? Array { get; set; }
    public MyObject? Object { get; set; }
}
```

You probably already use [Configuration in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration) and build an `IConfiguration` for to use in your service based on some default settings in an appsettings.json file.
Suppose you have an `appsettings.json` like this to configure `MyConfiguration`:
```json
{
    "config": {
        "array": [
            "default item 1"
        ],
        "object": {
            "one": 1,
            "two": 2
        }
    },
    "another config": {
        ...
    }
}
```

Now you want to start experimenting with different values deep within `MyConfiguration`.

Create a **new** folder for configurations files, for this example, we'll call it `Configurations` and add some files to it.
All `*.json`, `*.yaml`, and `*.yml` files in `Configurations` and any of its subdirectories will be loaded into memory.

`Configurations/feature_A.json`:
```json
{
    "metadata": {
        "aliases": [ "a" ],
        "owners": "a-team@company.com"
    },
    "options": {
        "config": {
            "array": [
                "example item 1"
            ]
        }
    }
}
```

`Configurations/feature_B/initial.yaml`:
```yaml
metadata:
    aliases:
        - "b"
    owners: "team-b@company.com"
options:
    config:
        array:
            - "different item 1"
            - "item 2"
        object:
            one: 11
            two: 22
            three: 3
```

When setting up your `IServiceCollection` for your service, do the following:
```csharp
services
    .AddOptionsProvider("Configurations")
    .ConfigureOptions<MyConfiguration>("config")
```

There are two simple ways to get the right version of `MyConfiguration` for the current request based on the enabled features.

## Using `IOptionsProvider` Directly

You can the inject `IOptionsProvider` into classes to get options for a given set of features.
Features names are not case-sensitive.

```csharp
using OptionsProvider;

class MyClass(IOptionsProvider optionsProvider)
{
    void DoSomething(...)
    {
        MyConfiguration options = optionsProvider.GetOptions<MyConfiguration>("config", ["A"]);
        // `options` be a result of merging the default values from appsettings.json, then applying Configurations/feature_A.json
        // because "a" is an alias for feature_A.json and aliases are case-insensitive.
    }
}
```

## Using `IOptionsSnapshot`

Alternatively, you can also use `IOptionsSnapshot<MyConfiguration>` and follow [.NET's Options pattern](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options).

When a request starts, set the feature names based on the enabled features in your system (for example, the enabled features could be passed in a request body or from headers):
```csharp
using OptionsProvider;

class MyController(IFeaturesContext context)
{
    public void InitializeContext(string[] enabledFeatures)
    {
        context.FeatureNames = enabledFeatures;
    }
}
```

Then while processing the request, `IFeaturesContext` will automatically be used to get the right configuration for the current request based on the enabled features.
To use this method, `MyConfiguration` must have public setters for all of its properties.

In your code, you can use `IOptionsSnapshot<MyConfiguration>` to get the right configuration for the current request based on the enabled features:
```csharp
class MyClass(IOptionsSnapshot<MyConfiguration> options)
{
    void DoSomething(...)
    {
        MyConfiguration options = options.Value;
    }
}
```

If `enabledFeatures` is `["A", "B"]`, then `MyConfiguration` will be built in this order:
1. Apply the default values the injected `IConfiguration`, i.e. the values from `appsettings.json` under `"config"`.
2. Apply the values from `Configurations/feature_A.json`.
3. Apply the values from `Configurations/feature_B/initial.yaml`.

## Caching
`["A", "B"]` is treated the same as `["a", "FeAtuRe_B/iNiTiAl"]` because using an alias is equivalent to using the path to the file and names and aliases are case-insensitive.
Both examples would retrieve the same instance from the cache and `IOptionsProvider` would return the same instance.
If `IOptionsSnapshot<MyConfiguration>` is used, then `MyConfiguration` will still only be built once and cached, but a different instance would be returned from `IOptionsSnapshot<MyConfiguration>.Value` for each scope because the options pattern creates a new instance each time.

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
api_key=<your NuGet API key>
cd src/OptionsProvider
dotnet pack --configuration Release
dotnet nuget push OptionsProvider/bin/Release/OptionsProvider.*.nupkg  --source https://api.nuget.org/v3/index.json -k $api_key --skip-duplicate
```
