using System.Collections.Generic;
using NUnit.Framework;
using RogueLikeEngine.Optimization.Pooling;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Entities.Effects;
using RogueLikeEngine.Systems.Weapons;
using RogueLikeEngine.Utils.Timers;

namespace RogueLikeEngine.Tests
{
    public class EntityEffectsTests
    {
        private TestObjects m_objects;
        private Entity m_entity;
        private readonly List<IEffect> m_addedCopies = new();

        [SetUp]
        public void SetUp()
        {
            m_objects = new TestObjects();
            m_addedCopies.Clear();
            m_entity = m_objects.CreateComponent<Entity>();
            m_entity.OnEffectAdded += effect =>
            {
                m_addedCopies.Add(effect);
                if (effect is TestEffect so) m_objects.Track(so);
            };
        }

        [TearDown]
        public void TearDown() => m_objects.Dispose();

        private TestEffect Apply(TestEffect template)
        {
            m_entity.AddEffect(template);
            return (TestEffect)m_addedCopies[^1];
        }

        [Test]
        public void AddEffect_StoresACopy_NotTheTemplate()
        {
            TestEffect template = TestEffect.Create(m_objects, "Burn");

            TestEffect copy = Apply(template);

            Assert.AreNotSame(template, copy);
            Assert.AreEqual(template.ID, copy.ID);
        }

        [Test]
        public void AddEffect_Twice_StacksOnTheSameCopy()
        {
            TestEffect template = TestEffect.Create(m_objects, "Burn");

            TestEffect first = Apply(template);
            TestEffect second = Apply(template);

            Assert.AreSame(first, second);
        }

        [Test]
        public void DifferentTemplates_WithSameName_AreSeparateEffects()
        {
            TestEffect first = Apply(TestEffect.Create(m_objects, "Burn"));
            TestEffect second = Apply(TestEffect.Create(m_objects, "Burn"));

            Assert.AreNotSame(first, second);
            Assert.AreNotEqual(first.ID, second.ID);
        }

        [Test]
        public void DifferentTemplates_WithEmptyNames_AreSeparateEffects()
        {
            TestEffect first = Apply(TestEffect.Create(m_objects, ""));
            TestEffect second = Apply(TestEffect.Create(m_objects, null));

            Assert.AreNotSame(first, second);
        }

        [Test]
        public void CopyOfACopy_KeepsTheTemplateId()
        {
            TestEffect template = TestEffect.Create(m_objects, "Burn");
            IEffect copy = template.GetCopy();
            IEffect copyOfCopy = copy.GetCopy();
            m_objects.Track((TestEffect)copy);
            m_objects.Track((TestEffect)copyOfCopy);

            Assert.AreEqual(template.ID, copy.ID);
            Assert.AreEqual(template.ID, copyOfCopy.ID);
        }

        [Test]
        public void RemoveEffect_DestroysTheCopy_NotTheTemplate()
        {
            TestEffect template = TestEffect.Create(m_objects, "Burn");
            TestEffect copy = Apply(template);

            m_entity.RemoveEffect(template, true);

            Assert.IsFalse(copy, "The removed copy should have been destroyed");
            Assert.IsTrue(template, "The template must never be destroyed");
        }

        [Test]
        public void ExpiredEffect_DestroysTheCopy()
        {
            TestEffect copy = Apply(TestEffect.Create(m_objects, "Burn", durationSeconds: 0));

            TestReflection.Invoke(m_entity, "Update");

            Assert.IsFalse(copy);
        }

        [Test]
        public void DestroyingTheEntity_ReleasesCopiesWithoutEndingThem()
        {
            TestEffect copy = Apply(TestEffect.Create(m_objects, "Burn"));

            // Unity doesn't send OnDestroy in edit mode, so it's called directly like Update
            TestReflection.Invoke(m_entity, "OnDestroy");

            Assert.IsFalse(copy, "The copy should be destroyed with the entity");
            Assert.AreEqual(0, copy.durationEndedCount, "Dying must not fire OnDurationEnded");
        }

        [Test]
        public void DamageOwner_DefaultsToTheAffectedEntity()
        {
            TestEffect copy = Apply(TestEffect.Create(m_objects, "Burn"));

            Assert.AreSame(m_entity, copy.DamageOwner);
        }

        [Test]
        public void DamageOwner_IsTheEntityThatAppliedTheEffect()
        {
            Entity attacker = m_objects.CreateComponent<Entity>("Attacker");

            m_entity.AddEffect(TestEffect.Create(m_objects, "Burn"), attacker);

            Assert.AreSame(attacker, m_addedCopies[^1].DamageOwner);
        }

        [Test]
        public void DamageOwner_StackingKeepsTheFirstOwner()
        {
            Entity first = m_objects.CreateComponent<Entity>("First");
            Entity second = m_objects.CreateComponent<Entity>("Second");
            TestEffect template = TestEffect.Create(m_objects, "Burn");

            m_entity.AddEffect(template, first);
            m_entity.AddEffect(template, second);

            Assert.AreSame(first, m_addedCopies[^1].DamageOwner);
        }

        [Test]
        public void RemoveEffect_NotApplied_DoesNotThrow()
        {
            TestEffect template = TestEffect.Create(m_objects, "Burn");

            Assert.DoesNotThrow(() => m_entity.RemoveEffect(template, true));
        }

        [Test]
        public void RemoveEffect_NotifiesTheCopy_NotTheTemplate()
        {
            TestEffect template = TestEffect.Create(m_objects, "Burn");
            TestEffect copy = Apply(template);

            m_entity.RemoveEffect(template, true);

            Assert.AreEqual(1, copy.durationEndedCount);
            Assert.AreEqual(0, template.durationEndedCount);
        }

        [Test]
        public void RemoveEffect_WithoutTrigger_DoesNotNotify()
        {
            TestEffect copy = Apply(TestEffect.Create(m_objects, "Burn"));

            m_entity.RemoveEffect(copy, false);

            Assert.AreEqual(0, copy.durationEndedCount);
        }

        [Test]
        public void RemoveEffect_ThenAddAgain_CreatesAFreshCopy()
        {
            TestEffect template = TestEffect.Create(m_objects, "Burn");
            TestEffect first = Apply(template);

            m_entity.RemoveEffect(template, false);
            TestEffect second = Apply(template);

            Assert.AreNotSame(first, second);
        }

        [Test]
        public void Update_CallsOnUpdate()
        {
            TestEffect copy = Apply(TestEffect.Create(m_objects, "Burn"));

            TestReflection.Invoke(m_entity, "Update");
            TestReflection.Invoke(m_entity, "Update");

            Assert.AreEqual(2, copy.updateCount);
        }

        [Test]
        public void Update_ExpiredEffect_IsNotifiedOnceAndRemoved()
        {
            TestEffect template = TestEffect.Create(m_objects, "Burn", durationSeconds: 0);
            TestEffect copy = Apply(template);

            TestReflection.Invoke(m_entity, "Update");
            TestReflection.Invoke(m_entity, "Update");

            Assert.AreEqual(1, copy.durationEndedCount);
            Assert.AreEqual(1, copy.updateCount, "A removed effect must not be updated again");
            Assert.AreNotSame(copy, Apply(template), "The expired effect should have been removed");
        }

        [Test]
        public void Update_EffectWithoutUpdateHook_StillExpires()
        {
            EffectScriptableObject template = m_objects.CreateAsset<EffectScriptableObject>();
            TestReflection.SetField(template, "effectName", "Plain");
            TestReflection.SetField(template, "duration", (AutoTimer)0f);
            m_entity.AddEffect(template);
            EffectScriptableObject copy = m_objects.Track((EffectScriptableObject)m_addedCopies[^1]);

            TestReflection.Invoke(m_entity, "Update");

            Assert.IsFalse(copy, "The expired effect's copy should have been removed and destroyed");
        }

        [Test]
        public void Update_EffectRemovingAnotherEffect_DoesNotThrow()
        {
            TestEffect victim = TestEffect.Create(m_objects, "Victim");
            TestEffect remover = TestEffect.Create(m_objects, "Remover");
            remover.removeOnUpdate = victim;
            TestEffect victimCopy = Apply(victim);
            Apply(remover);

            Assert.DoesNotThrow(() => TestReflection.Invoke(m_entity, "Update"));
            Assert.AreNotSame(victimCopy, Apply(victim), "The victim should have been removed");
        }

        [Test]
        public void Update_EffectAddingAnotherEffect_DoesNotThrow()
        {
            TestEffect added = TestEffect.Create(m_objects, "Added");
            TestEffect adder = TestEffect.Create(m_objects, "Adder");
            adder.addOnUpdate = added;
            Apply(adder);

            Assert.DoesNotThrow(() => TestReflection.Invoke(m_entity, "Update"));
            Assert.IsTrue(m_addedCopies.Exists(e => e.ID == added.ID));
        }

        [Test]
        public void ClearEffects_RemovesAndNotifiesEveryEffect()
        {
            TestEffect burn = Apply(TestEffect.Create(m_objects, "Burn"));
            TestEffect poison = Apply(TestEffect.Create(m_objects, "Poison"));

            m_entity.ClearEffects(true);
            TestReflection.Invoke(m_entity, "Update");

            Assert.AreEqual(1, burn.durationEndedCount);
            Assert.AreEqual(1, poison.durationEndedCount);
            Assert.AreEqual(0, burn.updateCount + poison.updateCount);
        }

        [Test]
        public void Projectile_WithPool_Despawn_ReturnsToPool()
        {
            Projectile projectile = m_objects.CreateComponent<Projectile>();
            FakePool pool = new();
            projectile.ParentPool = pool;

            // In edit mode a Destroy call would log an error and fail this test
            projectile.Despawn();

            Assert.AreEqual(1, pool.Returned.Count);
            Assert.AreSame(projectile, pool.Returned[0]);
            Assert.IsTrue(projectile);
        }

        [Test]
        public void Projectile_OnDisposed_ClearsItsEntityEffectsWeaponAndOwner()
        {
            Projectile projectile = m_objects.CreateComponent<Projectile>();
            Entity projectileEntity = projectile.gameObject.AddComponent<Entity>();
            TestReflection.Invoke(projectile, "Awake");
            List<IEffect> copies = new();
            projectileEntity.OnEffectAdded += copies.Add;
            projectileEntity.AddEffect(TestEffect.Create(m_objects, "Burn"));
            TestEffect copy = m_objects.Track((TestEffect)copies[0]);
            projectile.Weapon = new WeaponInstance(m_objects.CreateAsset<WeaponData>());
            projectile.Owner = m_entity;

            projectile.OnDisposed();

            Assert.AreEqual(1, copy.durationEndedCount);
            Assert.IsNull(projectile.Weapon);
            Assert.IsNull(projectile.Owner);
        }

        [Test]
        public void Projectile_WithoutEntity_HasNoEffectsHost()
        {
            Projectile projectile = m_objects.CreateComponent<Projectile>();
            TestReflection.Invoke(projectile, "Awake");

            Assert.IsNull(projectile.Entity);
            Assert.DoesNotThrow(() => projectile.OnDisposed());
        }

        private class FakePool : IPool
        {
            public readonly List<IPoolObject> Returned = new();
            public void ReturnToPool(IPoolObject obj) => Returned.Add(obj);
        }
    }
}
