#nullable enable
using System;
using Juahn.V2.Services;
using VContainer;

namespace Juahn.V2.VContainer
{
    /// <summary>
    /// Implements the framework's <see cref="IServiceLocator"/> abstraction on top of a VContainer
    /// <see cref="IObjectResolver"/>. Register this locator inside your <see cref="global::VContainer.Unity.LifetimeScope"/>
    /// (see <see cref="JuahnVContainerExtensions"/>) so that framework code depending on
    /// <see cref="IServiceLocator"/> resolves through the VContainer backend instead of the built-in
    /// <see cref="ServiceLocator"/>.
    /// </summary>
    /// <remarks>
    /// Use this when your project standardizes on VContainer for dependency injection but you still want to
    /// consume framework services through the provider-agnostic <see cref="IServiceLocator"/> contract. If you
    /// are not using VContainer, keep using the built-in <see cref="ServiceLocator"/> / <see cref="ServiceContainer"/>
    /// instead; this bridge adds no value there.
    /// </remarks>
    public sealed class VContainerServiceLocator : IServiceLocator
    {
        private readonly IObjectResolver _resolver;

        /// <summary>
        /// Creates a locator that delegates all resolution to the given VContainer <paramref name="resolver"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="resolver"/> is <c>null</c>.</exception>
        public VContainerServiceLocator(IObjectResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        /// <summary>The underlying VContainer resolver this locator delegates to.</summary>
        public IObjectResolver Resolver => _resolver;

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">
        /// Thrown if <typeparamref name="T"/> has not been registered in the VContainer scope. The
        /// underlying <see cref="VContainerException"/> is wrapped to satisfy the framework contract.
        /// </exception>
        public T Resolve<T>() where T : class
        {
            try
            {
                // VContainer throws VContainerException when the type is not registered.
                return _resolver.Resolve<T>();
            }
            catch (VContainerException exception)
            {
                throw new InvalidOperationException(
                    $"The type {typeof(T)} is not registered in the VContainer scope. " +
                    "Register it in your LifetimeScope.Configure (or via an IServiceInstaller passed to " +
                    "RegisterJuahnServices) before resolving.",
                    exception);
            }
        }

        /// <inheritdoc />
        public bool TryResolve<T>(out T? instance) where T : class
        {
            // IObjectResolver.TryResolve(Type, out object) is the non-generic primitive; it does not throw
            // when the type is missing, so it is safe for the try-pattern contract.
            if (_resolver.TryResolve(typeof(T), out var resolved) && resolved is T typed)
            {
                instance = typed;
                return true;
            }

            instance = null;
            return false;
        }
    }
}
