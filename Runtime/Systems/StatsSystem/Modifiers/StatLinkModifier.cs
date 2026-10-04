using System;
using UnityEngine;

namespace RogueLikeEngine.Systems.Stats
{
    [Serializable]
    public class StatLinkModifier : AbstractStatModifier
    {
        public StatDefinition linkedStatDefinition;
        [SerializeField] private float increaseRate;

        private Stat m_linkedStat;
        private StatLink m_statLink;
        
        public override float ValuePreview => increaseRate;
        public override int Priority => -100;

        public override void Configure(StatsStore statsStore, Stat ownerStat)
        {
            base.Configure(statsStore, ownerStat);
            m_linkedStat = StatsStore.GetOrCreateStat(linkedStatDefinition);
            m_statLink = new StatLink(OwnerStat, 0);
            m_linkedStat.AddStatLink(m_statLink);
        }

        public override float PreviewValueFor(StatsStore statsStore)
        {
            if (statsStore == null || !linkedStatDefinition) return 0;
            return GetLinkedValue(statsStore.GetOrCreateStat(linkedStatDefinition)) * increaseRate;
        }

        private float GetLinkedValue(Stat linkedStat) => Permanence switch
        {
            StatPermanence.Permanent => linkedStat.PermanentValue,
            StatPermanence.Temporary => linkedStat.TemporaryValue,
            StatPermanence.FinalValue => linkedStat.FinalValue,
            _ => throw new ArgumentOutOfRangeException()
        };

        public override float Value
        {
            get
            {
                float currentValue = GetLinkedValue(m_linkedStat);
                StatLink prevLink = OwnerStat.GetStatLinkFor(m_linkedStat);
                if (prevLink != null)
                    m_statLink.BaseValueIncreased =  (currentValue - prevLink.BaseValueIncreased) * increaseRate;
                else
                    m_statLink.BaseValueIncreased = currentValue * increaseRate;
                return m_statLink.BaseValueIncreased;
            }
        }

    }
}