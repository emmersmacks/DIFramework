using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections;
using DIFramework.Builder;
using DIFramework.Descriptors;
using DIFramework.Descriptors.Impl;

namespace DIFramework.Container
{
    public class Container : IContainer
    {
        private class Scope : IScope
        {
            private readonly Container _container;
            private readonly Dictionary<ServiceDescriptor, object> _scopedInstances = new Dictionary<ServiceDescriptor, object>();
            
            public Scope(Container container)
            {
                _container = container;
            }
    
            public object Resolve(Type service) 
                => _container.CreateInstance(service, this);
            
            internal bool TryGetScopedInstance(ServiceDescriptor descriptor, out object instance)
            {
                return _scopedInstances.TryGetValue(descriptor, out instance);
            }

            internal void SetScopedInstance(ServiceDescriptor descriptor, object instance)
            {
                _scopedInstances[descriptor] = instance;
            }
        }
    
        internal object CreateInstance(Type service, IScope scope)
        {
            if (service.IsGenericType)
            {
                var genDef = service.GetGenericTypeDefinition();
                var arg = service.GetGenericArguments()[0];

                if (genDef == typeof(IEnumerable<>)
                    || genDef == typeof(List<>)
                    || genDef == typeof(IList<>)
                    || genDef == typeof(ICollection<>))
                {
                    var descriptors = GetDescriptorsFor(arg);

                    var listType = typeof(System.Collections.Generic.List<>).MakeGenericType(arg);
                    var list = (IList)Activator.CreateInstance(listType);

                    foreach (var d in descriptors)
                    {
                        object inst;
                        if (d is InstanceBasedServiceDescriptor id)
                        {
                            inst = id.Instance;
                        }
                        else if (d is FactoryBasedServiceDescriptor fd)
                        {
                            if (fd.Lifetime == Data.Lifetime.Singleton)
                            {
                                if (!_singletonInstances.TryGetValue(fd, out inst))
                                {
                                    inst = fd.Factory(scope);
                                    _singletonInstances[fd] = inst;
                                }
                            }
                            else if (fd.Lifetime == Data.Lifetime.Scoped && scope is Scope s)
                            {
                                if (!s.TryGetScopedInstance(fd, out inst))
                                {
                                    inst = fd.Factory(scope);
                                    s.SetScopedInstance(fd, inst);
                                }
                            }
                            else
                            {
                                inst = fd.Factory(scope);
                            }
                        }
                        else
                        {
                            var td = d as TypeBasedServiceDescriptor;
                            var impl = td.ImplementationType;

                            // type-based descriptor lifetimes
                            if (td.Lifetime == Data.Lifetime.Singleton)
                            {
                                inst = _singletonInstances
                                    .Where(p => p.Key is TypeBasedServiceDescriptor d &&
                                                d.ImplementationType == impl)
                                    .Select(p => p.Value)
                                    .FirstOrDefault();
                                if (inst == null)
                                {
                                    inst = CreateByType(impl, scope);
                                    _singletonInstances[td] = inst;
                                }
                            }
                            else if (td.Lifetime == Data.Lifetime.Scoped && scope is Scope s2)
                            {
                                if (!s2.TryGetScopedInstance(td, out inst))
                                {
                                    inst = CreateByType(impl, scope);
                                    s2.SetScopedInstance(td, inst);
                                }
                            }
                            else
                            {
                                inst = CreateByType(impl, scope);
                            }
                        }

                        list.Add(inst);
                    }

                    return list;
                }
            }

            var descriptorsForType = GetDescriptorsFor(service);
            if (descriptorsForType == null || descriptorsForType.Count == 0)
            {
                if (_parent != null)
                    return _parent.CreateInstance(service, scope);

                return null;
            }

            var descriptor = descriptorsForType[descriptorsForType.Count - 1];

            if (!(_descriptors != null && _descriptors.TryGetValue(service, out var localList) && localList.Contains(descriptor))
                && _parent != null)
            {
                return _parent.CreateInstance(service, scope);
            }

            if (descriptor is InstanceBasedServiceDescriptor instanceDescriptor)
                return instanceDescriptor.Instance;

            if (descriptor is FactoryBasedServiceDescriptor factoryDescriptor)
            {
                if (factoryDescriptor.Lifetime == Data.Lifetime.Singleton)
                {
                    if (!_singletonInstances.TryGetValue(factoryDescriptor, out var inst))
                    {
                        inst = factoryDescriptor.Factory(scope);
                        _singletonInstances[factoryDescriptor] = inst;
                    }
                    return inst;
                }

                if (factoryDescriptor.Lifetime == Data.Lifetime.Scoped && scope is Scope ss)
                {
                    if (!ss.TryGetScopedInstance(factoryDescriptor, out var inst))
                    {
                        inst = factoryDescriptor.Factory(scope);
                        ss.SetScopedInstance(factoryDescriptor, inst);
                    }
                    return inst;
                }

                return factoryDescriptor.Factory(scope);
            }

            var typeDescriptor = descriptor as TypeBasedServiceDescriptor;
            var implementation = typeDescriptor.ImplementationType;

            if (typeDescriptor.Lifetime == Data.Lifetime.Singleton)
            {
                var inst = _singletonInstances
                    .Where(p => p.Key is TypeBasedServiceDescriptor d &&
                                d.ImplementationType == implementation)
                    .Select(p => p.Value)
                    .FirstOrDefault();
                if (inst == null)
                {
                    inst = CreateByType(implementation, scope);
                    _singletonInstances[typeDescriptor] = inst;
                }
                return inst;
            }

            if (typeDescriptor.Lifetime == Data.Lifetime.Scoped && scope is Scope sss)
            {
                if (!sss.TryGetScopedInstance(typeDescriptor, out var inst))
                {
                    inst = CreateByType(implementation, scope);
                    sss.SetScopedInstance(typeDescriptor, inst);
                }
                return inst;
            }

            return CreateByType(implementation, scope);
        }

        private object CreateByType(Type implementation, IScope scope)
        {
            var constructors = implementation.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (constructors.Length == 0)
                return Activator.CreateInstance(implementation);

            var constructor = constructors.Single();
            var parameters = constructor.GetParameters();
            var argsForConstructor = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType is IScope)
                {
                    argsForConstructor[i] = scope;
                }
                else
                {
                    argsForConstructor[i] = CreateInstance(parameters[i].ParameterType, scope);
                }
            }

            return constructor.Invoke(argsForConstructor);
        }
    
        private Dictionary<Type, List<ServiceDescriptor>> _descriptors;
        private readonly Dictionary<ServiceDescriptor, object> _singletonInstances = new Dictionary<ServiceDescriptor, object>();
        private readonly Container _parent;
    
        public Container(IEnumerable<ServiceDescriptor> descriptors, Container parent = null)
        {
            _descriptors = descriptors
                .GroupBy(x => x.ServiceType)
                .ToDictionary(g => g.Key, g => g.ToList());
            _parent = parent;
        }
        
        public IScope CreateScope()
        {
            return new Scope(this);
        }

        public IContainer CreateChild(IEnumerable<ServiceDescriptor> descriptors = null)
        {
            var list = descriptors == null ? new List<ServiceDescriptor>() : new List<ServiceDescriptor>(descriptors);
            return new Container(list, this);
        }

        public IContainerBuilder CreateChildBuilder()
        {
            return new ContainerBuilder(this);
        }

        internal List<ServiceDescriptor> GetDescriptorsFor(Type serviceType)
        {
            var result = new List<ServiceDescriptor>();
            if (_descriptors != null && _descriptors.TryGetValue(serviceType, out var list))
                result.AddRange(list);
            if (_parent != null)
                result.AddRange(_parent.GetDescriptorsFor(serviceType));
            return result;
        }
    }
}

