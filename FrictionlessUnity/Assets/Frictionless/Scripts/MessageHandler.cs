using System;

namespace Frictionless
{
	public class MessageHandler
	{
		public Type MessageType { get; set; }
		public object Target { get; set; }
		public Delegate Delegate { get; set; }
		public HandlerPersistence Persistence { get; set; }
	}
}
