using System.Collections;
using System.Reflection;
using NUnit.Framework;
using RogueLikeEngine.Audio;
using RogueLikeEngine.Optimization.Pooling;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
using Object = UnityEngine.Object;

namespace RogueLikeEngine.Tests.PlayMode
{
    public class SoundManagerPlayModeTests
    {
        private const string PoolRootName = "Pool_of_" + nameof(SoundEmitter);

        private SoundManager m_manager;
        private SoundEmitterPool m_pool;
        private GameObject m_template;

        [SetUp]
        public void SetUp()
        {
            m_template = new GameObject("SoundEmitterTemplate");
            m_template.SetActive(false);
            AudioSource source = m_template.AddComponent<AudioSource>();
            SetField(m_template.AddComponent<SoundEmitter>(), "m_audioSource", source);
            m_pool = ScriptableObject.CreateInstance<SoundEmitterPool>();
            m_pool.prefab = m_template.GetComponent<SoundEmitter>();
            m_manager = ScriptableObject.CreateInstance<SoundManager>();
            SetField(m_manager, "m_pool", m_pool);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1;
            Object.Destroy(GameObject.Find(PoolRootName));
            Object.Destroy(m_manager);
            Object.Destroy(m_pool);
            Object.Destroy(m_template);
        }

        private static AudioClip CreateClip(float length, string name = "TestClip") =>
            AudioClip.Create(name, Mathf.CeilToInt(length * 44100), 1, 44100, false);

        private static SoundClips CreateClips(AudioClip clip, float pitch = 1)
        {
            SoundClips clips = new();
            if (!clip) return clips;
            SetField(clips, "m_clips", new[] { clip });
            SetField(clips, "m_minPitch", pitch);
            SetField(clips, "m_maxPitch", pitch);
            return clips;
        }

        private static Sound CreateSound(float clipLength, float pitch = 1, bool loop = false, float minInterval = 0, int maxInstances = 0)
        {
            Sound sound = new();
            SetField(sound, "m_clips", CreateClips(CreateClip(clipLength), pitch));
            SetField(sound, "m_loop", loop);
            SetField(sound, "m_minInterval", minInterval);
            SetField(sound, "m_maxInstances", maxInstances);
            return sound;
        }

        private static SoundSequence CreateSequence(AudioClip start, AudioClip loop, AudioClip end)
        {
            SoundSequence sequence = new();
            SetField(sequence, "m_start", CreateClips(start));
            SetField(sequence, "m_loop", CreateClips(loop));
            SetField(sequence, "m_end", CreateClips(end));
            SetField(sequence, "m_minInterval", 0f);
            return sequence;
        }

        private static AudioSource[] Sources => GameObject.Find(PoolRootName).GetComponentsInChildren<AudioSource>();

        private static bool IsPlaying(AudioClip clip) => System.Array.Exists(Sources, source => source.clip == clip && source.isPlaying);

        private static void SetField(object target, string name, object value)
        {
            for (System.Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field == null) continue;
                field.SetValue(target, value);
                return;
            }

            Assert.Fail($"No field {name} on {target.GetType().Name}");
        }

        [Test]
        public void MinInterval_IgnoresALastPlayLeftFromAnEarlierPlaySession()
        {
            Sound sound = CreateSound(1, minInterval: 10);
            typeof(SoundBase<SoundPlayback>).GetField("m_lastPlayTime", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(sound, Time.realtimeSinceStartupAsDouble + 1000);

            Assert.IsTrue(m_manager.Play(sound).IsActive);
            Assert.IsFalse(m_manager.Play(sound).IsActive, "the interval still applies within the session");
        }

        [UnityTest]
        public IEnumerator StaleHandle_DoesNotAffectSoundReusingItsEmitter()
        {
            Sound sound = CreateSound(1);
            SoundHandle first = m_manager.Play(sound);
            first.Stop();
            Assert.IsFalse(first.IsActive, "a stopped handle goes inactive immediately, during the fade");
            yield return new WaitForSecondsRealtime(0.1f);

            SoundHandle second = m_manager.Play(sound);
            Assert.AreEqual(1, GameObject.Find(PoolRootName).transform.childCount, "the emitter should be reused");
            first.SetPitch(3);
            first.Stop();
            yield return null;
            Assert.IsTrue(second.IsActive);
            Assert.AreEqual(1, GameObject.Find(PoolRootName).GetComponentInChildren<AudioSource>().pitch, 0.001f);
        }

        [UnityTest]
        public IEnumerator Stop_FadesOutBeforeFreeingTheEmitter()
        {
            SoundHandle handle = m_manager.Play(CreateSound(1));
            AudioSource source = GameObject.Find(PoolRootName).GetComponentInChildren<AudioSource>();
            yield return null;
            handle.Stop();
            Assert.IsTrue(source.isPlaying);
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.IsFalse(source.gameObject.activeSelf);
            Assert.AreEqual(0, source.volume, 0.001f);
        }

        [UnityTest]
        public IEnumerator PitchChangedWhilePlaying_UpdatesLifetime()
        {
            SoundHandle handle = m_manager.Play(CreateSound(0.4f));
            yield return new WaitForSeconds(0.1f);
            handle.SetPitch(4);
            yield return new WaitForSeconds(0.15f);
            Assert.IsFalse(handle.IsActive, "the remaining 0.3 clip seconds take 0.075 seconds at pitch 4");
        }

        [UnityTest]
        public IEnumerator SetVolume_MultipliesSoundVolume()
        {
            SoundHandle handle = m_manager.Play(CreateSound(1));
            handle.SetVolume(0.25f);
            yield return null;
            Assert.AreEqual(0.25f, GameObject.Find(PoolRootName).GetComponentInChildren<AudioSource>().volume, 0.001f);
        }

        [UnityTest]
        public IEnumerator Duration_StopsLoopingSound()
        {
            Sound sound = CreateSound(0.1f, loop: true);
            SetField(sound, "m_duration", 0.2f);
            SoundHandle handle = m_manager.Play(sound);
            yield return new WaitForSeconds(0.1f);
            Assert.IsTrue(handle.IsActive);
            yield return new WaitForSeconds(0.2f);
            Assert.IsFalse(handle.IsActive);
        }

        [UnityTest]
        public IEnumerator Sound_EndsAfterClipLengthDividedByPitch()
        {
            SoundHandle handle = m_manager.Play(CreateSound(0.3f, pitch: 2));
            Assert.IsTrue(handle.IsActive);
            yield return new WaitForSeconds(0.25f);
            Assert.IsFalse(handle.IsActive);
        }

        [UnityTest]
        public IEnumerator Sound_LifetimeFollowsTimeScale()
        {
            Time.timeScale = 0.5f;
            SoundHandle handle = m_manager.Play(CreateSound(0.2f));
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsTrue(handle.IsActive);
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.IsFalse(handle.IsActive);
        }

        [UnityTest]
        public IEnumerator TimeScaleChangedWhilePlaying_UpdatesPitchAndLifetime()
        {
            SoundHandle handle = m_manager.Play(CreateSound(0.4f, pitch: 2));
            AudioSource source = GameObject.Find(PoolRootName).GetComponentInChildren<AudioSource>();
            yield return new WaitForSecondsRealtime(0.1f);
            Time.timeScale = 0.25f;
            yield return null;
            Assert.AreEqual(0.5f, source.pitch, 0.001f);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsTrue(handle.IsActive, "the remaining 0.1 game seconds take 0.4 real seconds at a quarter time scale");
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.IsFalse(handle.IsActive);
        }

        [UnityTest]
        public IEnumerator LoopingSound_PlaysUntilStopped()
        {
            SoundHandle handle = m_manager.Play(CreateSound(0.1f, loop: true));
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(handle.IsActive);
            handle.Stop();
            Assert.IsFalse(handle.IsActive);
        }

        [UnityTest]
        public IEnumerator ReplayingSound_DoesNotAllocate()
        {
            Sound sound = CreateSound(1);
            m_manager.Play(sound).Stop();
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(() => m_manager.Play(sound).Stop(), Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void SimultaneousPlays_OfOneSound_AreIndependent()
        {
            Sound sound = CreateSound(1);
            SoundHandle first = m_manager.Play(sound);
            SoundHandle second = m_manager.Play(sound);
            first.Stop();
            Assert.IsTrue(second.IsActive);
        }

        [UnityTest]
        public IEnumerator Delay_StartsSoundLater_InGameTime()
        {
            Sound sound = CreateSound(1);
            SetField(sound, "m_delay", 0.2f);
            SoundHandle handle = m_manager.Play(sound);
            AudioSource source = Sources[0];
            yield return new WaitForSeconds(0.1f);
            Assert.IsTrue(handle.IsActive);
            Assert.IsFalse(source.isPlaying);
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(source.isPlaying);
        }

        [Test]
        public void MaxInstances_StopsOldest()
        {
            Sound sound = CreateSound(1, maxInstances: 2);
            SoundHandle first = m_manager.Play(sound);
            SoundHandle second = m_manager.Play(sound);
            SoundHandle third = m_manager.Play(sound);
            Assert.IsFalse(first.IsActive);
            Assert.IsTrue(second.IsActive);
            Assert.IsTrue(third.IsActive);
        }

        [UnityTest]
        public IEnumerator MaxInstances_FreesSlotWhenSoundEnds()
        {
            Sound sound = CreateSound(0.1f, maxInstances: 1);
            m_manager.Play(sound);
            yield return new WaitForSeconds(0.2f);
            SoundHandle second = m_manager.Play(sound);
            SoundHandle third = m_manager.Play(sound);
            Assert.IsFalse(second.IsActive);
            Assert.IsTrue(third.IsActive);
        }

        [UnityTest]
        public IEnumerator Release_StopsLoopingSound_AndLetsOneShotFinish()
        {
            SoundHandle loop = m_manager.Play(CreateSound(0.1f, loop: true));
            SoundHandle oneShot = m_manager.Play(CreateSound(0.3f));
            yield return null;
            loop.Release();
            oneShot.Release();
            Assert.IsFalse(loop.IsActive);
            Assert.IsTrue(oneShot.IsActive);
            yield return new WaitForSeconds(0.4f);
            Assert.IsFalse(oneShot.IsActive);
        }

        [UnityTest]
        public IEnumerator Sequence_FlowsFromStartIntoLoop_UntilReleased()
        {
            AudioClip start = CreateClip(0.15f, "Start"), loop = CreateClip(0.1f, "Loop"), end = CreateClip(0.1f, "End");
            SoundHandle handle = m_manager.Play(CreateSequence(start, loop, end));
            yield return null;
            Assert.IsTrue(IsPlaying(start));
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(handle.IsActive);
            Assert.IsTrue(IsPlaying(loop));
            Assert.IsFalse(IsPlaying(start));

            handle.Release();
            yield return new WaitForSecondsRealtime(0.05f);
            Assert.IsTrue(IsPlaying(end));
            Assert.IsFalse(IsPlaying(loop), "the loop fades out when released");
            yield return new WaitForSeconds(0.15f);
            Assert.IsFalse(handle.IsActive);
        }

        [UnityTest]
        public IEnumerator Sequence_ReleasedDuringStart_SkipsToEnd()
        {
            AudioClip start = CreateClip(1, "Start"), loop = CreateClip(0.1f, "Loop"), end = CreateClip(0.1f, "End");
            SoundHandle handle = m_manager.Play(CreateSequence(start, loop, end));
            yield return null;
            handle.Release();
            yield return new WaitForSecondsRealtime(0.05f);
            Assert.IsTrue(IsPlaying(end));
            Assert.IsFalse(IsPlaying(start));
            Assert.IsFalse(IsPlaying(loop), "the scheduled loop is cancelled");
            yield return new WaitForSeconds(0.15f);
            Assert.IsFalse(handle.IsActive);
        }

        [UnityTest]
        public IEnumerator Sequence_WithoutLoop_PlaysStartThenEnd()
        {
            AudioClip start = CreateClip(0.1f, "Start"), end = CreateClip(0.1f, "End");
            SoundHandle handle = m_manager.Play(CreateSequence(start, null, end));
            yield return new WaitForSeconds(0.15f);
            Assert.IsTrue(IsPlaying(end));
            yield return new WaitForSeconds(0.15f);
            Assert.IsFalse(handle.IsActive);
        }

        [UnityTest]
        public IEnumerator Sequence_WithoutEnd_FinishesAfterReleaseFade()
        {
            SoundHandle handle = m_manager.Play(CreateSequence(null, CreateClip(0.1f, "Loop"), null));
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(handle.IsActive);
            handle.Release();
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.AreEqual(0, GameObject.Find(PoolRootName).GetComponentsInChildren<SoundEmitter>().Length);
        }

        [UnityTest]
        public IEnumerator ReplayingSequence_DoesNotAllocate()
        {
            SoundSequence sequence = CreateSequence(CreateClip(0.2f, "Start"), CreateClip(0.1f, "Loop"), CreateClip(0.1f, "End"));
            m_manager.Play(sequence).Stop();
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(() => m_manager.Play(sequence).Stop(), Is.Not.AllocatingGCMemory());
        }

        [UnityTest]
        public IEnumerator ChargeSound_PitchAndVolumeFollowIntensity()
        {
            ChargeSound sound = new();
            SetField(sound, "m_clips", CreateClips(CreateClip(0.1f)));
            SetField(sound, "m_pitchOverCharge", AnimationCurve.Linear(0, 1, 1, 2));
            SetField(sound, "m_volumeOverCharge", AnimationCurve.Linear(0, 0.5f, 1, 1));
            SetField(sound, "m_minInterval", 0f);
            SoundHandle handle = m_manager.Play(sound);
            AudioSource source = Sources[0];
            Assert.AreEqual(1, source.pitch, 0.001f);
            Assert.AreEqual(0.5f, source.volume, 0.001f);

            handle.SetIntensity(0.5f);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(1.5f, source.pitch, 0.001f);
            Assert.AreEqual(0.75f, source.volume, 0.001f);
            Assert.IsTrue(handle.IsActive, "a charge sound loops while held");

            handle.Release();
            Assert.IsFalse(handle.IsActive);
        }

        [Test]
        public void ThrottledSound_ReturnsInactiveHandle()
        {
            Sound sound = CreateSound(1, minInterval: 10);
            Assert.IsTrue(m_manager.Play(sound).IsActive);
            Assert.IsFalse(m_manager.Play(sound).IsActive);
        }
    }
}
