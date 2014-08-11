using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frictionless
{
	public class MessageRouter
	{
		private Dictionary<Type,List<MessageHandler>> handlers = new Dictionary<Type, List<MessageHandler>>();
		private List<Delegate> pendingRemovals = new List<Delegate>();
		private bool isRaisingMessage;

		public MessageRouter()
		{
		}

		public void AddHandler<T>(Action<T> handler)
		{
			List<MessageHandler> delegates = null;
			if (!handlers.TryGetValue(typeof(T), out delegates))
			{
				delegates = new List<MessageHandler>();
				handlers[typeof(T)] = delegates;
			}
			if (delegates.Find(x => x.Delegate == handler) == null)
				delegates.Add(new MessageHandler() { Target = handler.Target, Delegate = handler });
		}

		public void RemoveHandler<T>(Action<T> handler)
		{
			List<MessageHandler> delegates = null;
			if (handlers.TryGetValue(typeof(T), out delegates))
			{
				MessageHandler existingHandler = delegates.Find(x => x.Delegate == handler);
				if (existingHandler != null)
				{
					if (isRaisingMessage)
						pendingRemovals.Add(handler);
					else
						delegates.Remove(existingHandler);
				}
			}
		}

		public void RaiseMessage(object msg)
		{
			List<MessageHandler> delegates = null;
			if (handlers.TryGetValue(msg.GetType(), out delegates))
			{
				isRaisingMessage = true;
				try
				{
					foreach (MessageHandler h in delegates)
						h.Delegate.Method.Invoke(h.Target, new object[] { msg });
				}
				finally
				{
					isRaisingMessage = false;
				}
				foreach (Delegate d in pendingRemovals)
				{
					MessageHandler existingHandler = delegates.Find(x => x.Delegate == d);
					if (existingHandler != null)
						delegates.Remove(existingHandler);
				}
				pendingRemovals.Clear();
			}
		}

		public void Reset()
		{
			handlers.Clear();
		}

		public class MessageHandler
		{
			public object Target { get; set; }
			public Delegate Delegate { get; set; }
		}
	}
}
