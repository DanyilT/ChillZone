using System;
using UnityEngine;

namespace ChillZone.Scene.Settings.Cell
{
    [CreateAssetMenu(fileName = "TextSliderCell", menuName = "ChillZone/Settings/Cell/Text + Slider", order = 25)]
    public class TextSliderCell : SettingsCell
    {
        [SerializeField] private string text = "Label";
        [SerializeField] private float min = 0f;
        [SerializeField] private float max = 1f;
        [SerializeField] private float defaultValue = 0.5f;
        [SerializeField] private bool wholeNumbers;
        [SerializeField] private string actionKey;

        public override void Build(SettingsBuilder.SettingsCellBuilder builder, SettingsActions actions)
        {
            var initial = actions ? actions.GetSlider(actionKey, defaultValue) : defaultValue;
            Action<float> onChanged = actions ? value => actions.SetSlider(actionKey, value) : null;
            builder.CreateTextSliderCell(text, min, max, initial, wholeNumbers, onChanged);
        }
    }
}
