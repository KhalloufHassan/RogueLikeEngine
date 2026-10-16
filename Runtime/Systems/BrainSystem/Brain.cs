using RogueLikeEngine.Systems.Entities;
using RogueLikeEngine.Utils;
using UnityEngine;

namespace RogueLikeEngine.Systems.Brains
{
    [DisallowMultipleComponent, RequireComponent(typeof(Entity))]
    public class Brain : EntitySystem
    {
        [SerializeField] private BrainGraph m_graph;
        [SerializeReference] private BrainVariable[] m_variableOverrides = System.Array.Empty<BrainVariable>();

        private BrainGraph m_instance;

        public BrainContext Context { get; private set; }

        private void Start()
        {
            if (!m_graph || m_graph.Root == null) return;
            m_instance = Instantiate(m_graph);
            Context = new BrainContext(Entity ? Entity : GetComponent<Entity>(), m_instance.Variables);
            Context.ApplyOverrides(m_variableOverrides);
        }

        private void Update()
        {
            if (!IsSystemActive || !m_instance) return;
            Context.DeltaTime = Time.deltaTime;
            if (m_instance.Root.Tick(Context) != BrainStatus.Running) Restart();
        }

        /// <summary>Ends running actions and returns to the entry point without changing variables.</summary>
        public void Restart() => m_instance?.Root.Reset(Context);

        protected override void OnSystemActiveChanged()
        {
            if (!IsSystemActive) Restart();
        }

        private void OnDisable() => Restart();

        private void OnDestroy()
        {
            Restart();
            if (m_instance) UnityObjectUtility.DestroySafely(m_instance);
        }
    }
}
