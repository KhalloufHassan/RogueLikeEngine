using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using RogueLikeEngine.Systems.Stats;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RogueLikeEngine.Tests
{
    /// <summary>
    /// Tracks objects created by a test so they can be destroyed in TearDown,
    /// and gives access to private serialized fields that are normally set from the inspector.
    /// </summary>
    internal class TestObjects : IDisposable
    {
        private readonly List<Object> m_objects = new();

        public T Track<T>(T obj) where T : Object
        {
            m_objects.Add(obj);
            return obj;
        }

        public T CreateAsset<T>() where T : ScriptableObject => Track(ScriptableObject.CreateInstance<T>());

        public T CreateComponent<T>(string name = null) where T : Component
        {
            GameObject go = Track(new GameObject(name ?? typeof(T).Name));
            return go.AddComponent<T>();
        }

        public void Dispose()
        {
            foreach (Object obj in m_objects)
            {
                if (obj) Object.DestroyImmediate(obj);
            }

            m_objects.Clear();
        }
    }

    internal static class TestModifiers
    {
        public static BasicStatModifier Flat(StatDefinition stat, float value)
        {
            BasicStatModifier modifier = new();
            TestReflection.SetField(modifier, "targetStat", stat);
            modifier.ForceNewValue(value);
            return modifier;
        }
    }

    internal static class TestReflection
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static void SetField(object target, string fieldName, object value) => FindField(target, fieldName).SetValue(target, value);

        public static T GetField<T>(object target, string fieldName) => (T)FindField(target, fieldName).GetValue(target);

        /// <summary>Calls a parameterless instance method, e.g. a Unity message that isn't called in edit mode.</summary>
        public static void Invoke(object target, string methodName)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                MethodInfo method = type.GetMethod(methodName, Flags | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);
                if (method == null) continue;

                try
                {
                    method.Invoke(target, null);
                }
                catch (TargetInvocationException e) when (e.InnerException != null)
                {
                    ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                }

                return;
            }

            throw new MissingMethodException(target.GetType().Name, methodName);
        }

        private static FieldInfo FindField(object target, string fieldName)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(fieldName, Flags);
                if (field != null) return field;
            }

            throw new MissingFieldException(target.GetType().Name, fieldName);
        }
    }
}
