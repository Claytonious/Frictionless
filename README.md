# Frictionless

A lightweight, simple service locator and message router framework for Unity apps.

Use this package to reduce coupling and keep your game flexible. Unlike other frameworks, Frictionless:

* Doesn't bring along a ton of baggage
* Is completely aware of `GameObject`, `MonoBehaviour`, and other Unity concepts and plays nicely with them
* Doesn't force a single, opinionated style onto your architecture

Frictionless gives you two tools:

* `ServiceFactory`: a dirt simple service locator.
* `MessageRouter`: a flexible, general purpose message router.

# Installation
Download the .unitypackage from the Releases tab above and import into your project.

# ServiceFactory
Use `ServiceFactory` to prevent your components from needing to reference one another directly and to prevent your components from needing to know the concrete types of services that they depend on. You can use it to track and instantiate singletons and/or regular instances of any class. It is aware of Unity's `MonoBehaviour` and will automatically create `GameObject`s to host those on if needed.

## Register and Resolve
Use a simple singleton like this:

```csharp
// Your singleton MonoBehaviour ...
class Player : MonoBehaviour
{
	public void TakeDamage(int hitPoints)
	{
		// Argh!
	}
}

// In one place at startup, register a singleton instance of your player like this:
ServiceFactory.Instance.RegisterSingleton<Player>();

// A GameObject with the Player component is now in your scene

// Later, in any other class anywhere in the project, do stuff with Player:
ServiceFactory.Instance.Resolve<Player>().TakeDamage(100);
```

You can also code against interfaces so that the concrete implementation can change based on different contexts:
```csharp
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
	ServiceFactory.Instance.RegisterSingleton<IStore, AppleStore>();
#elif UNITY_ANDROID
	ServiceFactory.Instance.RegisterSingleton<IStore, GooglePlayStore>();
#endif
}

// Then at purchase time from anywhere else...

void HandlePurchaseGemsButtonTapped()
{
	ServiceFactory.Instance.Resolve<IStore>().Purchase("PackOfGems");
}
```

The same thing works with transient objects - instead of `RegisterSingleton` use `Register`. Callers will then receive new instances of the correct concrete type whenever they `Resolve` instead of getting a reference to the same instance.

## Route Messages
But this is still too hard wired because someone had to explicitly call `Player.TakeDamage`, so they needed knowledge of `Player`. Use `MessageRouter` to decouple even more!

```csharp
// Once at startup:
void Awake()
{
	ServiceFactory.Instance.RegisterSingleton<MessageRouter>();
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
		ServiceFactory.Instance.Resolve<MessageRouter>().AddMessageHandler<PlayerDamageMessage>(HandleDamageTaken);
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
		ServiceFactory.Instance.Resolve<MessageRouter>().AddMessageHandler<PlayerDamageMessage>((msg) =>
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
		ServiceFactory.Instance.Resolve<MessageRouter>().AddMessageHandler<PlayerDamageMessage>(HandleDamageTaken);
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
		ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new PlayerDamageMessage() { HitPoints = 100 });
	}
}

// Everything in the system that cares will handle the message, but the volcano didn't need to know about any of them!
```

In real projects, it's typical to reuse a few message instances instead of `new`ing them each time you raise them (just change their properties and raise again). Regular GC best practices apply here.

# Changing Scenes and Unloading
If you change scenes in your game, then you need to clear your message handlers and service registrations to prevent accidentally carrying references to dead objects across to the new scene. Doing this is simple:

```csharp
// First clear all message handlers and service registrations
ServiceFactory.Instance.Resolve<MessageRouter>().Reset();
ServiceFactory.Instance.Reset();

// Then load your new scene
Application.LoadLevel("foo");
```

If you actually *want* message handlers to live across scene loads, then just use `RemoveHandler` for all of the ones that should be removed, and don't call `Reset`. This requires a certain amount of discipline - you have to remember to `RemoveHandler` for every `AddHandler` that shouldn't survive into the new scene, but it's easy enought to do.

Or, implement the interface `IMultiSceneSingleton`. The `ServiceFactory` will automatically treat these as long-lived across the entire application lifetime. Be sure to initialize these with `DontDestroyOnLoad` when you create them, as you normally would with any Unity object that's going to outlive the scene it's in.

# MVVM and Higher Architecture
With `ServiceFactory` and `MessageRouter`, you have all of the building blocks that you need to implement MVVM or other patterns that separate view from logic. Frictionless itself doesn't care - you're free to go full MVVM or to apply a smaller subset of that pattern to only those places where you feel it adds genuine value.

# Why?
There are many existing frameworks like Strange IoC that provide this kind of functionality, but I hate them because they're unnecessarily heavy and opinionated. In contrast, Frictionless does *not* commit these sins:

* Force you to arrange your hierarchy of GameObjects in ways that accomodate message routing. Transforms have parents for the sake of applying transformations - NOT for the sake of organizing objects into contexts for message routing! Frictionless doesn't force any kind of hierarchy on you at all - do what's right for your game!
* Force you to apply MVVM or other patterns - though you're welcome to if you like!
* Shackle you into heavy, verbose, repetitive constructs throughout your project. You're encouraged to use message routing and service resolution *in only those places where it actually helps*! It's quite fine to forego these in places where they're not needed. Be pragmatic and use these tools only where they help!
* Force you to use attributes that cause "magic" to happen at runtime - magic that is hard to debug when things break in production across platforms. You can easily debug every call into `Resolve` or `RaiseMessage` to see exactly what's happening (which is very little, by the way!)
