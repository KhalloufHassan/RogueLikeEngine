using System.Collections.Generic;
using NUnit.Framework;
using RogueLikeEngine.Systems.Stats;
using RogueLikeEngine.Utils.Formatters;
using RogueLikeEngine.Utils.Randomizers;
using RogueLikeEngine.Utils.Timers;

namespace RogueLikeEngine.Tests
{
    public class UtilsTests
    {
        [Test]
        public void AutoTimer_AfterReset_IsNotFinishedUntilItsDuration()
        {
            AutoTimer timer = 10f;
            timer.Reset();

            Assert.IsFalse(timer.IsFinished);
            Assert.AreEqual(1f, timer.Percentage, 1e-3f);
        }

        [Test]
        public void AutoTimer_ZeroDuration_IsFinishedWithZeroPercentage()
        {
            AutoTimer timer = 0f;
            timer.Reset();

            Assert.IsTrue(timer.IsFinished);
            Assert.AreEqual(0f, timer.Percentage);
        }

        [Test]
        public void FloatTimer_ZeroDuration_PercentageIsNotNaN()
        {
            FloatTimer timer = 0f;

            Assert.AreEqual(0f, timer.Percentage);
        }

        [Test]
        public void RandomAutoTimer_Reset_PicksDurationInRangeAndStartsCounting()
        {
            RandomAutoTimer timer = new() { randomRange = new RandomFloatRange { min = 5, max = 10 } };

            timer.Reset();

            Assert.That(timer.timerDuration, Is.InRange(5f, 10f));
            Assert.IsFalse(timer.IsFinished);
        }

        [Test]
        public void WeightedList_NullOrEmpty_ReturnsDefault()
        {
            Assert.IsNull(new WeightedList<string>().GetRandomElement());
            Assert.IsNull(new WeightedList<string> { Elements = new List<WeightedElement<string>>() }.GetRandomElement());
        }

        [Test]
        public void WeightedList_OnlyNonZeroWeightIsPicked()
        {
            WeightedList<string> list = new()
            {
                Elements = new List<WeightedElement<string>>
                {
                    new() { element = "never", weight = 0 },
                    new() { element = "always", weight = 5 },
                }
            };

            for (int i = 0; i < 50; i++)
                Assert.AreEqual("always", list.GetRandomElement());
        }

        [Test]
        public void ItemsFormatter_SkipsEmptyModifierSlots()
        {
            Assert.AreEqual(string.Empty, new IStatModifier[] { null }.FormatModifiers());
        }
    }
}
