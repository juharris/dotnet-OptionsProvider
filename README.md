# OptionsProvider
Enables loading configurations from JSON files, YAML files, or your own custom implementations of [`IConfigurationSource`][custom-configuration-provider] to manage options for experiments or different configurations of your service which can overlap or intersect.

![NuGet Version](https://img.shields.io/nuget/v/OptionsProvider)

This project helps code scale better and be easier to maintain.
We should determine the right configuration for a request or process when it starts by passing the enabled features to an `IOptionsProvider`.
The returned options would be used throughout the request or process to change business logic.
Supporting deep configurations with many types of properties instead of simple enabled/disabled feature flags is important to help avoid conditional statements (`if` statements) and thus help code scale and be more maintainable as explained in [this article][cond-blog].

It's fine to use systems that support enabled/disabled feature flags, but we'll inevitably need to support more sophisticated configurations.
This project facilitates using deep configurations to be the backing for simple feature flags, thus keeping API contracts clean and facilitating the refactoring of code that uses the configurations.
Allowing clients to know about and pass in deep configurations for specific components is hard to maintain and makes it difficult to change the structure of the configurations.

See [Optify](https://github.com/juharris/optify) for an implementations in Ruby, Rust, and more coming soon.
That repository is mainly for versions backed by the Rust implementation where arrays are not merged only entirely overwritten, as might be more expected in other languages and unlike .NET's `IConfiguration`.

Core Features:
* **Each *feature flag* can be represented by a JSON or YAML file** which contains options to override default configuration values when processing feature names or experiment names in a request.
Note that YAML support is still experimental and parsing may change.
* **Custom configuration sources**: Tools like [Azure App Configuration][azure-app-configuration] to control options externally while the service is running can be used with this library as this library accepts custom `IConfigurationSource`s and overrides the current default `IConfiguration` when given feature names.
This project mainly encourages using features backed by configurations in files with your source code because that's the most clear way for developers to see what values are supported for different configurable options.
* **Multiple features** can be enabled for the same request to support overlapping or intersecting experiments which are ideally mutually exclusive.
* **Reads separate files in parallel** when loading your configurations. Keeping configurations for each experiment in a separate file keeps your configurations independent, clear, and easily maintainable.
* Supports clear file names and **aliases** for feature names.
* Uses the same logic that `ConfigurationBuilder` uses to load and combine configurations for experiments so that it's easy to understand as because the same as how `appsettings*.json` files are loaded and overridden.
* **Caching**: Built configuration objects are cached by default in `IMemoryCache` to avoid rebuilding the same objects for the same feature names.
Caching options such as the lifetime of an entry can be configured using [`MemoryCacheEntryOptions`][MemoryCacheEntryOptions].

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
    public string[]? MyArray { get; set; }
    public MyObject? MyObject { get; set; }
}
```

You probably already use [Configuration in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration) and build an `IConfiguration` to use in your service based on some default settings in an appsettings.json file.
Suppose you have an `appsettings.json` like this to configure `MyConfiguration`:
```json
{
    "myConfig": {
        "myArray": [
            "default item 1"
        ],
        "myObject": {
            "one": 1,
            "two": 2
        }
    },
    "anotherConfig": {
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
Markdown files (ending in `.md`) are ignored.

Create `Configurations/feature_A.json`:
```json
{
    "metadata": {
        "aliases": [ "a" ],
        "owners": "a-team@company.com"
    },
    "options": {
        "myConfig": {
            "myArray": [
                "example item 1"
            ]
        }
    }
}
```

Create `Configurations/feature_B/initial.yaml`:
```yaml
metadata:
    aliases:
        - "b"
    owners: "team-b@company.com"
options:
    myConfig:
        myArray:
            - "different item 1"
            - "item 2"
        myObject:
            one: 11
            two: 22
            three: 3
```

When setting up your `IServiceCollection` for your service, do the following:
```csharp
services
    .AddOptionsProvider(path: "Configurations")
    .ConfigureOptions<MyConfiguration>(optionsKey: "myConfig")
```

There are a few simple ways to get the right version of `MyConfiguration` for the current request based on the enabled features.

## Using `IOptionsProvider` Directly
You can the inject `IOptionsProvider` into classes to get options for a given set of features.
Features names are not case-sensitive.

```csharp
using OptionsProvider;

class MyClass(IOptionsProvider optionsProvider)
{
    void DoSomething(...)
    {
        MyConfiguration options = optionsProvider.GetOptions<MyConfiguration>("myConfig", ["A"]);
        // `options` will be a result of merging the default values from appsettings.json, then applying Configurations/feature_A.json
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
    private void InitializeContext(string[] enabledFeatures)
    {
        // Set the enabled feature names for the current request.
        // This is also where custom filtering for the features can be done.
        // For example, one could filter out features that are not enabled for the current user or client application.
        context.FeatureNames = enabledFeatures;
    }
}
```

Then while processing the request, `IFeaturesContext` will automatically be used to get the right configuration for the current request based on the enabled features.

You can use `IOptionsSnapshot<MyConfiguration>` to get the right configuration for the current request based on the enabled features.
Example:
```csharp
class MyClass(IOptionsSnapshot<MyConfiguration> options)
{
    void DoSomething(...)
    {
        MyConfiguration options = options.Value;
    }
}
```

For this to work, `MyConfiguration` must have public setters for all of its properties, as shown above.

If `enabledFeatures` is `["A", "B"]`, then `MyConfiguration` will be built in this order:
1. Apply the default values the injected `IConfiguration`, i.e. the values from `appsettings.json` within `"myConfig"`.
2. Apply the values from `Configurations/feature_A.json`.
3. Apply the values from `Configurations/feature_B/initial.yaml`.

## Using a Builder
Using `IOptionsProviderBuilder` directly can be helpful if you are working in a large service and do not want to use `IOptionsProvider` as a global or shared singleton for all options.

Configurations can be loaded when setting up your DI container or in a constructor.
For example:
```csharp
internal sealed class MyProvider
{
    private readonly IOptionsProvider _optionsProvider;

    public MyProvider(IOptionsProviderBuilder builder)
    {
        _optionsProvider = builder
            .AddDirectory("Configurations")
            .Build();
    }
}
```

## Caching
`["A", "B"]` is treated the same as `["a", "FeAtuRe_B/iNiTiAl"]` because using an alias is equivalent to using the relative path to the file and names and aliases are case-insensitive.
Both examples would retrieve the same instance from the cache and `IOptionsProvider` would return the same instance.
If `IOptionsSnapshot<MyConfiguration>` is used, then `MyConfiguration` will still only be built once and cached, but a different instance would be returned from `IOptionsSnapshot<MyConfiguration>.Value` for each scope because the options pattern creates a new instance each time.

Caching options such as the lifetime of an entry can be configured using [`MemoryCacheEntryOptions`][MemoryCacheEntryOptions].

## Preserving Configuration Files
To ensure that the latest configuration files are used when running your service or tests, you **may** need to ensure the `Configuration` folder gets copied to the output folder.
In your .csproj files with the `Configurations` folder, add a section like:
```xml
<ItemGroup>
  <Content Include="Configurations/**">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

or if there are already rules about including files, but the latest configuration file for a feature is not found, you can try:
```xml
<ItemGroup>
  <None Include="Configurations/**">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

## Configuration Building Examples
### Overriding Values in Arrays/Lists
Note that .NET's `ConfigurationBuilder` does not concatenate lists, it merges them and overwrites entries because it treats each item in a list like a value in a dictionary indexed by the item's index.

For example, if the following features are applied:

`Configurations/feature_A.yaml`:
```yaml
options:
    myConfig:
        myArray:
            - 1
            - 2
```

`Configurations/feature_B.yaml`:
```yaml
options:
    myConfig:
        myArray:
            - 3
```

The resulting `MyConfiguration` for `["feature_A", "feature_B"]` will have `myArray` set to `[3, 2]` because the second list is applied after the first list.
The builder views the lists as:

`myArray` from `Configurations/feature_A.yaml`:\
`myArray:0` = `1`\
`myArray:1` = `2`

`myArray` from `Configurations/feature_B.yaml`:\
`myArray:0` = `3`

| key | `feature_A` | `feature_B` | Resulting `myArray` |
|-|-|-|-|
| `myArray:0` | `1` | `3` | `3` |
| `myArray:1` | `2` | | `2` |

So the merged result is:\
`myArray:0` = `3`\
`myArray:1` = `2`

So `myArray` becomes `[3, 2]`.

For more details, see [here](https://stackoverflow.com/questions/67196795/configurationbuilder-does-not-override-node-when-adding-another-json-fill).

### Overriding Values in Objects
Overriding entries in objects is more straightforward because the builder treats objects like dictionaries.
Values are overwritten if the same key is used in a feature that is applied later.
To delete a value for a key, one could set the value to `null` and then have custom logic in the service to ignore values that are `null`.

### Building Strings
Use [`ConfigurableString`][ConfigurableString] to customize string values using templates and slots that are recursively replaced with values.
For example:\
`"{{root}}"`\
➡️ `"{{greeting}}{{subject}}{{conclusion}}{{end}}"`\
➡️ `"Hello {{subject}}{{conclusion}}{{end}}"`\
➡️ `"Hello World!{{conclusion}}{{end}}"`\
➡️ `"Hello World! I hope you have a {{adjective}} day and enjoy yourself and your time.{{end}}"`\
➡️ `"Hello World! I hope you have a good day and enjoy yourself and your time.{{end}}"`\
➡️ `"Hello World! I hope you have a good day and enjoy yourself and your time."`

By default, `"{{"` and `"}}"` are used as delimiters for slots, but these can be customized as shown below.

This implementation uses simple string operations to build the string value because these simple operations should be sufficient for most cases.
More sophisticated implementations can use libraries like Fluid, Handlebars, Scriban, etc.
We do not want to add such dependencies by default to this mostly minimal library.
Perhaps extensions to this library could be published in the future.
It's important to build strings that may be customized in a way that fosters collaboration, otherwise, it is too tempting to copy long strings and make small changes to a specific part which can lead to a lot of duplication and maintenance issues.
Similar strings would end up in many places, resulting in bifurcation of important parts, making it difficult to update many strings when a change is needed.

Example:
```csharp
internal sealed class MyConfiguration
{
    public ConfigurableString? MyString { get; set; }
}
```

In a configuration file, `default.yaml`:
```yaml
options:
    myConfig:
        myString:
            template: "{{root}}"
            values:
                root: "{{greeting}}{{subject}}{{conclusion}}{{end}}"
                greeting: "Hello "
                subject: "World!"
                conclusion: " I hope you have a good day and enjoy yourself and your time."
                end: ""
```

The resulting value for `MyString.Value` will be: `"Hello World! I hope you have a good day and enjoy yourself and your time."`.

To override only the subject, `subject_everyone.yaml`:
```yaml
options:
    myConfig:
        myString:
            values:
                subject: "Everyone!"
```

The resulting value for `MyString.Value` with the features `["default", "subject_everyone"]` enabled will be: `"Hello Everyone! I hope you have a good day and enjoy yourself and your time."`.

To quickly experiment with a full raw string, the most robust way is set the value for the slot `"{{root}}"` to the raw string in a new configuration file, `raw_string.yaml` (note that this is not very collaborative as explained below in the Best Practices section):
```yaml
options:
    myConfig:
        myString:
            values:
                root: "Hello World! I hope you have a good day and enjoy yourself and your time."
```

For convenience, another way to quickly experiment with a new value for `myString` is to override its value in a new configuration file, `raw_string.yaml` (note that this is not very collaborative as explained below in the Best Practices section):
```yaml
options:
    myConfig:
        myString: "{{greeting}} This is a raw string and no replacements will be done."
```

The resulting value for `MyString.Value` with the features `["default", "raw_string"]` enabled will be: `"{{greeting}} This is a raw string and no replacements will be done."`.
Again, this is just for convenience for quick experimentation and not collaborative because in this case `myString` cannot be further configured by applying new features to the list of enabled features once the `"raw_string"` feature is in the list.

Delimiters can be customized. Example:
```yaml
options:
    myConfig:
        myString:
            template: "<root>"
            values:
                root: "<greeting><subject><introduction><conclusion><end>"
                introduction: " I am the app."
                startDelimiter: "<",
                endDelimiter: ">",
```

The resulting value for `MyString.Value` will be: `"Hello World! I am the app. I hope you have a good day and enjoy yourself and your time."`.

> [!NOTE]
> This implementation to build strings is meant to be a simple implementation to handle most cases.
It is not meant to handle every type of edge case with every possible delimiter.
It is not meant to handle complex cases like other libraries such as Fluid, Handlebars, Scriban, etc. might handle.
We may change and optimization the logic to suit typical cases, but anyone relying on odd behavior such as the delimiters within the delimiters (`"{{slotA{{slotB}}}}"`) may not get consistent results in a backwards compatible way after library updates.
In some cases we may add options to configure the replacement logic so that the old behavior can be enabled again.

Another simple way to build a string could be to concatenate values from an array or dictionary, but this is not recommended for strings that many configurations would want to customize because it would be difficult to maintain since other files will need to be cross-referenced much more in order to understand the order that values might be used.

#### Best Practices for Collaboratively Building Strings
* The default template should not have literal values.
This makes it easy to completely override the entire string by overriding the value for the key `"root"` for quick experimentation of a proof of concept.

* Use a slot with a value of an empty string, `""`, to replace the slot with nothing.\
\
For example, if we set `"conclusion": ""`, then `"{{greeting}}{{subject}}{{conclusion}}"` will become `"Hello World!"`.

* Use a slot with a value of `null` to imitate deleting a slot from the values and ensure that the slot will not be replaced.\
\
For example, if we set `"conclusion": null`, then `"{{greeting}}{{subject}}{{conclusion}}"` will become `"Hello World!{{conclusion}}"`.

* If we want to experiment with changing a small part of a value for a slot, then **DO NOT** override the entire value and only change that one small part in your configuration because this will make maintaining such long copied strings across many files difficult, the "copypasta" problem.
Inevitably, the same long string will be copied and modified in many places, leading to bifurcation of important parts and making it difficult to update many strings when a change to one part is needed, for example, after a successful experiment with changing another part of the string.
It needs to be seamless to update the default value for most of the string that is not desired to be changed for every experiment.\
\
**Solution**: convert the specific part that needs to be modified to a slot,
set the default value for that slot to the current value,
and then override that new slot in another file.\
\
For example, to experiment with changing `"good"` to `"great"` in `conclusion: " I hope you have a good day and enjoy yourself and your time."`, change the default configuration to:\
In `default.yaml`:
    ```yaml
    options:
        myConfig:
            myString:
                ...
                values:
                    ...
                    conclusion: " I hope you have a {{adjective}} day and enjoy yourself and your time."
                    adjective: "good"
    ```

    Then in a new configuration, `great_feature.yaml`:
    ```yaml
    options:
        myConfig:
            myString:
                values:
                    adjective: "great"
    ```

    Then to use `"great"` in the string, enable the features `["default", "great_feature"]`.
    So that "default" is applied first as a base, then "great_feature" is applied to override the adjective to "great".

    Of course, now you should probably also convert `"a"` to a slot since it might need to overridden to `"an"` if the adjective starts with a vowel.

* Use JSON files for managing large configurations that many may want to customize because it is easier to see the desired structure, validate the structure, and automatically format the structure.
It is also easier to resolve merge conflicts in JSON files because the format can be validated easily.
Using YAML files is fine for short configurations for a couple of values or configurations that are not expected to be customized often.

* Unicode in YAML files: Use a later version of `YamlDotNet` in your project, for example: `<PackageReference Include="YamlDotNet" Version="16.2.1" />`.
For example, emojis and other characters in Unicode might not work well in YAML files by default because this library only requires an old version of `YamlDotNet` in order be compatible with older projects.
Alternatively, use JSON files for managing strings beyond ASCII characters.

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
[cond-blog]: https://medium.com/@justindharris/conditioning-code-craft-clear-and-concise-conditional-code-f4f328c43c2b
[ConfigurableString]: ./src/OptionsProvider/OptionsProvider/String/ConfigurableString.cs
[custom-configuration-provider]: https://learn.microsoft.com/en-us/dotnet/core/extensions/custom-configuration-provider
[MemoryCacheEntryOptions]: https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.memorycacheentryoptions
