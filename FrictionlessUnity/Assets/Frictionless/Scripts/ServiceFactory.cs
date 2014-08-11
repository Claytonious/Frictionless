using System;
using System.Collections.Generic;

/// <summary>
/// A simple, *single-threaded*, dependency injection container appropriate for use with Unity.
/// </summary>
using UnityEngine;

public class ServiceFactory
{
	private static Dictionary<Type,Type> singletons = new Dictionary<Type, Type>();
	private static Dictionary<Type,Type> transients = new Dictionary<Type, Type>();
	private static Dictionary<Type,object> singletonInstances = new Dictionary<Type, object>();

	private ServiceFactory()
	{
	}

	public static void Reset()
	{
		singletons.Clear();
		transients.Clear();
		singletonInstances.Clear();
	}

	public static void RegisterSingleton<TConcrete>()
	{
		singletons[typeof(TConcrete)] = typeof(TConcrete);
	}

	public static void RegisterSingleton<TAbstract,TConcrete>()
	{
		singletons[typeof(TAbstract)] = typeof(TConcrete);
	}
	
	public static void RegisterSingleton<TConcrete>(TConcrete instance)
	{
		singletons[typeof(TConcrete)] = typeof(TConcrete);
		singletonInstances[typeof(TConcrete)] = instance;
	}

	public static void RegisterTransient<TAbstract,TConcrete>()
	{
		transients[typeof(TAbstract)] = typeof(TConcrete);
	}

	public static T Resolve<T>() where T : class
	{
		T result = default(T);
		Type concreteType = null;
		if (singletons.TryGetValue(typeof(T), out concreteType))
		{
			object r = null;
			if (!singletonInstances.TryGetValue(typeof(T), out r))
			{
				if (concreteType.IsSubclassOf(typeof(MonoBehaviour)))
				{
					GameObject singletonGameObject = new GameObject();
					r = singletonGameObject.AddComponent(concreteType);
					singletonGameObject.name = typeof(T).ToString() + " (singleton)";
				}
				else
					r = concreteType.GetConstructor(Type.EmptyTypes).Invoke(new object[] { });
				singletonInstances[typeof(T)] = r;
			}
			result = (T)r;
		}
		else if (transients.TryGetValue(typeof(T), out concreteType))
		{
			object r = concreteType.GetConstructor(Type.EmptyTypes).Invoke(new object[] { });
			result = (T)r;
		}
		return result;
	}
}
