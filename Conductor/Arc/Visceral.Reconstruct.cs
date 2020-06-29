// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FastExpressionCompiler;

#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1649 // File name should match first type name

namespace Arc.Visceral
{
    /// <summary>
    /// Reconstruct delegate.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="t">The object to be reconstructed.</param>
    /// <returns>The object reconstructed.</returns>
    public delegate T ReconstructFunc<T>(T t);

    /// <summary>
    /// Reconstruct.Do() will call Reconstruct() method of each class which implements IReconstructable interface.
    /// </summary>
    public interface IReconstructable
    {
        public void Reconstruct();
    }

    /// <summary>
    /// Get the reconstruct delegate (ReconstructAction&lt;T&gt;).
    /// </summary>
    public interface IReconstructResolver
    {
        bool Get<T>(out ReconstructFunc<T>? func);
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ReconstructableAttribute : Attribute
    {
        public ReconstructableAttribute()
        {
        }
    }

    public class DefaultReconstructResolver : IReconstructResolver
    {
        /// <summary>
        /// Default instance.
        /// </summary>
        public static readonly DefaultReconstructResolver Instance = new DefaultReconstructResolver();

        public bool Get<T>(out ReconstructFunc<T>? action)
        {
            action = default;

            var type = typeof(T);
            if (type.IsPrimitive)
            {
                return true; // empty
            }
            else if (type == typeof(string))
            {
                return true; // empty
            }

            return false; // not supported.
        }
    }

    public static class Reconstruct
    {
        private static IReconstructResolver[]? resolvers; // Internal use.

        private static Stack<Type> circularDependencyCheck = new Stack<Type>();

        static Reconstruct()
        {
        }

        public static IReconstructResolver[] Resolvers // Get Resolver.
        {
            get
            {
                // Default Resolver.
                resolvers ??= new IReconstructResolver[] { DefaultReconstructResolver.Instance };
                return resolvers;
            }

            set
            {
                resolvers = value;
            }
        }

        public static T Do<T>(T t)
        {
            if (t == null)
            {
                throw new ArgumentNullException();
            }

            var c = ResolverCache<T>.Cache;
            if (c != null)
            {
                return c.Invoke(t);
            }

            return t;
        }

        public static ReconstructFunc<T>? BuildCode<T>()
        {
            lock (circularDependencyCheck)
            {
                var type = typeof(T);
                if (type.Namespace?.StartsWith("System") == true)
                {
                    return null;
                }

                circularDependencyCheck.Push(type);

                var info = ObjectInfo.CreateFromType(type);
                var expressions = new List<Expression>();

                // log Console.WriteLine("cache: " + type.Name);
                // log expressions.Add(Expression.Call(null, typeof(Console).GetMethod("WriteLine", new Type[] { typeof(String) }), Expression.Constant("reconstruct: " + type.Name)));
                var arg = Expression.Parameter(type); // type
                foreach (var member in info.Members.Where(x => x.Type.IsClass && !x.IsStatic))
                {// Class
                    if (circularDependencyCheck.Contains(member.Type))
                    {// skip (circular dependency).
                        continue;
                    }

                    var prop = Expression.PropertyOrField(arg, member.Name);

                    if (typeof(IReconstructable).IsAssignableFrom(member.Type) || member.Type.IsDefined(typeof(ReconstructableAttribute), true) || Attribute.IsDefined(member.MemberInfo, typeof(ReconstructableAttribute)))
                    { // If the member implements IReconstructable or ReconstructableAttribute, try to create an instance.
                        if (member.IsWritable)
                        {
                            if (member.Type.GetConstructor(Type.EmptyTypes) != null)
                            {
                                var e = Expression.IfThen(
                                    Expression.Equal(prop, Expression.Constant(null)),
                                    Expression.Assign(prop, Expression.New(member.Type)));
                                expressions.Add(e);
                            }
                            else if (member.Type == typeof(string))
                            {
                                expressions.Add(Expression.IfThen(
                                    Expression.Equal(prop, Expression.Constant(null)),
                                    Expression.Assign(prop, Expression.Constant(string.Empty))));
                            }
                        }
                    }

                    // reconstruct (ResolverCache<T>.Cache?.Invoke(t);).
                    var memberReconstructorCache = typeof(ResolverCache<>).MakeGenericType(member.Type);
                    var reconstructorAction = memberReconstructorCache.GetField(nameof(ResolverCache<int>.Cache));
                    if (reconstructorAction?.GetValue(memberReconstructorCache) != null)
                    {
                        expressions.Add(Expression.IfThen(
                                Expression.NotEqual(prop, Expression.Constant(null)),
                                Expression.Invoke(Expression.MakeMemberAccess(null, reconstructorAction), prop)));
                    }
                }

                foreach (var member in info.Members.Where(x => x.Type.IsStruct() && !x.IsStatic))
                {// Struct
                    if (circularDependencyCheck.Contains(member.Type))
                    {// skip (circular dependency).
                        continue;
                    }

                    var prop = Expression.PropertyOrField(arg, member.Name);

                    // reconstruct (ResolverCache<T>.Cache?.Invoke(t);).
                    var memberReconstructorCache = typeof(ResolverCache<>).MakeGenericType(member.Type);
                    var reconstructorAction = memberReconstructorCache.GetField(nameof(ResolverCache<int>.Cache));
                    if (reconstructorAction?.GetValue(memberReconstructorCache) != null)
                    {
                        if (member.IsWritable)
                        {
                            expressions.Add(Expression.Assign(prop, Expression.Invoke(Expression.MakeMemberAccess(null, reconstructorAction), prop)));
                        }
                        else
                        {
                            expressions.Add(Expression.Invoke(Expression.MakeMemberAccess(null, reconstructorAction), prop));
                        }
                    }
                }

                // IReconstruct.Reconstruct()
                try
                {
                    if (type.GetInterfaces().Contains(typeof(IReconstructable)))
                    {
                        var miReconstruct = type.GetInterfaceMap(typeof(IReconstructable)).InterfaceMethods.First(x => x.Name == "Reconstruct");
                        expressions.Add(Expression.Call(arg, miReconstruct));
                    }
                }
                catch
                {
                }

                expressions.Add(arg);

                circularDependencyCheck.Pop();

                var body = Expression.Block(expressions.ToArray());
                var lamda = Expression.Lambda<ReconstructFunc<T>>(body, arg);

                return lamda.CompileFast(); // Alternative: lamda.Compile().
            }
        }

        public static void RebuildCache<T>()
        { // Discard the old cache and rebuild cache.
            ResolverCache<T>.PrepareCache();
        }

        private static class ResolverCache<T>
        {
#pragma warning disable SA1401 // Fields should be private
            public static ReconstructFunc<T>? Cache;
#pragma warning restore SA1401 // Fields should be private

            static ResolverCache()
            {
                PrepareCache();
            }

            public static void PrepareCache()
            {
                foreach (var x in Reconstruct.Resolvers)
                {
                    if (x.Get<T>(out var m))
                    { // found
                        Cache = m;
                        return;
                    }
                }

                Cache = Reconstruct.BuildCode<T>();
            }
        }
    }
}
