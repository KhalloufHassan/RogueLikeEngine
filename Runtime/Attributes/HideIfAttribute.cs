namespace RogueLikeEngine.Attributes
{
    public class HideIfAttribute : ShowIfAttribute
    {
        public HideIfAttribute(string conditionFieldName, object compareValue = null)
            : base(conditionFieldName, compareValue,true)
        {
        }
    }

}