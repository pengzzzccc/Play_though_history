using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnknownTechnology.Core.Events;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnknownTechnology.Tests.EditMode
{
    public sealed class EventBusTests
    {
        private readonly struct TestMessage
        {
            public TestMessage(int value)
            {
                Value = value;
            }

            public int Value { get; }
        }

        [Test]
        public void Publish_DeliversStronglyTypedMessage()
        {
            var bus = new EventBus();
            var received = 0;
            bus.Subscribe<TestMessage>(message => received = message.Value);

            bus.Publish(new TestMessage(42));

            Assert.That(received, Is.EqualTo(42));
        }

        [Test]
        public void Dispose_IsIdempotentAndStopsFutureDelivery()
        {
            var bus = new EventBus();
            var calls = 0;
            var subscription = bus.Subscribe<TestMessage>(_ => calls++);

            subscription.Dispose();
            subscription.Dispose();
            bus.Publish(new TestMessage(1));

            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void UnsubscribeInsideCallback_DoesNotChangeCurrentSnapshot()
        {
            var bus = new EventBus();
            var firstCalls = 0;
            var secondCalls = 0;
            IDisposable first = null;
            first = bus.Subscribe<TestMessage>(_ =>
            {
                firstCalls++;
                first.Dispose();
            });
            bus.Subscribe<TestMessage>(_ => secondCalls++);

            bus.Publish(new TestMessage(1));
            bus.Publish(new TestMessage(2));

            Assert.That(firstCalls, Is.EqualTo(1));
            Assert.That(secondCalls, Is.EqualTo(2));
        }

        [Test]
        public void SubscriberException_IsLoggedAndOtherSubscribersContinue()
        {
            var bus = new EventBus();
            var healthySubscriberCalled = false;
            bus.Subscribe<TestMessage>(_ => throw new InvalidOperationException("event bus test failure"));
            bus.Subscribe<TestMessage>(_ => healthySubscriberCalled = true);
            LogAssert.Expect(LogType.Exception, new Regex("event bus test failure"));

            bus.Publish(new TestMessage(1));

            Assert.That(healthySubscriberCalled, Is.True);
        }

        [Test]
        public void Clear_RemovesEverySubscription()
        {
            var bus = new EventBus();
            var calls = 0;
            bus.Subscribe<TestMessage>(_ => calls++);
            bus.Clear();

            bus.Publish(new TestMessage(1));

            Assert.That(calls, Is.Zero);
        }
    }
}
