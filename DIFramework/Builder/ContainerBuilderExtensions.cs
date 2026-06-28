using System;
using DIFramework.Data;
using DIFramework.Descriptors.Impl;

namespace DIFramework.Builder
{
    public static class ContainerBuilderExtensions
    {
        private static IContainerBuilder RegisterType(this IContainerBuilder builder, Type service, Type implementation, Lifetime lifetime)
        {
            var descriptor = new TypeBasedServiceDescriptor()
            { 
                ImplementationType = implementation, 
                Lifetime = lifetime,
                ServiceType = service 
            };
            builder.Register(descriptor);
            return builder;
        }
    
        private static IContainerBuilder RegisterFactory(this IContainerBuilder builder, Type type,
            Func<IScope, object> factory, Lifetime lifetime)
        {
            var descriptor = new FactoryBasedServiceDescriptor()
            {
                Factory = factory,
                Lifetime = lifetime,
                ServiceType = type
            };
            builder.Register(descriptor);
            return builder;
        }
    
        private static IContainerBuilder RegisterInstance(this IContainerBuilder builder, Type type, object instance)
        {
            var descriptor = new InstanceBasedServiceDescriptor(type, instance);
            builder.Register(descriptor);
            return builder;
        }
    
        #region ByType
    
        public static IContainerBuilder RegisterSingleton(
            this IContainerBuilder builder,
            Type @serviceInterface,
            Type serviceImplementation)
            => builder.RegisterType(serviceInterface, serviceImplementation, Lifetime.Singleton);
        
        public static IContainerBuilder RegisterSingletonFor(this IContainerBuilder builder, Type implementation, params Type[] serviceTypes)
        {
            if (implementation == null) throw new ArgumentNullException(nameof(implementation));
            if (serviceTypes == null || serviceTypes.Length == 0)
                throw new ArgumentException("At least one service type must be provided", nameof(serviceTypes));
    
            foreach (var service in serviceTypes)
                builder.RegisterType(service, implementation, Lifetime.Singleton);
    
            return builder;
        }
    
        public static IContainerBuilder RegisterTransient(
            this IContainerBuilder builder,
            Type @serviceInterface,
            Type serviceImplementation)
            => builder.RegisterType(serviceInterface, serviceImplementation, Lifetime.Transient);
        
        public static IContainerBuilder RegisterTransientFor(this IContainerBuilder builder, Type implementation, params Type[] serviceTypes)
        {
            if (implementation == null) throw new ArgumentNullException(nameof(implementation));
            if (serviceTypes == null || serviceTypes.Length == 0)
                throw new ArgumentException("At least one service type must be provided", nameof(serviceTypes));
    
            foreach (var service in serviceTypes)
                builder.RegisterType(service, implementation, Lifetime.Transient);
    
            return builder;
        }
        
        public static IContainerBuilder RegisterScoped(
            this IContainerBuilder builder,
            Type @serviceInterface,
            Type serviceImplementation)
            => builder.RegisterType(serviceInterface, serviceImplementation, Lifetime.Scoped);
    
        #endregion
        
        #region ByTypeGeneric
    
        public static IContainerBuilder RegisterSingleton<TInterface, TImplementation>(
            this IContainerBuilder builder) 
            => builder.RegisterType(typeof(TInterface), typeof(TImplementation), Lifetime.Singleton);
        
        public static IContainerBuilder RegisterTransient<TInterface, TImplementation>(
            this IContainerBuilder builder) 
            => builder.RegisterType(typeof(TInterface), typeof(TImplementation), Lifetime.Transient);
        
        public static IContainerBuilder RegisterScoped<TInterface, TImplementation>(
            this IContainerBuilder builder) 
            => builder.RegisterType(typeof(TInterface), typeof(TImplementation), Lifetime.Scoped);
    
        public static IContainerBuilder RegisterSingleton<TImplementation>(this IContainerBuilder builder)
            => builder.RegisterType(typeof(TImplementation), typeof(TImplementation), Lifetime.Singleton);
    
        public static IContainerBuilder RegisterSingletonFor<TImplementation>(this IContainerBuilder builder, params Type[] serviceTypes)
            => builder.RegisterSingletonFor(typeof(TImplementation), serviceTypes);
        
        public static IContainerBuilder RegisterTransientFor<TImplementation>(this IContainerBuilder builder, params Type[] serviceTypes)
            => builder.RegisterTransientFor(typeof(TImplementation), serviceTypes);
    
        #endregion
        
        #region ByInstance
    
        public static IContainerBuilder RegisterSingleton(
            this IContainerBuilder builder,
            Type type, object instance)
            => builder.RegisterInstance(type, instance);
    
        public static IContainerBuilder RegisterSingleton<T>(
            this IContainerBuilder builder,
            object instance)
            => builder.RegisterInstance(typeof(T), instance);
    
        public static IContainerBuilder RegisterSingletonFor(this IContainerBuilder builder, object instance, params Type[] serviceTypes)
        {
            if (serviceTypes == null || serviceTypes.Length == 0)
                throw new ArgumentException("At least one service type must be provided", nameof(serviceTypes));
    
            foreach (var service in serviceTypes)
                builder.RegisterInstance(service, instance);
    
            return builder;
        }
    
        #endregion
    }
}

