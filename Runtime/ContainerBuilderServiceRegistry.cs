#nullable enable
using System;
using Juahn.V2.Services;
using VContainer;

namespace Juahn.V2.VContainer
{
    /// <summary>
    /// Adapts a VContainer <see cref="IContainerBuilder"/> to the framework's <see cref="IServiceRegistry"/>
    /// write-side contract. This lets existing <see cref="IServiceInstaller"/> implementations - which bind
    /// into an <see cref="IServiceRegistry"/> - register their services into a VContainer
    /// <see cref="global::VContainer.Unity.LifetimeScope"/> without modification.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only the write side (the <c>Bind*</c> family) is meaningful here: registration happens during
    /// <see cref="global::VContainer.Unity.LifetimeScope.Configure"/>, before the container is built. The read
    /// side (<see cref="Resolve{T}"/> / <see cref="TryResolve{T}"/>) is intentionally unavailable during
    /// configuration because VContainer has no live <see cref="IObjectResolver"/> yet; those members throw
    /// <see cref="InvalidOperationException"/>. Resolve through <see cref="VContainerServiceLocator"/> after the
    /// scope is built instead.
    /// </para>
    /// <para>
    /// Because framework bindings are pre-created instances (installers pass concrete instances, not types),
    /// every <c>Bind</c> maps to VContainer's <c>RegisterInstance</c>. These instances remain externally owned;
    /// the container does not call Dispose on them. Use type/factory registration for owned services.
    /// </para>
    /// </remarks>
    public sealed class ContainerBuilderServiceRegistry : IServiceRegistry
    {
        private readonly IContainerBuilder _builder;

        /// <summary>
        /// Creates a registry adapter that forwards bindings to the given VContainer <paramref name="builder"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is <c>null</c>.</exception>
        public ContainerBuilderServiceRegistry(IContainerBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        /// <inheritdoc />
        public IServiceRegistry Bind<T>(T instance) where T : class
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance), $"Cannot bind a null instance to {typeof(T)}.");
            }

            if (!typeof(T).IsInterface)
            {
                throw new ArgumentException(
                    $"Cannot bind {instance} because {typeof(T)} is not an interface. Use BindSelf for concrete-type registration.");
            }

            _builder.RegisterInstance(instance).As<T>();

            return this;
        }

        /// <inheritdoc />
        public IServiceRegistry BindSelf<T>(T instance) where T : class
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance), $"Cannot bind a null instance to {typeof(T)}.");
            }

            // Register the concrete type under its own key (AsSelf), mirroring ServiceContainer.BindSelf.
            _builder.RegisterInstance(instance).AsSelf();

            return this;
        }

        /// <inheritdoc />
        public IServiceRegistry Bind<T1, T2>(object instance)
            where T1 : class
            where T2 : class
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance), $"Cannot bind a null instance to {typeof(T1)} and {typeof(T2)}.");
            }

            EnsureAssignable<T1>(instance);
            EnsureAssignable<T2>(instance);

            _builder.RegisterInstance(instance).As(typeof(T1), typeof(T2));

            return this;
        }

        /// <inheritdoc />
        public IServiceRegistry Bind<T1, T2, T3>(object instance)
            where T1 : class
            where T2 : class
            where T3 : class
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance),
                    $"Cannot bind a null instance to {typeof(T1)}, {typeof(T2)} and {typeof(T3)}.");
            }

            EnsureAssignable<T1>(instance);
            EnsureAssignable<T2>(instance);
            EnsureAssignable<T3>(instance);

            _builder.RegisterInstance(instance).As(typeof(T1), typeof(T2), typeof(T3));

            return this;
        }

        /// <inheritdoc />
        public IServiceRegistry BindFactory<T>(Func<T> factory) where T : class
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory), $"Cannot bind a null factory to {typeof(T)}.");
            }

            if (!typeof(T).IsInterface)
            {
                throw new ArgumentException($"Cannot bind factory because {typeof(T)} is not an interface.");
            }

            // VContainer resolves the factory lazily as a scoped singleton via a resolver delegate.
            _builder.Register(_ => factory(), Lifetime.Singleton).As<T>();

            return this;
        }

        /// <summary>
        /// Not supported during VContainer configuration. VContainer registrations are immutable once the
        /// builder is finalized; unregistering must be handled by scoping the <c>LifetimeScope</c> instead.
        /// </summary>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        public bool Clean<T>() where T : class
        {
            throw new NotSupportedException(
                "ContainerBuilderServiceRegistry cannot remove a binding: VContainer registrations are immutable " +
                "once configured. Dispose or re-create the LifetimeScope to release services.");
        }

        /// <summary>
        /// Not supported during VContainer configuration.
        /// </summary>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        public void Clean()
        {
            throw new NotSupportedException(
                "ContainerBuilderServiceRegistry cannot clear bindings: VContainer registrations are immutable " +
                "once configured. Dispose or re-create the LifetimeScope to release services.");
        }

        /// <summary>
        /// Not available during configuration. There is no live resolver until the scope is built; use
        /// <see cref="VContainerServiceLocator"/> against the built scope instead.
        /// </summary>
        /// <exception cref="InvalidOperationException">Always thrown.</exception>
        public T Resolve<T>() where T : class
        {
            throw new InvalidOperationException(
                "Cannot resolve from ContainerBuilderServiceRegistry during configuration. Resolve through " +
                "VContainerServiceLocator after the LifetimeScope has been built.");
        }

        /// <summary>
        /// Not available during configuration. Always returns <c>false</c> with a <c>null</c> instance because
        /// no resolver exists yet; use <see cref="VContainerServiceLocator"/> against the built scope instead.
        /// </summary>
        public bool TryResolve<T>(out T? instance) where T : class
        {
            instance = null;
            return false;
        }

        private static void EnsureAssignable<T>(object instance)
        {
            if (!(instance is T))
            {
                throw new ArgumentException($"The instance {instance.GetType()} does not implement {typeof(T)}.");
            }
        }
    }
}
