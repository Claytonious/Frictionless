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
		private static readonly Dictionary<Type,Type> _singletons = new();
		private static readonly Dictionary<Type,Type> _transients = new();
		private static readonly Dictionary<Type,object> _singletonInstances = new();
		private static bool _isReinitializingSingletons;
		private static readonly Dictionary<Type,Type> _pendingSingletons = new();
		private static readonly Dictionary<Type,object> _pendingSingletonInstances = new();

		public static bool IsEmpty => _singletons.Count == 0 && _transients.Count == 0;

		public static void Reset(bool keepMultisceneSingletons = true)
		{
			var survivorRegisteredTypes = new List<Type>();
			var survivors = new List<object>();
			foreach(var pair in _singletonInstances)
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
			_singletons.Clear();
			_transients.Clear();
			_singletonInstances.Clear();
			_pendingSingletons.Clear();

			for (var i = 0; i < survivors.Count; i++)
			{
				_singletonInstances[survivorRegisteredTypes[i]] = survivors[i];
				_singletons[survivorRegisteredTypes[i]] = survivors[i].GetType();
			}
		}

		public static void RegisterSingleton<TConcrete>(Type abstractType, TConcrete instance)
		{
			if (_isReinitializingSingletons)
			{
				_pendingSingletons[abstractType] = typeof(TConcrete);
				_pendingSingletonInstances[abstractType] = instance;
				return;
			}
			_singletons[abstractType] = typeof(TConcrete);
			_singletonInstances[abstractType] = instance;
		}

		public static void RegisterSingleton<TConcrete>()
		{
			if (_isReinitializingSingletons)
			{
				_pendingSingletons[typeof(TConcrete)] = typeof(TConcrete);
				return;
			}
			_singletons[typeof(TConcrete)] = typeof(TConcrete);
		}

		public static void RegisterSingleton<TAbstract,TConcrete>()
		{
			if (_isReinitializingSingletons)
			{
				_pendingSingletons[typeof(TAbstract)] = typeof(TConcrete);
				return;
			}
			_singletons[typeof(TAbstract)] = typeof(TConcrete);
		}
		
		public static void RegisterSingleton<TConcrete>(TConcrete instance, bool onlyIfNotExists = false) where TConcrete : class
		{
			if (onlyIfNotExists && Resolve<TConcrete>() != default)
			{
				return;
			}
			if (_isReinitializingSingletons)
			{
				_pendingSingletons[typeof(TConcrete)] = typeof(TConcrete);
				_pendingSingletonInstances[typeof(TConcrete)] = instance;
				return;
			}
			_singletons[typeof(TConcrete)] = typeof(TConcrete);
			_singletonInstances[typeof(TConcrete)] = instance;
		}

		public static void RegisterTransient<TAbstract,TConcrete>()
		{
			_transients[typeof(TAbstract)] = typeof(TConcrete);
		}

		public static T Resolve<T>() where T : class
		{
			return Resolve<T>(false);
		}

		public static T Resolve<T>(bool onlyExisting) where T : class
		{
			T result = default(T);
			if (_singletons.TryGetValue(typeof(T), out var concreteType))
			{
				object r = null;
				if (!_singletonInstances.TryGetValue(typeof(T), out r) && !onlyExisting)
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
						r = Activator.CreateInstance(concreteType);
					_singletonInstances[typeof(T)] = r;
				}
				result = (T)r;
			}
			else if (_transients.TryGetValue(typeof(T), out concreteType))
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
			foreach(var pair in _singletonInstances)
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

			foreach (var pair in _pendingSingletons)
			{
				_singletons[pair.Key] = pair.Value;
			}

			foreach (var pair in _pendingSingletonInstances)
			{
				_singletonInstances[pair.Key] = pair.Value;
			}

			_pendingSingletons.Clear();
			_pendingSingletonInstances.Clear();
			_isReinitializingSingletons = false;
		}
	}
}
