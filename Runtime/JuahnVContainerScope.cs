#nullable enable
using System.Collections.Generic;
using Juahn.V2.Services;
using VContainer;
using VContainer.Unity;

namespace Juahn.V2.VContainer
{
    /// <summary>
    /// A convenience <see cref="LifetimeScope"/> base class that wires JuahnFrameworkV2 into VContainer.
    /// It registers a <see cref="VContainerServiceLocator"/> (so framework code can resolve through
    /// <see cref="IServiceLocator"/>) and applies any framework <see cref="IServiceInstaller"/>s supplied by
    /// <see cref="CollectInstallers"/> into the VContainer builder.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the VContainer-flavored analogue of the built-in <see cref="ServiceBootstrapper"/>. Use it when
    /// your project standardizes on VContainer: subclass it, place it on your root scope game object, and
    /// override <see cref="CollectInstallers"/> to supply your installers. If you are NOT using VContainer,
    /// prefer <see cref="ServiceBootstrapper"/> with the built-in <see cref="ServiceLocator"/> instead.
    /// </para>
    /// <para>
    /// Override <see cref="Configure(IContainerBuilder)"/> and call <c>base.Configure(builder)</c> if you need
    /// to add extra VContainer registrations of your own alongside the framework bindings.
    /// </para>
    /// </remarks>
    public class JuahnVContainerScope : LifetimeScope
    {
        /// <summary>
        /// Override to supply the framework installers that should be applied to this scope. Called once during
        /// <see cref="Configure(IContainerBuilder)"/>. The default implementation supplies none.
        /// </summary>
        protected virtual IEnumerable<IServiceInstaller> CollectInstallers()
        {
            yield break;
        }

        /// <summary>
        /// Registers the framework service locator and applies the collected installers. Call
        /// <c>base.Configure(builder)</c> from a subclass override to keep this wiring.
        /// </summary>
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterJuahnServiceLocator();

            var installers = new List<IServiceInstaller>(CollectInstallers());
            builder.RegisterJuahnServices(installers.ToArray());
        }
    }
}
