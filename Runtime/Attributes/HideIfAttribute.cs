namespace RogueLikeEngine.Attributes
{
    using UnityEngine;

    public class HideIfAttribute : PropertyAttribute
    {
        public string ConditionFieldName { get; }
        public object CompareValue { get; }

        public HideIfAttribute(string conditionFieldName, object compareValue = null)
        {
            ConditionFieldName = conditionFieldName;
            CompareValue = compareValue;
        }
    }

}