using System.Collections.Generic;
using System.Text;
using RogueLikeEngine.Systems.Stats;
using UnityEngine;

namespace DefaultNamespace.RogueLikeEngine.Utils.Formatters
{
    public static class ItemsFormatter
    {
        public static string FormatModifiers(this IEnumerable<IStatModifier> modifiers,StatsStore simulateStore = null)
        {
            StringBuilder sb = new();

            foreach (IStatModifier modifier in modifiers)
            {
                string formattedText = modifier switch
                {
                    StatLinkModifier linkMod => FormatLinkModifier(linkMod,simulateStore),
                    _ => modifier.Mode == StatModifierMode.RateIncrease 
                        ? FormatPercentageModifier(modifier) 
                        : FormatRegularModifier(modifier)
                };
                
                sb.AppendLine(formattedText);
            }
            return sb.ToString().TrimEnd();
        }

        private static string FormatRegularModifier(IStatModifier modifier)
        {
            string statName = $"<color=yellow>{modifier.TargetStat.statName}</color>";
            string formattedValue = FormatNumericValue(modifier.ValuePreview,modifier.FormatingOptions.ValueSign);
            return $"{formattedValue} {statName}";
        }

        private static string FormatPercentageModifier(IStatModifier modifier)
        {
            string color = GetColorForValue(modifier.ValuePreview,modifier.FormatingOptions.ValueSign);
            float percentValue = modifier.ValuePreview * 100f;
            string changeWord = modifier.ValuePreview >= 0 ? "increased" : "decreased";
            string statName = $"<color=yellow>{modifier.TargetStat.statName}</color>";

            return $"{statName} Modifications are <color={color}>{changeWord} by {Mathf.Abs(percentValue):F0}%</color>";
        }

        private static string FormatLinkModifier(StatLinkModifier modifier, StatsStore simulateStore = null)
        {
            string statName = $"<color=yellow>{modifier.TargetStat.statName}</color>";
            string linkedStatName = modifier.linkedStatDefinition.statName;
            string formattedValue = FormatNumericValue(modifier.ValuePreview,modifier.FormatingOptions.ValueSign);
            float currentValue = simulateStore?.GetOrCreateStat(modifier.TargetStat).PreviewModifierValue(modifier) ?? 0;
            string formattedCurrentValue = FormatNumericValue(currentValue,modifier.FormatingOptions.ValueSign);

            return $"{formattedValue} {statName} for every 1 {linkedStatName} you have [{formattedCurrentValue}]";
        }

        private static string FormatNumericValue(float value, ValueSign valueSign)
        {
            string sign = value >= 0 ? "+" : "";
            string color = GetColorForValue(value, valueSign);
            
            // For neutral sign, we skip color tags entirely
            if (valueSign == ValueSign.Neutral)
            {
                return $"{sign}{value:F0}";
            }
            
            return $"<color={color}>{sign}{value:F0}</color>";
        }

        private static string GetColorForValue(float value, ValueSign valueSign)
        {
            return valueSign switch
            {
                ValueSign.FromValue => value > 0 ? "green" : value < 0 ? "red" : "white",
                ValueSign.Positive => "green",
                ValueSign.Negative => "red",
                _ => "white"
            };
        }


    }
}