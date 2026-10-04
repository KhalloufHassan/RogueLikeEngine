using System.Collections.Generic;
using NUnit.Framework;
using RogueLikeEngine.Extensions;

namespace RogueLikeEngine.Tests
{
    public class CollectionsExtensionsTests
    {
        private static readonly List<int> List = new() { 10, 11, 12, 13 };
        private static bool Never(int _) => false;
        private static bool IsEven(int x) => x % 2 == 0;
        private static bool IsOdd(int x) => x % 2 == 1;

        [TestCase(0)]
        [TestCase(3)]
        [TestCase(-1)]
        [TestCase(99)]
        public void FirstOrDefaultStartingAtIndex_NoMatch_ReturnsDefault(int index)
        {
            Assert.AreEqual(0, List.FirstOrDefaultStartingAtIndex(index, Never));
        }

        [TestCase(0)]
        [TestCase(3)]
        [TestCase(-1)]
        [TestCase(99)]
        public void LastOrDefaultStartingAtIndex_NoMatch_ReturnsDefault(int index)
        {
            Assert.AreEqual(0, List.LastOrDefaultStartingAtIndex(index, Never));
        }

        [Test]
        public void StartingAtIndex_SingleElementNoMatch_ReturnsDefault()
        {
            List<int> single = new() { 10 };

            Assert.AreEqual(0, single.FirstOrDefaultStartingAtIndex(0, Never));
            Assert.AreEqual(0, single.LastOrDefaultStartingAtIndex(0, Never));
        }

        [Test]
        public void StartingAtIndex_Empty_ReturnsDefault()
        {
            List<int> empty = new();

            Assert.AreEqual(0, empty.FirstOrDefaultStartingAtIndex(0, _ => true));
            Assert.AreEqual(0, empty.LastOrDefaultStartingAtIndex(0, _ => true));
        }

        [TestCase(1, ExpectedResult = 12)]
        [TestCase(2, ExpectedResult = 10)]
        [TestCase(-1, ExpectedResult = 10)]
        public int FirstOrDefaultStartingAtIndex_FindsNextWrappingAround(int index) => List.FirstOrDefaultStartingAtIndex(index, IsEven);

        [TestCase(2, ExpectedResult = 11)]
        [TestCase(0, ExpectedResult = 13)]
        [TestCase(99, ExpectedResult = 13)]
        public int LastOrDefaultStartingAtIndex_FindsPreviousWrappingAround(int index) => List.LastOrDefaultStartingAtIndex(index, IsOdd);

        [Test]
        public void StartingAtIndex_SkipsTheStartElement()
        {
            Assert.AreEqual(0, List.FirstOrDefaultStartingAtIndex(1, x => x == 11));
            Assert.AreEqual(0, List.LastOrDefaultStartingAtIndex(1, x => x == 11));
        }

        [Test]
        public void IndexOf_ReturnsIndexOrMinusOne()
        {
            int[] array = { 5, 6, 7 };

            Assert.AreEqual(1, array.IndexOf(6));
            Assert.AreEqual(-1, array.IndexOf(9));
        }

        [Test]
        public void RemoveLast_ReturnsAndRemovesLastElement()
        {
            List<int> list = new() { 1, 2, 3 };

            Assert.AreEqual(3, list.RemoveLast());
            CollectionAssert.AreEqual(new[] { 1, 2 }, list);
        }

        [Test]
        public void Shuffle_KeepsTheSameElements()
        {
            List<int> list = new() { 1, 2, 3, 4, 5, 6 };

            list.Shuffle();

            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6 }, list);
        }
    }
}
