using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// A simple, *single-threaded*, service locator appropriate for use with Unity.
/// </summary>

namespace Frictionless
{
	public class ServiceFactory
	{
		private static ServiceFactory instance;

		private Dictionary<Type,Type> singletons = new Dictionary<Type, Type>();
		private Dictionary<Type,Type> transients = new Dictionary<Type, Type>();
		private Dictionary<Type,object> singletonInstances = new Dictionary<Type, object>();

		static ServiceFactory()
		{
			instance = new ServiceFactory();
		}

		protected ServiceFactory()
		{
		}

		public static ServiceFactory Instance
		{
			get { return instance; }
		}

		public bool IsEmpty
		{
			get { return singletons.Count == 0 && transients.Count == 0; }
		}

		public void HandleNewSceneLoaded()
		{
			List<IMultiSceneSingleton> multis = new List<IMultiSceneSingleton>();
			foreach(KeyValuePair<Type,object> pair in singletonInstances)
			{
				IMultiSceneSingleton multi = pair.Value as IMultiSceneSingleton;
				if (multi != null)
					multis.Add (multi);
			}
			foreach(var multi in multis)
			{
				MonoBehaviour behavior = multi as MonoBehaviour;
				if (behavior != null)
					behavior.StartCoroutine(multi.HandleNewSceneLoaded());
			}
		}

		public void Reset()
		{
			List<Type> survivorRegisteredTypes = new List<Type>();
			List<object> survivors = new List<object>();
			foreach(KeyValuePair<Type,object> pair in singletonInstances)
			{
				if (pair.Value is IMultiSceneSingleton)
				{
					survivors.Add(pair.Value);
					survivorRegisteredTypes.Add(pair.Key);
				}
			}
			singletons.Clear();
			transients.Clear();
			singletonInstances.Clear();

			for (int i = 0; i < survivors.Count; i++)
			{
				singletonInstances[survivorRegisteredTypes[i]] = survivors[i];
				singletons[survivorRegisteredTypes[i]] = survivors[i].GetType();
			}
		}

		public void RegisterSingleton<TConcrete>()
		{
			singletons[typeof(TConcrete)] = typeof(TConcrete);
		}

		public void RegisterSingleton<TAbstract,TConcrete>()
		{
			singletons[typeof(TAbstract)] = typeof(TConcrete);
		}
		
		public void RegisterSingleton<TConcrete>(TConcrete instance)
		{
			singletons[typeof(TConcrete)] = typeof(TConcrete);
			singletonInstances[typeof(TConcrete)] = instance;
		}

		public void RegisterTransient<TAbstract,TConcrete>()
		{
			transients[typeof(TAbstract)] = typeof(TConcrete);
		}

		public T Resolve<T>() where T : class
		{
			return Resolve<T>(false);
		}

		public T Resolve<T>(bool onlyExisting) where T : class
		{
			T result = default(T);
			Type concreteType = null;
			if (singletons.TryGetValue(typeof(T), out concreteType))
			{
				object r = null;
				if (!singletonInstances.TryGetValue(typeof(T), out r) && !onlyExisting)
				{
	#if NETFX_CORE
					if (concreteType.GetTypeInfo().IsSubclassOf(typeof(MonoBehaviour)))
	#else
					if (concreteType.IsSubclassOf(typeof(MonoBehaviour)))
	#endif
					{
						GameObject singletonGameObject = new GameObject();
						r = singletonGameObject.AddComponent(concreteType);
						singletonGameObject.name = typeof(T).ToString() + " (singleton)";
					}
					else
						r = Activator.CreateInstance(concreteType);
					singletonInstances[typeof(T)] = r;

					IMultiSceneSingleton multi = r as IMultiSceneSingleton;
					if (multi != null)
						multi.HandleNewSceneLoaded();
				}
				result = (T)r;
			}
			else if (transients.TryGetValue(typeof(T), out concreteType))
			{
				object r = Activator.CreateInstance(concreteType);
				result = (T)r;
			}
			return result;
		}
	}
}
