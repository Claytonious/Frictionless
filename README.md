#Frictionless

A lightweight, simple dependency injection and message router framework for Unity apps.

Use this package to reduce coupling and keep your game flexible. Unlike other frameworks, Frictionless:

* Doesn't bring along a ton of baggage
* Is completely aware of `GameObject`, `MonoBehaviour`, and other Unity concepts and plays nicely with them
* Doesn't force a single, opinionated style onto your architecture

Frictionless gives you two tools:

* `ServiceFactory`: a dirt simple dependency injection container.
* `MessageRouter`: a flexible, general purpose message router.

#ServiceFactory
Use `ServiceFactory` to prevent your components from needing to reference one another directly and to prevent your components from needing to know the concrete types of services that they depend on. You can use it to track and instantiate singletons and/or regular instances of any class. It is aware of Unity's `MonoBehaviour` and will automatically create `GameObject`s to host those on if needed.

##Register and Resolve
Use a simple singleton like this:

```c#
// Your singleton MonoBehaviour ...
class Player : MonoBehaviour
{
	public void TakeDamage(int hitPoints)
	{
		// Argh!
	}
}

// In one place at startup, register a singleton instance of your player like this:
ServiceFactory.RegisterSingleton<Player>();

// A GameObject with the Player component is now in your scene

// Later, in any other class anywhere in the project, do stuff with Player:
ServiceFactory.Resolve<Player>().TakeDamage(100);
```

You can also code against interfaces so that the concrete implementation can change based on different contexts:
```c#
interface IStore
{
	bool Purchase(string sku);
}

// Elsewhere...
class AppleStore : IStore
{
	[DllImport]
	private static extern bool PurchaseNative(string sku);

	public bool Purchase(string sku)
	{
		return PurchaseNative(sku);
	}
}

// And still elsewhere...
class GooglePlayStore : IStore
{
	public bool Purchase(string sku)
	{
		using (AndroidJavaClass cls = new AndroidJavaClass("com.yourcompany.store")) 
		{ 
			return cls.CallStatic<bool>("purchase", sku);
		}
	} 
}

// And other stores...

// At game startup:
void Awake()
{
#if UNITY_IOS
	ServiceFactory.RegisterSingleton<IStore, AppleStore>();
#elif UNITY_ANDROID
	ServiceFactory.RegisterSingleton<IStore, GooglePlayStore>();
#endif
}

// Then at purchase time from anywhere else...

void HandlePurchaseGemsButtonTapped()
{
	ServiceFactory.Resolve<IStore>().Purchase("PackOfGems");
}
```

The same thing works with transient objects - instead of `RegisterSingleton` use `Register`. Callers will then receive new instances of the correct concrete type whenever they `Resolve` instead of getting a reference to the same instance.

##Route Messages
But this is still too hard wired because someone had to explicitly call `Player.TakeDamage`, so they needed knowledge of `Player`. Use `MessageRouter` to decouple even more!

```c#
// Once at startup:
void Awake()
{
	ServiceFactory.RegisterSingleton<MessageRouter>();
}

// Create a new message (messages can be *any* reference type)
public class PlayerDamageMessage
{
	public int HitPoints { get; set; }
}

// Subscribe to this message in relevant places:

public class Player : MonoBehaviour
{
	void Start()
	{
		ServiceFactory.Resolve<MessageRouter>().AddMessageHandler<PlayerDamageMessage>(HandleDamageTaken);
	}

	private void HandleDamageTaken(PlayerDamageMessage msg)
	{
		this.hitPoints -= msg.HitPoints;
		if (this.hitPoints <= 0)
			// Argh!!! Play a death animation or somefink!
	}
}

// Elsewhere...
public class PlayerHitPointsGUIWidget : MonoBehaviour
{
	void Start()
	{
		// You can use lambdas instead of methods if you like (OK under AOT on iOS)
		ServiceFactory.Resolve<MessageRouter>().AddMessageHandler<PlayerDamageMessage>((msg) =>
		{
			displayedHitPoints -= msg.HitPoints;
			myTextLabel.text = String.Format("{0:n0}", displayedHitPoints);
			myHealthBar.width = displayedHitPoints * 100;
		});
	}
}

// And elsewhere ...
public class ScreenBloodImageEffect : MonoBehaviour
{
	void Start()
	{
		ServiceFactory.Resolve<MessageRouter>().AddMessageHandler<PlayerDamageMessage>(HandleDamageTaken);
	}

	private void HandleDamageTaken(PlayerDamageMessage msg)
	{
		// Show blood stains on the screen in post process effects!
		bloodAmount = msg.HitPoints * 10;
	}
}

// When shit hits the fan, announce to the world that the player is taking damage
void Update()
{
	if (volcanoExploded)
	{
		ServiceFactory.Resolve<MessageRouter>().RaiseMessage(new PlayerDamageMessage() { HitPoints = 100 });
	}
}

// Everything in the system that cares will handle the message, but the volcano didn't need to know about any of them!
```

In real projects, it's typical to reuse a few message instances instead of `new`ing them each time you raise them (just change their properties and raise again). Regular GC best practices apply here.
