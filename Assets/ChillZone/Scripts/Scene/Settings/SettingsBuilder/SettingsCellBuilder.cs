using System;
using System.Collections.Generic;
using ChillZone.Core;
using ChillZone.UI.Utils.Config;
using ChillZone.UI.Window.Config;
using ChillZone.UI.Window.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChillZone.Scene.Settings.SettingsBuilder
{
    /// <summary>
    /// Instantiates settings cell views into the scroll content. Interactive cells use the
    /// assigned view prefabs; section headers and spacers are generated in code. Cells are
    /// flagged <see cref="HideFlags.DontSave"/> so the scene file stays clean — the builder
    /// regenerates them (edit mode included) from the registry.
    /// </summary>
    public class SettingsCellBuilder : MonoBehaviour
    {
        [Header("View Cell Prefabs")]
        [SerializeField] private GameObject textOnlyButtonViewCellPrefab;
        [SerializeField] private GameObject textButtonViewCellPrefab;
        [SerializeField] private GameObject textToggleViewCellPrefab;
        [SerializeField] private GameObject textSliderViewCellPrefab;
        [SerializeField] private GameObject textDropdownViewCellPrefab;
        [SerializeField] private GameObject dualButtonViewCellPrefab;
        [SerializeField] private GameObject textInputFieldViewCellPrefab;

        [Header("References")]
        [SerializeField] private RectTransform contentContainer;

        [Header("Default Heights (-1 = keep prefab height)")]
        [SerializeField] private float defaultCellHeight = 100f;
        [SerializeField] private float sectionHeaderHeight = 40f;
        [SerializeField] private float spacerHeight = 20f;

        private readonly List<GameObject> _createdCells = new();

        #region interactive cells (prefab views, public)

        public GameObject CreateTextCell(string text, bool clickable = false, Action onClick = null)
        {
            var cell = InstantiateView(textOnlyButtonViewCellPrefab, "Text-only");
            if (!cell) return null;

            SetPrimaryText(cell, text);

            var button = cell.GetComponent<Button>();
            if (clickable && onClick != null)
            {
                button ??= cell.AddComponent<Button>();
                button.onClick.AddListener(() => onClick());
                WireClickSound(button);
            }
            else if (button) button.interactable = false;

            return Finish(cell, defaultCellHeight);
        }

        public GameObject CreateTextButtonCell(string text, string buttonText, Action onButtonClick)
        {
            var cell = InstantiateView(textButtonViewCellPrefab, "Text+Button");
            if (!cell) return null;

            var texts = cell.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0) texts[0].text = text;
            if (texts.Length > 1) texts[1].text = buttonText;

            var button = cell.GetComponentInChildren<Button>();
            if (button && onButtonClick != null)
            {
                button.onClick.AddListener(() => onButtonClick());
                WireClickSound(button);
            }

            return Finish(cell, defaultCellHeight);
        }

        public GameObject CreateTextToggleCell(string text, bool initialValue, Action<bool> onToggleChanged)
        {
            var cell = InstantiateView(textToggleViewCellPrefab, "Text+Toggle");
            if (!cell) return null;

            SetPrimaryText(cell, text);

            var toggle = cell.GetComponentInChildren<Toggle>();
            if (toggle)
            {
                toggle.isOn = initialValue;   // set before listeners are added so this doesn't fire the toggle SFX
                if (onToggleChanged != null) toggle.onValueChanged.AddListener(value => onToggleChanged(value));
                toggle.onValueChanged.AddListener(_ => AudioService.PlayUi(UiSound.Toggle));
            }

            return Finish(cell, defaultCellHeight);
        }

        public GameObject CreateTextSliderCell(string text, float min, float max, float initialValue, bool wholeNumbers, Action<float> onSliderChanged)
        {
            var cell = InstantiateView(textSliderViewCellPrefab, "Text+Slider");
            if (!cell) return null;

            SetPrimaryText(cell, text);

            var slider = cell.GetComponentInChildren<Slider>();
            if (slider)
            {
                slider.minValue = min;
                slider.maxValue = max;
                slider.wholeNumbers = wholeNumbers;
                slider.value = initialValue;
                if (onSliderChanged != null) slider.onValueChanged.AddListener(value => onSliderChanged(value));
            }

            return Finish(cell, defaultCellHeight);
        }

        public GameObject CreateTextDropdownCell(string text, string[] options, int initialIndex, Action<int> onDropdownChanged)
        {
            var cell = InstantiateView(textDropdownViewCellPrefab, "Text+Dropdown");
            if (!cell) return null;

            SetPrimaryText(cell, text);

            var dropdown = cell.GetComponentInChildren<TMP_Dropdown>();
            if (dropdown != null)
            {
                dropdown.ClearOptions();
                dropdown.AddOptions(new List<string>(options ?? Array.Empty<string>()));
                dropdown.value = initialIndex;
                if (onDropdownChanged != null) dropdown.onValueChanged.AddListener(value => onDropdownChanged(value));
            }

            return Finish(cell, defaultCellHeight);
        }

        public GameObject CreateDualButtonCell(string title, (string label, Action onClick) a, (string label, Action onClick) b)
        {
            var cell = InstantiateView(dualButtonViewCellPrefab, "Dual-Button");
            if (!cell) return null;

            var buttons = cell.GetComponentsInChildren<Button>();
            if (buttons.Length > 0) BindButton(buttons[0], a);
            if (buttons.Length > 1) BindButton(buttons[1], b);

            // Optional title = the first label that isn't inside either button.
            if (!string.IsNullOrEmpty(title))
            {
                foreach (var label in cell.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (IsInsideAnyButton(label.transform, buttons)) continue;
                    label.GetComponentInParent<RectTransform>().offsetMin = new Vector2(50f, 10f);
                    label.gameObject.SetActive(true);
                    label.text = title;
                    break;
                }
            }

            return Finish(cell, defaultCellHeight);
        }

        public GameObject CreateTextInputFieldCell(string text, string placeholder, string initialValue, Action<string> onInputChanged)
        {
            var cell = InstantiateView(textInputFieldViewCellPrefab, "Text+Input");
            if (!cell) return null;

            SetPrimaryText(cell, text);

            var input = cell.GetComponentInChildren<TMP_InputField>();
            if (input != null)
            {
                if (input.placeholder is TextMeshProUGUI ph) ph.text = placeholder;
                input.text = initialValue;
                if (onInputChanged != null) input.onValueChanged.AddListener(value => onInputChanged(value));
            }

            return Finish(cell, defaultCellHeight);
        }

        public GameObject CreateCodeInputCell(string label, string placeholder, Func<string, bool> onSubmit)
        {
            var cell = InstantiateView(textInputFieldViewCellPrefab, "Code-Input");
            if (!cell) return null;

            var input = cell.GetComponentInChildren<TMP_InputField>();

            // Set the row label = the first TMP that isn't part of the input field.
            foreach (var tmp in cell.GetComponentsInChildren<TextMeshProUGUI>())
            {
                if (input && tmp.transform.IsChildOf(input.transform)) continue;
                tmp.text = label;
                break;
            }

            if (input)
            {
                if (input.placeholder is TextMeshProUGUI ph) ph.text = placeholder;
                input.text = string.Empty;
                if (onSubmit != null)
                    input.onSubmit.AddListener(value => { if (onSubmit(value)) input.text = string.Empty; });
            }

            return Finish(cell, defaultCellHeight);
        }

        public GameObject CreateCustomCell(GameObject customPrefab, float height = -1f)
        {
            var cell = InstantiateView(customPrefab, "Custom");
            if (!cell) return null;

            // Custom prefabs (e.g. the developer-options throw-mode buttons) aren't ButtonManager buttons,
            // so wire the UI click SFX onto whatever buttons they contain.
            foreach (var button in cell.GetComponentsInChildren<Button>(true))
                WireClickSound(button);

            return Finish(cell, height > 0 ? height : defaultCellHeight);
        }

        #endregion

        #region code-generated cells (no prefab, public)

        public GameObject CreateSectionHeader(string headerText, float height = -1f)
        {
            if (!contentContainer) return null;

            var config = TextConfig.TitleTextDefault();
            config.alignment = TextAlignmentOptions.MidlineLeft;
            var go = RenderUtils.CreateText(contentContainer, headerText, config, "SectionHeader");
            return Finish(go, height > 0 ? height : sectionHeaderHeight);
        }

        public GameObject CreateSpacer(float height = -1f)
        {
            if (!contentContainer) return null;
            var go = RenderUtils.CreateChild(contentContainer, "Spacer", typeof(RectTransform));
            return Finish(go, height > 0 ? height : spacerHeight);
        }

        public GameObject CreateInfoText(string text, float height = -1f, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
        {
            if (!contentContainer) return null;

            var config = TextConfig.BodyTextDefault(); // muted, wraps
            config.alignment = alignment;
            var go = RenderUtils.CreateText(contentContainer, text, config, "Info");
            return Finish(go, height > 0 ? height : sectionHeaderHeight);
        }

        #endregion

        #region lifecycle / utility (public)

        public void ClearAllCells()
        {
            _createdCells.Clear();
            if (!contentContainer) return;

            for (int i = contentContainer.childCount - 1; i >= 0; i--)
            {
                var child = contentContainer.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        public int CellCount => _createdCells.Count;

        #endregion

        #region helpers (private)

        private GameObject InstantiateView(GameObject prefab, string label)
        {
            if (prefab) return Instantiate(prefab, contentContainer);
            Debug.LogError($"[SettingsCellBuilder] {label} view prefab not assigned.", this);
            return null;
        }

        private GameObject Finish(GameObject cell, float height)
        {
            SetLayoutHeight(cell, height);
            cell.hideFlags = HideFlags.DontSave; // regenerated from the registry; never serialised into the scene
            _createdCells.Add(cell);
            return cell;
        }

        private static void SetPrimaryText(GameObject cell, string text)
        {
            var label = cell.GetComponentInChildren<TextMeshProUGUI>();
            if (label) label.text = text;
        }

        private static void BindButton(Button button, (string label, Action onClick) entry)
        {
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label) label.text = entry.label;
            if (entry.onClick != null)
            {
                button.onClick.AddListener(() => entry.onClick());
                WireClickSound(button);
            }
        }

        /// <summary>Adds the shared UI click SFX to a button (cells are built outside ButtonManager, so they wire it themselves).</summary>
        private static void WireClickSound(Button button)
        {
            if (button) button.onClick.AddListener(AudioService.PlayButtonClick);
        }

        private static bool IsInsideAnyButton(Transform t, Button[] buttons)
        {
            foreach (var button in buttons)
                if (button && t.IsChildOf(button.transform)) return true;
            return false;
        }

        private static void SetLayoutHeight(GameObject cell, float height)
        {
            var layout = cell.GetComponent<LayoutElement>() ?? cell.AddComponent<LayoutElement>();
            layout.minHeight = height > 0 ? height : layout.minHeight;
            layout.preferredHeight = height > 0 ? height : layout.preferredHeight;
        }

        #endregion
    }
}
