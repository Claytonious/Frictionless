using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Frictionless
{
	/// <summary>
	/// A simple, *single-threaded*, service locator appropriate for use with Unity.
	/// </summary>
	public static class ServiceFactory
	{
		private static readonly Dictionary<Type,Type> Singletons = new();
		private static readonly Dictionary<Type,Type> Transients = new();
		private static readonly Dictionary<Type,object> SingletonInstances = new();
		private static bool _isReinitializingSingletons;
		private static readonly Dictionary<Type,Type> PendingSingletons = new();
		private static readonly Dictionary<Type,object> PendingSingletonInstances = new();

		public static bool IsEmpty => Singletons.Count == 0 && Transients.Count == 0;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void InitializeDomain()
		{
			Singletons.Clear();
			Transients.Clear();
			SingletonInstances.Clear();
			PendingSingletons.Clear();
			PendingSingletonInstances.Clear();
		}

		public static void Reset(bool keepMultisceneSingletons = true)
		{
			var survivorRegisteredTypes = new List<Type>();
			var survivors = new List<object>();
			foreach(var pair in SingletonInstances)
			{
				if (pair.Value is IMultiSceneSingleton)
				{
					if (keepMultisceneSingletons)
					{
						survivors.Add(pair.Value);
						survivorRegisteredTypes.Add(pair.Key);
					}
					else if (pair.Value is MonoBehaviour monoBehaviour)
					{
						UnityEngine.Object.Destroy(monoBehaviour);
					}
				}
			}
			Singletons.Clear();
			Transients.Clear();
			SingletonInstances.Clear();
			PendingSingletons.Clear();
			PendingSingletonInstances.Clear();

			for (var i = 0; i < survivors.Count; i++)
			{
				SingletonInstances[survivorRegisteredTypes[i]] = survivors[i];
				Singletons[survivorRegisteredTypes[i]] = survivors[i].GetType();
			}
		}

		public static void RegisterSingleton<TConcrete>(Type abstractType, TConcrete instance)
		{
			if (_isReinitializingSingletons)
			{
				PendingSingletons[abstractType] = typeof(TConcrete);
				PendingSingletonInstances[abstractType] = instance;
				return;
			}
			Singletons[abstractType] = typeof(TConcrete);
			SingletonInstances[abstractType] = instance;
		}

		public static void RegisterSingleton<TConcrete>()
		{
			if (_isReinitializingSingletons)
			{
				PendingSingletons[typeof(TConcrete)] = typeof(TConcrete);
				return;
			}
			Singletons[typeof(TConcrete)] = typeof(TConcrete);
		}

		public static void RegisterSingleton<TAbstract,TConcrete>()
		{
			if (_isReinitializingSingletons)
			{
				PendingSingletons[typeof(TAbstract)] = typeof(TConcrete);
				return;
			}
			Singletons[typeof(TAbstract)] = typeof(TConcrete);
		}
		
		public static void RegisterSingleton<TConcrete>(TConcrete instance, bool onlyIfNotExists = false) where TConcrete : class
		{
			if (onlyIfNotExists && Resolve<TConcrete>() != default)
			{
				return;
			}
			if (_isReinitializingSingletons)
			{
				PendingSingletons[typeof(TConcrete)] = typeof(TConcrete);
				PendingSingletonInstances[typeof(TConcrete)] = instance;
				return;
			}
			Singletons[typeof(TConcrete)] = typeof(TConcrete);
			SingletonInstances[typeof(TConcrete)] = instance;
		}

		public static void RegisterTransient<TAbstract,TConcrete>()
		{
			Transients[typeof(TAbstract)] = typeof(TConcrete);
		}

		public static T Resolve<T>() where T : class
		{
			return Resolve<T>(false);
		}

		public static T Resolve<T>(bool onlyExisting) where T : class
		{
			T result = default(T);
			Type concreteType = null;
			if (Singletons.TryGetValue(typeof(T), out concreteType))
			{
				object r = null;
				if (!SingletonInstances.TryGetValue(typeof(T), out r) && !onlyExisting)
				{
	#if NETFX_CORE
					if (concreteType.GetTypeInfo().IsSubclassOf(typeof(MonoBehaviour)))
	#else
					if (concreteType.IsSubclassOf(typeof(MonoBehaviour)))
	#endif
					{
						GameObject singletonGameObject = new GameObject();
						r = singletonGameObject.AddComponent(concreteType);
						singletonGameObject.name = $"{typeof(T)} (singleton)";
					}
					else
					{
						r = Activator.CreateInstance(concreteType);
					}
					SingletonInstances[typeof(T)] = r;
				}
				result = (T)r;
			}
			else if (Transients.TryGetValue(typeof(T), out concreteType))
			{
				object r = Activator.CreateInstance(concreteType);
				result = (T)r;
			}
			return result;
		}

		public static IEnumerator HandleSceneLoad(AsyncOperation sceneLoadOperation)
		{
			yield return sceneLoadOperation;
			HandleSceneLoaded();
		}

		public static void HandleSceneLoaded()
		{
			_isReinitializingSingletons = true;
			foreach(var pair in SingletonInstances)
			{
				if (pair.Value is IReinitializingMultiSceneSingleton reinitializingMultiSceneSingleton)
				{
					try
					{
						reinitializingMultiSceneSingleton.ReinitializeAfterSceneLoad();
					}
					catch (Exception ex)
					{
						Debug.LogError($"Exception servicing ReinitializeAfterSceneLoad on {reinitializingMultiSceneSingleton}: {ex}");
					}
				}
			}

			foreach (var pair in PendingSingletons)
			{
				Singletons[pair.Key] = pair.Value;
			}

			foreach (var pair in PendingSingletonInstances)
			{
				SingletonInstances[pair.Key] = pair.Value;
			}

			PendingSingletons.Clear();
			PendingSingletonInstances.Clear();
			_isReinitializingSingletons = false;
		}
	}
}
