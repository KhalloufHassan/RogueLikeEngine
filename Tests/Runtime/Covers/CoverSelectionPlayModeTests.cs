using System.Collections.Generic;
using NUnit.Framework;
using RogueLikeEngine.Systems.Brains;
using RogueLikeEngine.Systems.Brains.Nodes;
using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Systems.Movements;
using UnityEngine;
using UnityEngine.AI;

namespace RogueLikeEngine.Tests.PlayMode
{
    public class CoverSelectionPlayModeTests
    {
        private const int CoverLayer = 31;
        private const string DestinationVariable = "Destination";
        private static readonly Vector3 Origin = new(2000, 0, 2000);

        private readonly List<GameObject> m_objects = new();
        private NavMeshData m_data;
        private NavMeshDataInstance m_instance;
        private FindCoverPosition m_findCover;
        private BrainContext m_context;

        [SetUp]
        public void SetUp()
        {
            Entity owner = CreateCharacter(Origin);
            Entity threat = CreateCharacter(Origin + Vector3.forward * 5);
            GameObject wall = CreateObject("Cover", Origin + new Vector3(0, .6f, 1.1f));
            wall.layer = CoverLayer;
            BoxCollider box = wall.AddComponent<BoxCollider>();
            box.size = new Vector3(8, 1.2f, .3f);
            wall.AddComponent<Cover>();

            NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(0);
            List<NavMeshBuildSource> sources = new()
            {
                new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    transform = Matrix4x4.TRS(Origin + Vector3.down * .1f, Quaternion.identity, Vector3.one),
                    size = new Vector3(20, .2f, 20)
                },
                new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    transform = wall.transform.localToWorldMatrix,
                    size = box.size
                }
            };
            m_data = NavMeshBuilder.BuildNavMeshData(settings, sources, new Bounds(Origin, new Vector3(24, 8, 24)), Vector3.zero, Quaternion.identity);
            Assert.IsNotNull(m_data);
            m_instance = NavMesh.AddNavMeshData(m_data);
            m_findCover = new FindCoverPosition
            {
                Target = new BrainValue<Entity> { Constant = threat },
                DestinationVariable = DestinationVariable,
                Layers = 1 << CoverLayer,
                AgentTypeID = settings.agentTypeID
            };
            m_context = new BrainContext(owner, new BrainVariable[] { new BrainVector3 { Name = DestinationVariable } });
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            m_instance.Remove();
            if (m_data) Object.Destroy(m_data);
            foreach (GameObject gameObject in m_objects)
            {
                gameObject.SetActive(false);
                Object.Destroy(gameObject);
            }
            m_objects.Clear();
        }

        [Test]
        public void ChoosesAnotherPositionWhenCoverIsOccupiedAndReusesItAfterDisable()
        {
            Assert.AreEqual(BrainStatus.Success, m_findCover.Tick(m_context));
            Vector3 preferred = m_context.Get<Vector3>(DestinationVariable);
            Entity occupant = CreateCharacter(preferred + Vector3.forward * .3f);
            CapsuleCollider capsule = occupant.GetComponent<CapsuleCollider>();
            capsule.height = 1;
            capsule.center = Vector3.up * .5f;
            Physics.SyncTransforms();
            m_findCover.Reset(m_context);
            Assert.AreEqual(BrainStatus.Success, m_findCover.Tick(m_context));
            Assert.Greater(Vector3.Distance(preferred, m_context.Get<Vector3>(DestinationVariable)), .9f);

            occupant.gameObject.SetActive(false);
            Physics.SyncTransforms();
            m_findCover.Reset(m_context);
            Assert.AreEqual(BrainStatus.Success, m_findCover.Tick(m_context));
            Assert.That(Vector3.Distance(preferred, m_context.Get<Vector3>(DestinationVariable)), Is.LessThan(.01f));
        }

        [Test]
        public void FailsWhenEveryCoverPositionIsOccupied()
        {
            for (int i = 0; i < 3; i++)
            {
                m_findCover.Reset(m_context);
                Assert.AreEqual(BrainStatus.Success, m_findCover.Tick(m_context));
                CreateCharacter(m_context.Get<Vector3>(DestinationVariable) + Vector3.forward * .3f);
                Physics.SyncTransforms();
            }
            m_findCover.Reset(m_context);
            Assert.AreEqual(BrainStatus.Failure, m_findCover.Tick(m_context));
        }

        [Test]
        public void IgnoresTheSearchingCharacterAndTriggerVolumes()
        {
            Assert.AreEqual(BrainStatus.Success, m_findCover.Tick(m_context));
            Vector3 preferred = m_context.Get<Vector3>(DestinationVariable);
            Entity trigger = CreateCharacter(preferred);
            trigger.GetComponent<CapsuleCollider>().isTrigger = true;
            Physics.SyncTransforms();
            m_findCover.Reset(m_context);
            Assert.AreEqual(BrainStatus.Success, m_findCover.Tick(m_context));
            Assert.That(Vector3.Distance(preferred, m_context.Get<Vector3>(DestinationVariable)), Is.LessThan(.01f));
        }

        private Entity CreateCharacter(Vector3 position)
        {
            GameObject character = CreateObject("Character", position);
            CapsuleCollider capsule = character.AddComponent<CapsuleCollider>();
            capsule.height = 1.8f;
            capsule.radius = .3f;
            capsule.center = Vector3.up * .9f;
            return character.AddComponent<Entity>();
        }

        private GameObject CreateObject(string name, Vector3 position)
        {
            GameObject gameObject = new(name);
            gameObject.transform.position = position;
            m_objects.Add(gameObject);
            return gameObject;
        }
    }
}
