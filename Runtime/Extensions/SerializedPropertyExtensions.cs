using System;
using System.Collections;
using System.Reflection;
using UnityEditor;

namespace RogueLikeEngine.Extensions
{
    public static class SerializedPropertyExtensions
    {
        public static object GetParentObject(SerializedProperty property)
        {
            if (property == null) return null;

            object obj = property.serializedObject.targetObject;
            string path = property.propertyPath.Replace(".Array.data[", "[");
            string[] elements = path.Split('.');

            for (int i = 0; i < elements.Length - 1; i++) // Go up to the parent
            {
                string element = elements[i];

                if (element.Contains("["))
                {
                    // Handle list/array index
                    string fieldName = element[..element.IndexOf("[", StringComparison.Ordinal)];
                    int index = int.Parse(element[element.IndexOf("[", StringComparison.Ordinal)..].Replace("[", "")
                        .Replace("]", ""));

                    obj = GetIndexedValue(obj, fieldName, index);
                }
                else
                {
                    obj = GetFieldValue(obj, element);
                }

                if (obj == null) break;
            }

            return obj;
        }

        private static object GetFieldValue(object source, string name)
        {
            if (source == null) return null;
            var type = source.GetType();

            while (type != null)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                    return field.GetValue(source);

                type = type.BaseType;
            }

            return null;
        }

        private static object GetIndexedValue(object source, string fieldName, int index)
        {
            if (GetFieldValue(source, fieldName) is not IEnumerable enumerable) return null;

            IEnumerator enumerator = enumerable.GetEnumerator();
            using IDisposable disposable = enumerator as IDisposable;
            for (int i = 0; i <= index; i++)
            {
                if (!enumerator.MoveNext()) return null;
            }

            return enumerator.Current;
        }
    }
}