# com.juahn.v2.vcontainer

A bridge/adapter between [VContainer](https://github.com/hadashiA/VContainer) (hadashiA's DI
container) and JuahnFrameworkV2's own DI abstraction (`com.juahn.v2.di`).

## Purpose

JuahnFrameworkV2 ships a small provider-agnostic DI abstraction:

- `IServiceLocator` - read side: `Resolve<T>()`, `TryResolve<T>(out T)`
- `IServiceRegistry` - write side: `Bind` / `BindSelf` / `BindFactory` / `Clean`
- `IServiceInstaller` - a unit of binding logic (`InstallBindings(IServiceRegistry)`)
- `ServiceLocator` / `ServiceContainer` - the built-in default implementation

This package lets a project that has **standardized on VContainer** keep depending on the
framework's `IServiceLocator` abstraction while VContainer does the actual resolving. Framework
code stays provider-agnostic; the DI backend becomes VContainer.

**This is opt-in and separate.** The built-in `ServiceLocator` / `ServiceContainer` remain intact
and are unaffected. Only add this package if you actually use VContainer.

### When to use this vs. the built-in ServiceLocator

| Situation | Use |
| --- | --- |
| Project already uses VContainer for DI | This bridge (`com.juahn.v2.vcontainer`) |
| No DI container / want the framework default | Built-in `ServiceLocator` + `ServiceBootstrapper` (`com.juahn.v2.di`) |

## Requirement: install VContainer yourself

VContainer is **not** bundled and is **not** a UPM dependency of this package (the assembly
references the `VContainer` assembly by name). The consumer must install it, e.g.:

- OpenUPM: `jp.hadashikick.vcontainer`
- Git URL:
  `https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer`

If VContainer is not present, this package's assembly (`juahn.v2.VContainer`) will fail to compile
because its `VContainer` assembly reference is unresolved.

## What's provided

- **`VContainerServiceLocator : IServiceLocator`** - wraps a VContainer `IObjectResolver`.
  `Resolve<T>` / `TryResolve<T>` delegate to the resolver. Any framework code depending on
  `IServiceLocator` now runs on a VContainer backend.
- **`ContainerBuilderServiceRegistry : IServiceRegistry`** - adapts a VContainer `IContainerBuilder`
  so existing `IServiceInstaller`s (which bind into `IServiceRegistry`) register into VContainer
  unchanged. Each `Bind`/`BindSelf` maps to `RegisterInstance(...).As<...>()`/`.AsSelf()`;
  `BindFactory` maps to a lazily-resolved singleton. Registration-only: `Resolve`/`Clean` are not
  available during configuration (resolve via `VContainerServiceLocator` after the scope is built).
- **`JuahnVContainerExtensions`** - `IContainerBuilder.RegisterJuahnServices(params IServiceInstaller[])`
  and `IContainerBuilder.RegisterJuahnServiceLocator()`.
- **`JuahnVContainerScope : LifetimeScope`** - a ready-made scope that registers the locator and
  applies installers; the VContainer analogue of the built-in `ServiceBootstrapper`.

## Usage

### Option A - extension methods in your own LifetimeScope

```csharp
using Juahn.V2.Services;
using Juahn.V2.VContainer;
using VContainer;
using VContainer.Unity;

public sealed class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // Expose the framework's IServiceLocator, backed by this VContainer scope.
        builder.RegisterJuahnServiceLocator();

        // Apply existing framework installers into VContainer.
        builder.RegisterJuahnServices(
            new AudioInstaller(),
            new SaveSystemInstaller());

        // ... your own VContainer registrations ...
    }
}
```

### Option B - subclass the provided scope

```csharp
using System.Collections.Generic;
using Juahn.V2.Services;
using Juahn.V2.VContainer;

public sealed class GameLifetimeScope : JuahnVContainerScope
{
    protected override IEnumerable<IServiceInstaller> CollectInstallers()
    {
        yield return new AudioInstaller();
        yield return new SaveSystemInstaller();
    }
}
```

### Resolving through the abstraction

```csharp
// Injected by VContainer, or resolved from the scope's Container:
IServiceLocator locator = scope.Container.Resolve<IServiceLocator>();

var audio = locator.Resolve<IAudioService>();

if (locator.TryResolve<IOptionalService>(out var optional))
{
    optional!.DoThing();
}
```

## How it fits the framework

`com.juahn.v2.di` defines the DI contracts and a built-in implementation. This package is a thin
adapter layer that plugs those same contracts into VContainer, so downstream framework packages and
game code depend only on `IServiceLocator` and never on a concrete container. Swap the backend by
choosing which bootstrapper/scope you place in the scene:

- `ServiceBootstrapper` (built-in) -> backed by `ServiceContainer`
- `JuahnVContainerScope` (this package) -> backed by VContainer

## License

MIT. See `LICENSE.md`.

## Scope and disposal contract (2026-09)

`RegisterJuahnServiceLocator` registers a scoped locator: resolving in a child scope must see child-only services. `Bind`/`BindSelf` map to externally owned `RegisterInstance` values; the container does not dispose those instances. Type and factory registrations create container-owned services. Register per-run game/UI/physics services in a child scope and dispose it once when the run ends. The bridge does not drive game ticks. IdleMine pins VContainer 1.19.0 by Git commit and validates ownership and scope resolution in RuntimeCompositionTests.
