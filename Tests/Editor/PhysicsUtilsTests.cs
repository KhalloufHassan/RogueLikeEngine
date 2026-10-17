using System;
using System.Collections.Generic;
using NUnit.Framework;
using RogueLikeEngine.Utils;
using UnityEngine;

namespace RogueLikeEngine.Tests
{
    public class PhysicsUtilsTests
    {
        private const int TestLayer = 31;
        private const int TestMask = 1 << TestLayer;
        private static readonly Vector3 Center = new(10000, 10000, 10000);
        private TestObjects m_objects;

        [SetUp]
        public void SetUp() => m_objects = new TestObjects();

        [TearDown]
        public void TearDown() => m_objects.Dispose();

        private BoxCollider CreateCollider(Vector3 position, bool trigger = false, int layer = TestLayer)
        {
            BoxCollider collider = m_objects.CreateComponent<BoxCollider>();
            collider.transform.position = position;
            collider.size = Vector3.one * .2f;
            collider.gameObject.layer = layer;
            collider.isTrigger = trigger;
            return collider;
        }

        private static IEnumerable<Collider> Query(int shape, Vector3 center,
            QueryTriggerInteraction triggers = QueryTriggerInteraction.Ignore) => shape switch
        {
            0 => PhysicsUtils.OverlapSphere(center, 1, TestMask, triggers),
            1 => PhysicsUtils.OverlapCapsule(center - Vector3.up, center + Vector3.up, 1, TestMask, triggers),
            2 => PhysicsUtils.OverlapBox(center, Vector3.one, Quaternion.identity, TestMask, triggers),
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void NestedQueriesDoNotOverwriteOuterResults(int shape)
        {
            Collider first = CreateCollider(Center);
            Collider second = CreateCollider(Center + Vector3.right * .5f);
            Collider other = CreateCollider(Center + Vector3.right * 10);
            Physics.SyncTransforms();

            HashSet<Collider> visited = new();
            foreach (Collider collider in Query(shape, Center))
            {
                int innerCount = 0;
                foreach (Collider hit in Query((shape + 1) % 3, other.transform.position))
                {
                    Assert.AreSame(other, hit);
                    innerCount++;
                }
                Assert.AreEqual(1, innerCount);
                visited.Add(collider);
            }
            CollectionAssert.AreEquivalent(new[] { first, second }, visited);
        }

        [TestCase(0, 32)]
        [TestCase(1, 32)]
        [TestCase(2, 32)]
        [TestCase(0, 129)]
        [TestCase(1, 129)]
        [TestCase(2, 129)]
        public void FullBuffersGrowWithoutLosingHits(int shape, int count)
        {
            HashSet<Collider> expected = new();
            for (int i = 0; i < count; i++) expected.Add(CreateCollider(Center));
            Physics.SyncTransforms();

            int actual = 0;
            foreach (Collider collider in Query(shape, Center))
            {
                Assert.IsTrue(expected.Remove(collider));
                actual++;
            }
            Assert.AreEqual(count, actual);
            Assert.IsEmpty(expected);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void QueriesRespectLayersTriggersAndShape(int shape)
        {
            Collider solid = CreateCollider(Center);
            Collider trigger = CreateCollider(Center, true);
            Collider capsuleEnd = CreateCollider(Center + Vector3.up * 1.6f);
            CreateCollider(Center, layer: 0);
            Physics.SyncTransforms();

            int ignoredCount = 0;
            foreach (Collider collider in Query(shape, Center))
            {
                Assert.IsTrue(collider == solid || shape == 1 && collider == capsuleEnd);
                ignoredCount++;
            }
            Assert.AreEqual(shape == 1 ? 2 : 1, ignoredCount);

            int includedCount = 0;
            bool foundTrigger = false;
            foreach (Collider collider in Query(shape, Center, QueryTriggerInteraction.Collide))
            {
                foundTrigger |= collider == trigger;
                includedCount++;
            }
            Assert.AreEqual(shape == 1 ? 3 : 2, includedCount);
            Assert.IsTrue(foundTrigger);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void EmptyQueriesDoNotExposePreviousHits(int shape)
        {
            CreateCollider(Center);
            Physics.SyncTransforms();
            Assert.AreEqual(1, ReadQuery(shape, Center));
            Assert.AreEqual(0, ReadQuery(shape, Center + Vector3.right * 10));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void QueryRunsWhenEnumeratedAndCanRunAgain(int shape)
        {
            IEnumerable<Collider> query = Query(shape, Center);
            CreateCollider(Center);
            Physics.SyncTransforms();
            Assert.AreEqual(1, ReadQuery(query));

            CreateCollider(Center);
            Physics.SyncTransforms();
            Assert.AreEqual(2, ReadQuery(query));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BoxRespectsOrientationLayersAndTriggers(bool rotated)
        {
            Collider origin = CreateCollider(Center);
            Collider right = CreateCollider(Center + Vector3.right * 1.5f);
            Collider forward = CreateCollider(Center + Vector3.forward * 1.5f);
            Collider trigger = CreateCollider(Center, true);
            CreateCollider(Center, layer: 0);
            Physics.SyncTransforms();

            Quaternion orientation = rotated ? Quaternion.Euler(0, 90, 0) : Quaternion.identity;
            Vector3 halfExtents = new(2, .25f, .25f);
            IEnumerable<Collider> ignored = PhysicsUtils.OverlapBox(Center, halfExtents, orientation,
                TestMask, QueryTriggerInteraction.Ignore);
            Assert.AreEqual(2, ReadQuery(ignored));
            HashSet<Collider> expected = new() { origin, rotated ? forward : right };
            foreach (Collider hit in ignored) Assert.IsTrue(expected.Remove(hit));
            Assert.IsEmpty(expected);

            IEnumerable<Collider> included = PhysicsUtils.OverlapBox(Center, halfExtents, orientation,
                TestMask, QueryTriggerInteraction.Collide);
            Assert.AreEqual(3, ReadQuery(included));
            bool foundTrigger = false;
            foreach (Collider hit in included) foundTrigger |= hit == trigger;
            Assert.IsTrue(foundTrigger);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void EnumerationReusesBuffersAfterWarmup(int exitMode)
        {
            CreateCollider(Center);
            CreateCollider(Center);
            Physics.SyncTransforms();
            for (int shape = 0; shape < 3; shape++) ReadQuery(Query(shape, Center), exitMode);

            IEnumerable<Collider>[] queries = new IEnumerable<Collider>[96];
            for (int i = 0; i < queries.Length; i++) queries[i] = Query(i % 3, Center);
            GC.GetAllocatedBytesForCurrentThread();

            long before = GC.GetAllocatedBytesForCurrentThread();
            int hits = 0;
            foreach (IEnumerable<Collider> query in queries) hits += ReadQuery(query, exitMode);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.AreEqual(exitMode == 0 ? 192 : 96, hits);
            Assert.AreEqual(0, allocated);
        }

        private static int ReadQuery(int shape, Vector3 center) => ReadQuery(Query(shape, center));

        private static int ReadQuery(IEnumerable<Collider> query, int exitMode = 0)
        {
            int count = 0;
            foreach (Collider collider in query)
            {
                count++;
                if (exitMode == 1) break;
                if (exitMode == 2) return count;
            }
            return count;
        }
    }
}
