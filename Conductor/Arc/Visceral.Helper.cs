// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;

#pragma warning disable SA1649 // File name should match first type name

namespace Arc.Visceral
{
    internal static class ReflectionExtensions
    {
        public static bool IsNullable(this System.Reflection.TypeInfo type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Nullable<>);
        }

        public static bool IsPublic(this System.Reflection.TypeInfo type)
        {
            return type.IsPublic;
        }

        public static bool IsStruct(this System.Type type)
        {
            return type.IsValueType && !type.IsEnum && !type.IsPrimitive;
        }

        public static bool IsIndexer(this System.Reflection.PropertyInfo propertyInfo)
        {
            return propertyInfo.GetIndexParameters().Length > 0;
        }

        public static bool IsConstructedGenericType(this System.Reflection.TypeInfo type)
        {
            return type.AsType().IsConstructedGenericType;
        }

        public static MethodInfo? GetGetMethod(this PropertyInfo propInfo)
        {
            return propInfo.GetMethod;
        }

        public static MethodInfo? GetSetMethod(this PropertyInfo propInfo)
        {
            return propInfo.SetMethod;
        }
    }

    internal class ObjectMember
    {
        public bool IsProperty => this.PropertyInfo != null;

        public bool IsField => this.FieldInfo != null;

        public bool IsWritable { get; set; }

        public bool IsReadable { get; set; }

        public Type Type => this.IsField ? this.FieldInfo!.FieldType : this.PropertyInfo!.PropertyType;

        public MemberInfo MemberInfo { get; set; } = default!;

        public FieldInfo? FieldInfo { get; set; }

        public PropertyInfo? PropertyInfo { get; set; }

        public string Name => this.IsProperty ? this.PropertyInfo!.Name : this.FieldInfo!.Name;

        public bool IsValueType
        {
            get
            {
                MemberInfo mi = this.IsProperty ? (MemberInfo)this.PropertyInfo! : this.FieldInfo!;
                if (mi.DeclaringType == null)
                {
                    return false;
                }

                return mi.DeclaringType.GetTypeInfo().IsValueType;
            }
        }

        public bool IsStatic
        {
            get
            {
                if (this.PropertyInfo != null)
                {
                    return this.PropertyInfo.GetAccessors(true)[0].IsStatic;
                }

                return this.FieldInfo?.IsStatic == true;
            }
        }
    }

    internal class ObjectInfo
    {
        public ObjectInfo(Type type, ConstructorInfo? ctor, bool isClass, ObjectMember[] members)
        {
            this.Type = type;
            this.Constructor = ctor;
            this.IsClass = isClass;
            this.Members = members;
        }

        public Type Type { get; set; }

        public bool IsClass { get; set; }

        public bool IsStruct => !this.IsClass;

        public ConstructorInfo? Constructor { get; set; }

        public ObjectMember[] Members { get; set; }

        public static ObjectInfo CreateFromType(Type type)
        {
            TypeInfo ti = type.GetTypeInfo();
            var isClass = ti.IsClass || ti.IsInterface || ti.IsAbstract;
            var isStruct = ti.IsValueType;
            var members = new List<ObjectMember>();

            // ConstructorInfo ctor = ti.DeclaredConstructors.First(x => x.GetParameters().Length == 0);
            var ctor = ti.GetConstructor(Type.EmptyTypes);

            foreach (PropertyInfo item in type.GetRuntimeProperties())
            {
                if (item.IsIndexer())
                {
                    continue;
                }

                var getMethod = item.GetGetMethod(true);
                var setMethod = item.GetSetMethod(true);

                var member = new ObjectMember
                {
                    MemberInfo = item,
                    PropertyInfo = item,
                    IsReadable = (getMethod != null) && !getMethod.IsStatic,
                    IsWritable = (setMethod != null) && !setMethod.IsStatic,
                };

                members.Add(member);
            }

            foreach (FieldInfo item in type.GetRuntimeFields())
            {
                if (item.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>(true) != null)
                {
                    continue;
                }

                if (item.IsStatic)
                {
                    continue;
                }

                var member = new ObjectMember
                {
                    MemberInfo = item,
                    FieldInfo = item,
                    IsReadable = true,
                    IsWritable = !item.IsInitOnly,
                };

                members.Add(member);
            }

            return new ObjectInfo(type, ctor, isClass, members.ToArray());
        }
    }
}
