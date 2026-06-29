namespace DIFramework
{
    public static class ScopeExtensions
    {
        public static T Resolve<T>(this IScope scope, bool collectParentDescriptors = true)
        {
            return (T)scope.Resolve(typeof(T), collectParentDescriptors);
        }
    }
}


