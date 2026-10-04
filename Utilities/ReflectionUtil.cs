using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace WotHCustomItemSDK
{
    internal static class ReflectionUtil
    {
        public static Type TypeByName(string fullName)
        {
            return AccessTools.TypeByName(fullName);
        }

        public static object GetMember(object instance, string memberName)
        {
            if (instance == null)
                return null;

            return GetMember(instance.GetType(), instance, memberName, false);
        }

        public static object GetStaticMember(Type type, string memberName)
        {
            if (type == null)
                return null;

            return GetMember(type, null, memberName, true);
        }

        private static object GetMember(Type type, object instance, string memberName, bool isStatic)
        {
            FieldInfo field = AccessTools.Field(type, memberName);
            if (field != null && field.IsStatic == isStatic)
                return field.GetValue(instance);

            PropertyInfo property = AccessTools.Property(type, memberName);
            if (property != null && property.GetMethod != null && property.GetMethod.IsStatic == isStatic)
                return property.GetValue(instance, null);

            return null;
        }

        public static bool SetMember(object instance, string memberName, object value)
        {
            if (instance == null)
                return false;

            Type type = instance.GetType();
            FieldInfo field = AccessTools.Field(type, memberName);
            if (field != null && !field.IsStatic)
            {
                if (value == null || field.FieldType.IsInstanceOfType(value) || CanAssignNullable(field.FieldType, value))
                {
                    field.SetValue(instance, value);
                    return true;
                }
            }

            PropertyInfo property = AccessTools.Property(type, memberName);
            if (property != null && property.SetMethod != null && !property.SetMethod.IsStatic)
            {
                if (value == null || property.PropertyType.IsInstanceOfType(value) || CanAssignNullable(property.PropertyType, value))
                {
                    property.SetValue(instance, value, null);
                    return true;
                }
            }

            return false;
        }

        public static bool SetStaticMember(Type type, string memberName, object value)
        {
            if (type == null)
                return false;

            FieldInfo field = AccessTools.Field(type, memberName);
            if (field != null && field.IsStatic)
            {
                field.SetValue(null, value);
                return true;
            }

            PropertyInfo property = AccessTools.Property(type, memberName);
            if (property != null && property.SetMethod != null && property.SetMethod.IsStatic)
            {
                property.SetValue(null, value, null);
                return true;
            }

            return false;
        }

        private static bool CanAssignNullable(Type targetType, object value)
        {
            if (value == null)
                return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;

            return targetType.IsAssignableFrom(value.GetType());
        }

        public static object Invoke(object instance, string methodName, params object[] args)
        {
            if (instance == null)
                return null;

            MethodInfo method = FindMethod(instance.GetType(), methodName, args == null ? 0 : args.Length);
            if (method == null)
            {
                Plugin.LogWarning("Method not found: " + instance.GetType().FullName + "." + methodName);
                return null;
            }

            return method.Invoke(instance, args);
        }

        public static object InvokeStatic(Type type, string methodName, params object[] args)
        {
            if (type == null)
                return null;

            MethodInfo method = FindMethod(type, methodName, args == null ? 0 : args.Length);
            if (method == null)
            {
                Plugin.LogWarning("Static method not found: " + type.FullName + "." + methodName);
                return null;
            }

            return method.Invoke(null, args);
        }

        public static bool InvokeExtensionMethod(Component instance, string methodName)
        {
            if (instance == null || string.IsNullOrEmpty(methodName))
                return false;

            Type instanceType = instance.GetType();

            Assembly gameAssembly = instanceType.Assembly;
            if (TryInvokeExtensionInAssembly(gameAssembly, instance, instanceType, methodName))
                return true;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly assembly = assemblies[i];
                if (assembly == gameAssembly)
                    continue;

                if (TryInvokeExtensionInAssembly(assembly, instance, instanceType, methodName))
                    return true;
            }

            return false;
        }

        private static bool TryInvokeExtensionInAssembly(
            Assembly assembly,
            Component instance,
            Type instanceType,
            string methodName)
        {
            if (assembly == null)
                return false;

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }
            catch
            {
                return false;
            }

            if (types == null)
                return false;

            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];
                if (type == null || !type.IsSealed || !type.IsAbstract)
                    continue;

                MethodInfo[] methods;
                try
                {
                    methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                }
                catch
                {
                    continue;
                }

                for (int m = 0; m < methods.Length; m++)
                {
                    MethodInfo method = methods[m];
                    if (method.Name != methodName || !method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
                        continue;

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 0 || parameters.Length > 1)
                        continue;

                    object targetArgument = GetExtensionTargetArgument(parameters[0].ParameterType, instance, instanceType);
                    if (targetArgument == null)
                        continue;

                    try
                    {
                        method.Invoke(null, new object[] { targetArgument });
                        return true;
                    }
                    catch
                    {
                    }
                }
            }

            return false;
        }

        private static object GetExtensionTargetArgument(Type parameterType, Component instance, Type instanceType)
        {
            if (parameterType.IsAssignableFrom(instanceType))
                return instance;

            if (parameterType.IsAssignableFrom(typeof(GameObject)))
                return instance.gameObject;

            if (parameterType.IsAssignableFrom(typeof(Transform)))
                return instance.transform;

            return null;
        }

        public static MethodInfo FindMethod(Type type, string methodName, int parameterCount)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != methodName)
                    continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == parameterCount)
                    return method;
            }

            return null;
        }

        public static MethodInfo FindMethodByNameAndParameterCount(Type type, string methodName, int parameterCount)
        {
            return FindMethod(type, methodName, parameterCount);
        }

        public static Component GetComponentByTypeName(GameObject gameObject, string typeName)
        {
            if (gameObject == null)
                return null;

            Type type = TypeByName(typeName);
            if (type == null)
            {
                Plugin.LogWarning("Type not found: " + typeName);
                return null;
            }

            return gameObject.GetComponent(type);
        }

        public static GameObject InstantiateChild(GameObject original, Transform parent)
        {
            if (original == null)
                return null;

            UnityEngine.Object result = UnityEngine.Object.Instantiate(original, parent);
            return result as GameObject;
        }

        public static GameObject FindChildByName(Transform parent, string name)
        {
            if (parent == null)
                return null;

            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child.gameObject;

                GameObject result = FindChildByName(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }

        public static void DestroyIfFound(Transform root, string name)
        {
            GameObject obj = FindChildByName(root, name);
            if (obj != null)
                UnityEngine.Object.Destroy(obj);
        }

        public static IList GetIListMember(object instance, string memberName)
        {
            object value = GetMember(instance, memberName);
            return value as IList;
        }

        public static void RemoveAtSafe(IList list, int index)
        {
            if (list == null)
                return;

            if (index >= 0 && index < list.Count)
                list.RemoveAt(index);
        }

        public static void ClearList(object instance, string memberName)
        {
            IList list = GetIListMember(instance, memberName);
            if (list != null)
                list.Clear();
        }

        public static bool AddToList(object instance, string memberName, object value)
        {
            IList list = GetIListMember(instance, memberName);
            if (list == null)
                return false;

            list.Add(value);
            return true;
        }

        public static object GetListElement(object instance, string memberName, int index)
        {
            IList list = GetIListMember(instance, memberName);
            if (list == null || index < 0 || index >= list.Count)
                return null;

            return list[index];
        }

        public static Delegate CreateNoArgDelegate(Type delegateType, object target, MethodInfo method)
        {
            if (delegateType == null || method == null)
                return null;

            if (!typeof(Delegate).IsAssignableFrom(delegateType))
                return null;

            try
            {
                return Delegate.CreateDelegate(delegateType, target, method);
            }
            catch
            {
                return null;
            }
        }
    }
}
