﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace PerunDrawer
{
	public static class Utilities
	{
		public static T GetValue<T>(object source, string name, T defaultValue)
		{
			T result;
			return GetValue(source, name, out result) ? result : defaultValue;
		}

		public static bool GetValue<T>(object source, string name, out T value)
		{
			if (source != null)
			{
				Type type = source.GetType();
				FieldInfo field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				if (field != null && field.FieldType == typeof(T))
				{
					value = (T) field.GetValue(source);
					return true;
				}
				PropertyInfo property = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
				if (property != null && property.PropertyType == typeof(T))
				{
					value = (T) property.GetValue(source, null);
					return true;
				}
				MethodInfo method = type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
				if (method != null && method.ReturnType == typeof(T))
				{
					value = (T) method.Invoke(source, null);
					return true;
				}
			}
			value = default(T);
			return false;
		}
		
		public static bool SetValue<T>(object source, string name, T value)
		{
			if (source != null)
			{
				Type type = source.GetType();
				FieldInfo field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				if (field != null)
				{
					field.SetValue(source, value);
					return true;
				}
				PropertyInfo property = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
				if (property != null)
				{
					property.SetValue(source, value, null);
					return true;
				}
			}
			return false;
		}

		public static List<Attribute> GetAttributes(object source, string name)
		{
			if(source == null)
				return null;
			Type type = source.GetType();
			
			FieldInfo field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			if(field != null)
				return field.GetCustomAttributes(false).Cast<Attribute>().ToList();
			
			PropertyInfo property = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
			if(property != null)
				return property.GetCustomAttributes(false).Cast<Attribute>().ToList();
			
			MethodInfo method = type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
			if(method != null)
				return method.GetCustomAttributes(false).Cast<Attribute>().ToList();
			
			return null;
		}
		
		public static List<Attribute> GetTypeAttributes(object source)
		{
			return source != null ? source.GetType().GetCustomAttributes(false).Cast<Attribute>().ToList() : null;
		}
		
		public static List<Attribute> GetElementTypeAttributes(object source)
		{
			if (source == null)
				return null;
			Type type = GetElementType(source);
			return type != null ? type.GetCustomAttributes(false).Cast<Attribute>().ToList() : null;
		}

		public static Type GetElementType(object source)
		{
			if (source == null)
				return null;
			Type listType = source.GetType();
			return listType.IsGenericType ? listType.GetGenericArguments().First() : listType.GetElementType();
		}
		
		public static object GetParent(SerializedProperty prop)
		{
			var path = prop.propertyPath.Replace(".Array.data[", "[");
			object obj = prop.serializedObject.targetObject;
			var elements = path.Split('.');
			foreach(var element in elements.Take(elements.Length-1))
			{
				if(element.Contains("["))
				{
					var elementName = element.Substring(0, element.IndexOf("["));
					var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[","").Replace("]",""));
					obj = GetValue(obj, elementName, index);
				}
				else
				{
					obj = GetValue(obj, element);
				}
			}
			return obj;
		}
		
		public static object GetValue(object source, string name)
		{
			if(source == null)
				return null;
			var type = source.GetType();
			
			var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			if(field != null)
				return field.GetValue(source);
			
			var property = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
			if(property != null)
				return property.GetValue(source, null);
			
			return null;
		}
	
		public static object GetValue(object source, int index)
		{
			var enumerable = source as IEnumerable;
			var enm = enumerable.GetEnumerator();
			while(index-- >= 0)
				enm.MoveNext();
			return enm.Current;
		}
		
		public static object GetValue(object source, string name, int index)
		{
			var enumerable = GetValue(source, name) as IEnumerable;
			return GetValue(enumerable, index);
		}
		
		public static void ListInsert(object parent, string fieldName, object source, int index, object value)
		{
			Type listType = source.GetType();
			if (listType.IsArray)
			{
				Type type = Utilities.GetElementType(source);
				Type genericListType = typeof(List<>).MakeGenericType(type);
				var list = (IList)Activator.CreateInstance(genericListType);
				foreach (var e in (IEnumerable)source)
					list.Add(e);
				list.Insert(index, value);
				var array = Array.CreateInstance(type, list.Count);
				list.CopyTo(array, 0);
				Utilities.SetValue(parent, fieldName, array);
			}
			else
				((IList) source).Insert(index, value);
		}
		
		public static void ListRemove(object parent, string fieldName, object source, int index)
		{
			Type listType = source.GetType();
			if (listType.IsArray)
			{
				Type type = Utilities.GetElementType(source);
				Type genericListType = typeof(List<>).MakeGenericType(type);
				var list = (IList)Activator.CreateInstance(genericListType);
				foreach (var e in (IEnumerable)source)
					list.Add(e);
				list.RemoveAt(index);
				var array = Array.CreateInstance(type, list.Count);
				list.CopyTo(array, 0);
				Utilities.SetValue(parent, fieldName, array);
			}
			else
				((IList) source).RemoveAt(index);
		}

		public static Dictionary<A, T> FindByAttribute<A, T>(object source) where A : Attribute where T: MemberInfo
		{
			Dictionary<A, T> dictionary = new Dictionary<A, T>();
			if (source != null)
			{
				Type type = source.GetType();
				var members = type.GetMembers(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				foreach (var member in members)
					if (member is T)
						foreach (var attr in member.GetCustomAttributes(typeof(A), false))
							dictionary.Add((A)attr, (T)member);
			}
			return dictionary;
		}
	}
}
