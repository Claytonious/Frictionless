using System;
using Frictionless;
using NUnit.Framework;

public class MessageRouterTests
{
    [Test]
    public void Routes()
    {
        var numberOfTestMessagesReceived = 0;
        MessageRouter.AddHandler<TestMessage>((msg) =>
        {
            ++numberOfTestMessagesReceived;
            Assert.AreEqual("Foo", msg.Text);
            Assert.AreEqual(7, msg.Number);
        });
        var testMsg = new TestMessage
        {
            Text = "Foo",
            Number = 7
        };
        MessageRouter.RaiseMessage(testMsg);
        Assert.AreEqual(1, numberOfTestMessagesReceived);
    }

    [Test]
    public void AddsAndRemovesAreIdempotent()
    {
        var numberOfTestMessagesReceived = 0;

        Action<TestMessage> handler = (msg) =>
        {
            ++numberOfTestMessagesReceived;
        };

        MessageRouter.AddHandler(handler);

        // Test that basic routing happens...
        MessageRouter.RaiseMessage(new TestMessage());
        Assert.AreEqual(1, numberOfTestMessagesReceived);
        MessageRouter.RaiseMessage(new TestMessage());
        Assert.AreEqual(2, numberOfTestMessagesReceived);

        // Subscribing the same handler again should be idempotent...
        MessageRouter.AddHandler(handler);
        MessageRouter.RaiseMessage(new TestMessage());
        Assert.AreEqual(3, numberOfTestMessagesReceived);

        MessageRouter.AddHandler(handler);
        MessageRouter.RaiseMessage(new TestMessage());
        Assert.AreEqual(4, numberOfTestMessagesReceived);

        // Test unsubscribing...
        MessageRouter.RemoveHandler(handler);
        MessageRouter.RaiseMessage(new TestMessage());
        Assert.AreEqual(4, numberOfTestMessagesReceived);
    }
} 
