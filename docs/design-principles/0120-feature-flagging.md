# Feature Flagging

## Design Principles

* We want to be able to deploy code which includes features/code that we don't want visible/available/enabled for end users.
* We want to be able to progressively roll out certain features to specific users, or segments of the market to manage any risk of deploying new features
* This optionality can be attributed to all end-users, or specific end-users, or even all users within a specific tenant
* We want those features to be configured externally to the running system, without changing what has been deployed
* We want to have those flags managed separately to our system, so that we don't have to build this kind of infrastructure ourselves

## Implementation

We have provided a service called `IFeatureFlags` that is available in any component of the architecture.

> That service will be implemented by an adapter to a 3rd party external system such as FlagSmith, GitLab, LaunchDarkly etc.

We have also provided an API to access this capability from the BEFFE, so flags can be shared in the Frontend JS app.

The interface `IFeatureFlags` provides methods to query flags in the system, using pre-defined flags in the code, that should be represented in the 3rd party system.

For example,

```c#
public class MyClass
{
    private readonly IFeatureFlags _featureFlags;
    
    public MyClass(IFeatureFlags featureFlags)
    {
        _featureFlags = featureFlags;
    }
    
    public void DoSomethingForAllUsers()
    {
        if (_featureFlags.IsEnabled(Flag.MyFeature))
        {
            ...do somethign with this feature
        }
    }

    public void DoSomethingForTheCallerUser(ICallerContext caller)
    {
        if (_featureFlags.IsEnabled(Flag.MyFeature, caller))
        {
            ...do somethign with this feature
        }
    }
}
```

Where `MyFeature` is defined as a flag in `FeatureFlags.resx` file in the `Common` project.

### Defining flags

In code, flags are defined in the `FeatureFlags.resx` file in the `Common` project.

A source generator runs every build to translate those entries in the resource file to instances of the `Flags` class, to provide a typed collection of flags for use in code.

> This provides an easy way for intellisense to offer you the possible flags in the codebase to avoid using flags that no longer exist.