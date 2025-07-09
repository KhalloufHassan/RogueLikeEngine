namespace RogueLikeEngine.Attributes
{
    using UnityEngine;

    public class ShowIfAttribute : PropertyAttribute
    {
        public string ConditionFieldName { get; }
        public object CompareValue { get; }
        public bool Inverse { get; }

        public ShowIfAttribute(string conditionFieldName, object compareValue = null, bool inverse = false)
        {
            ConditionFieldName = conditionFieldName;
            CompareValue = compareValue;
            Inverse = inverse;
        }
    }

}