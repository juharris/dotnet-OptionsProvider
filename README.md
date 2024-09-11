# OptionsProvider
Enables loading configurations from JSON files, YAML files, or your own [`IConfigurationSource`s][custom-configuration-provider] to manage options for experiments.

Features:
* **Each *feature flag* can be represented by a JSON or YAML file** which contains options to override default configuration values when processing feature names or experiment names in a request.
Note that YAML support is still experimental and parsing may change.
* **Reads separate files in parallel** to keep independent configurations clear and easily maintainable.
* Supports clear file names and **aliases** for feature names.
* Uses the same logic that `ConfigurationBuilder` uses to load files so that it's easy to understand as it's the same as how `appsettings*.json` files are loaded.
* **Caching**: Built configuration objects are cached by default in `IMemoryCache` to avoiding rebuilding the same objects for the same feature names.

This project mainly focuses on supporting features backed by configurations in files with your source code because that's the most clear way for developers to see what values are supported for different configurable options.
Tools like [Azure App Configuration][azure-app-configuration] to control options externally while the service is running can be used with this library as this library accepts custom `IConfigurationSource`s and overrides the current default `IConfiguration` when given feature names.

# Installation
```
dotnet add package OptionsProvider
```

See more at [NuGet.org](https://www.nuget.org/packages/OptionsProvider#readme-body-tab).

# Example
Suppose you have a class that you want to use to configure your logic:
```csharp
internal sealed class MyConfiguration
{
    public string[]? Array { get; set; }
    public MyObject? Object { get; set; }
}
```

You probably already use [Configuration in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration) and build an `IConfiguration` to use in your service based on some default settings in an appsettings.json file.
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

_Note: Do not put default values directly in the class because they cannot be overriden to `null`.
Instead, put defaults in appsettings.json for clarity because it's easier to see them in configuration files instead of classes.
Default values in appsettings.json cannot be overridden to `null` either.
If a value needs to be set to `null` for a feature, then do not set a default in appsettings.json either._

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

## Preserving Configuration Files
To ensure that the latest configuration files are used when running your service or tests, you **may** need to ensure the `Configuration` folder gets copied to the output folder.
In your .csproj files with the `Configurations` folder, add a section like:
```csproj
<ItemGroup>
  <Content Include="Configurations/**">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

or there are already rule about including files, but the configuration file for a feature isn't found, you can try:
```csproj
<ItemGroup>
  <None Include="Configurations/**">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

## Configuration Building Examples
### Arrays/Lists
Note that `ConfigurationBuilder` does not concatenate lists, it merges them and overwrites entries because it treats each item in a list like a value in a dictionary indexed by the item's index.

For example, if the following features are applied:

`Configurations/feature_A.yaml`:
```yaml
options:
  config:
    array:
      - 1
      - 2
```

`Configurations/feature_B.yaml`:
```yaml
options:
  config:
    array:
      - 3
```

The resulting `MyConfiguration` for `["feature_A", "feature_B"]` will have `array` set to `[3, 2]` because the second list is applied 'on top of' the first list.
The builder views the lists as:

`array` from `Configurations/feature_A.yaml`:\
`array:0` = `1`\
`array:1` = `2`

`array` from `Configurations/feature_B.yaml`:\
`array:0` = `3`

| key | `feature_A` | `feature_B` | Resulting `array` |
|-|-|-|-|
| `array:0` | `1` | `3` | `3` |
| `array:1` | `2` | | `2` |

So the merged result is:\
`array:0` = `3`\
`array:1` = `2`

So `array` becomes `[3, 2]`.

For more details, see [here](https://stackoverflow.com/questions/67196795/configurationbuilder-does-not-override-node-when-adding-another-json-fill).

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

[azure-app-configuration]: https://learn.microsoft.com/en-us/azure/azure-app-configuration/
[custom-configuration-provider]: https://learn.microsoft.com/en-us/dotnet/core/extensions/custom-configuration-provider