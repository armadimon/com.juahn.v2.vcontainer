#nullable enable
using System;
using Juahn.V2.Services;
using VContainer;

namespace Juahn.V2.VContainer
{
    /// <summary>
    /// Extension methods that bridge the framework's DI abstraction into VContainer during
    /// <see cref="global::VContainer.Unity.LifetimeScope.Configure"/>.
    /// </summary>
    public static class JuahnVContainerExtensions
    {
        /// <summary>
        /// Runs the given framework <paramref name="installers"/> against a VContainer
        /// <paramref name="builder"/>, translating every <see cref="IServiceRegistry"/> binding into a
        /// VContainer registration. Call this from your <c>LifetimeScope.Configure</c>.
        /// </summary>
        /// <param name="builder">The VContainer builder being configured.</param>
        /// <param name="installers">The framework installers to apply, in order.</param>
        /// <returns>The same <paramref name="builder"/> to allow chained calls.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="builder"/> or <paramref name="installers"/> is <c>null</c>.
        /// </exception>
        public static IContainerBuilder RegisterJuahnServices(
            this IContainerBuilder builder,
            params IServiceInstaller[] installers)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (installers == null)
            {
                throw new ArgumentNullException(nameof(installers));
            }

            var registry = new ContainerBuilderServiceRegistry(builder);

            for (var i = 0; i < installers.Length; i++)
            {
                var installer = installers[i];
                if (installer == null)
                {
                    continue;
                }

                installer.InstallBindings(registry);
            }

            return builder;
        }

        /// <summary>
        /// Registers a <see cref="VContainerServiceLocator"/> as the <see cref="IServiceLocator"/> for the
        /// scope, so that framework code depending on <see cref="IServiceLocator"/> resolves through this
        /// VContainer scope. Call this from your <c>LifetimeScope.Configure</c> in addition to (or instead of)
        /// <see cref="RegisterJuahnServices"/>.
        /// </summary>
        /// <param name="builder">The VContainer builder being configured.</param>
        /// <returns>The same <paramref name="builder"/> to allow chained calls.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is <c>null</c>.</exception>
        public static IContainerBuilder RegisterJuahnServiceLocator(this IContainerBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // The locator needs the fully built IObjectResolver, which VContainer injects into the factory.
            builder.Register<IServiceLocator>(
                resolver => new VContainerServiceLocator(resolver),
                Lifetime.Singleton);

            return builder;
        }
    }
}
