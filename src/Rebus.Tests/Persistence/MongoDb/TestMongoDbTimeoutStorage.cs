﻿using System;
using System.Linq;
using NUnit.Framework;
using Rebus.MongoDb;
using Shouldly;

namespace Rebus.Tests.Persistence.MongoDb
{
    [TestFixture, Category(TestCategories.Mongo)]
    public class TestMongoDbTimeoutStorage : MongoDbFixtureBase
    {
        const string TimeoutsCollectionName = "timeouts";
        MongoDbTimeoutStorage storage;

        protected override void DoSetUp()
        {
            storage = new MongoDbTimeoutStorage(ConnectionString, TimeoutsCollectionName);
        }

        protected override void DoTearDown()
        {
            DropCollection(TimeoutsCollectionName);
        }

        [Test]
        public void DoesNotComplainWhenTheSameTimeoutIsAddedMultipleTimes()
        {
            var justSomeTime = new DateTime(2010, 1, 1, 10, 30, 0, DateTimeKind.Utc);

            storage.Add(new Timeout.Timeout { CorrelationId = "blah", ReplyTo = "blah blah", TimeToReturn = justSomeTime });
            storage.Add(new Timeout.Timeout { CorrelationId = "blah", ReplyTo = "blah blah", TimeToReturn = justSomeTime });
            storage.Add(new Timeout.Timeout { CorrelationId = "blah", ReplyTo = "blah blah", TimeToReturn = justSomeTime });
        }

        [Test]
        public void CanStoreAndRemoveTimeouts()
        {
            var justSomeUtcTimeStamp = new DateTime(2010, 3, 10, 12, 30, 15, DateTimeKind.Utc);
            var justAnotherUtcTimeStamp = justSomeUtcTimeStamp.AddHours(2);

            storage.Add(new Timeout.Timeout
            {
                CorrelationId = "first",
                ReplyTo = "somebody",
                TimeToReturn = justSomeUtcTimeStamp,
                CustomData = null,
            });

            var thirtytwoKilobytesOfDollarSigns = new string('$', 32768);

            storage.Add(new Timeout.Timeout
            {
                CorrelationId = "second",
                ReplyTo = "somebody",
                TimeToReturn = justAnotherUtcTimeStamp,
                CustomData = thirtytwoKilobytesOfDollarSigns,
            });

            TimeMachine.FixTo(justSomeUtcTimeStamp.AddSeconds(-1));

            var dueTimeoutsBeforeTimeout = storage.RemoveDueTimeouts().Count();
            dueTimeoutsBeforeTimeout.ShouldBe(0);

            TimeMachine.FixTo(justSomeUtcTimeStamp.AddSeconds(1));

            var dueTimeoutsAfterFirstTimeout = storage.RemoveDueTimeouts();
            dueTimeoutsAfterFirstTimeout.Count().ShouldBe(1);

            var timeout = dueTimeoutsAfterFirstTimeout.First();
            timeout.CorrelationId.ShouldBe("first");
            timeout.ReplyTo.ShouldBe("somebody");
            timeout.TimeToReturn.ShouldBe(justSomeUtcTimeStamp);

            TimeMachine.FixTo(justAnotherUtcTimeStamp.AddSeconds(1));

            var dueTimeoutsAfterSecondTimeout = storage.RemoveDueTimeouts();
            dueTimeoutsAfterSecondTimeout.Count().ShouldBe(1);

            var secondTimeout = dueTimeoutsAfterSecondTimeout.First();
            secondTimeout.CorrelationId.ShouldBe("second");
            secondTimeout.ReplyTo.ShouldBe("somebody");
            secondTimeout.TimeToReturn.ShouldBe(justAnotherUtcTimeStamp);
            secondTimeout.CustomData.ShouldBe(thirtytwoKilobytesOfDollarSigns);
        }
    }
}