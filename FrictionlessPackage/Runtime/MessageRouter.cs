using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Frictionless
{
	public static class MessageRouter
	{
		private static readonly HashSet<Action> Clearers = new();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void InitializeDomain()
		{
			Reset();
		}

		public static void AddHandler<T>(Action<T> handler)
		{
			var handlers = MessageHandler<T>.Handlers;
			if (handlers.All(h => h != handler))
			{
				handlers.Add(handler);
				Clearers.Add(MessageHandler<T>.Clear);
			}
		}

		public static void RemoveHandler<T>(Action<T> handlerToRemove)
		{
			if (MessageHandler<T>._isRaisingMessage)
			{
				MessageHandler<T>.PendingRemovals.Add(handlerToRemove);
			}
			else
			{
				MessageHandler<T>.Handlers.Remove(handlerToRemove);
			}
		}

		public static void Reset()
		{
			foreach (var clearer in Clearers)
			{
				clearer.Invoke();
			}
		}

		public static void RaiseMessage<T>(T msg)
		{
			MessageHandler<T>.RaiseAll(msg);
		}

		public class MessageHandlerBase
		{
		}

		public class MessageHandler<T> : MessageHandlerBase
		{
			public static bool _isRaisingMessage;
			public static readonly List<Action<T>> Handlers = new();
			public static readonly List<Action<T>> PendingRemovals = new();

			public static void Clear()
			{
				Handlers.Clear();
				PendingRemovals.Clear();
			}

			public static void RaiseAll(T msg)
			{
				_isRaisingMessage = true;
				try
				{
					foreach (var handler in Handlers)
					{
						try
						{
							handler.Invoke(msg);
						}
						catch (Exception ex)
						{
							UnityEngine.Debug.LogError($"Exception while raising message {msg}: {ex}");
						}
					}
				}
				finally
				{
					_isRaisingMessage = false;
				}
				if (PendingRemovals.Count > 0)
				{
					Handlers.RemoveAll(h => PendingRemovals.Contains(h));
					PendingRemovals.Clear();
				}
			}
		}
	}
}
