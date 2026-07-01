using System;
using ChillZone.UI.Utils.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChillZone.UI.Window.Utils
{
    public static class RenderUtils
    {
        /// <summary>
        /// Create a child GameObject with specified components and set its parent to the given transform.
        /// </summary>
        /// <param name="parent">The transform to set as the parent of the new GameObject.</param>
        /// <param name="name">The name of the new GameObject.</param>
        /// <param name="components">The types of components to add to the new GameObject.</param>
        /// <returns>The created GameObject with the specified components, parented to the given transform.</returns>
        public static GameObject CreateChild(Transform parent, string name, params Type[] components)
        {
            var go = new GameObject(name, components);
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>
        /// Create a TextMeshProUGUI GameObject with specified text and configuration, parented to the given transform.
        /// </summary>
        /// <param name="parent">The transform to set as the parent of the new Text GameObject.</param>
        /// <param name="text">The text content to set on the TextMeshProUGUI component.</param>
        /// <param name="config">The TextConfig object containing configuration settings for the TextMeshProUGUI component.</param>
        /// <param name="name">The name of the new Text GameObject. The default is "Text".</param>
        /// <returns>The created GameObject with a TextMeshProUGUI component configured according to the provided TextConfig, parented to the given transform.</returns>
        public static GameObject CreateText(Transform parent, string text, TextConfig config, string name = "Text")
        {
            var go = CreateChild(parent, name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var txt = go.GetComponent<TextMeshProUGUI>();
            txt.text = text;
            txt.fontSize = config.fontSize;
            txt.enableAutoSizing = config.enableAutoSizing;
            txt.fontSizeMin = config.fontSizeMin;
            txt.fontSizeMax = config.fontSizeMax;
            txt.fontStyle = config.fontStyle;
            txt.color = config.color != Color.clear ? config.color : Color.white;
            txt.alignment = config.alignment;
            txt.raycastTarget = false;
            txt.enableWordWrapping = true;
            txt.overflowMode = TextOverflowModes.Overflow;

            return go;
        }

        /// <summary>
        /// Set up an Image component with specified color, sprite, type, raycast target, and aspect preservation settings. This method configures the Image component according to the provided parameters and returns the modified Image for further use or chaining.
        /// </summary>
        /// <param name="parent">The transform to set as the parent of the new Image GameObject.</param>
        /// <param name="color">The color to set on the Image component, which will determine the tint of the image. If a sprite is provided, this color will be multiplied with the sprite's colors; if no sprite is provided, this color will be used as a solid color for the Image.</param>
        /// <param name="sprite">The optional Sprite to set on the Image component, which will be displayed according to the specified spriteType. If null, the Image will use a solid color based on the provided color parameter.</param>
        /// <param name="spriteType">The Image.Type to set on the Image component, determining how the sprite will be rendered (e.g., Simple, Sliced, Tiled). The default is Image.Type.Sliced, which allows for scalable UI elements while maintaining borders.</param>
        /// <param name="raycastTarget">A boolean value to set on the Image component's raycastTarget property, determining whether the Image will block raycasts and receive input events. If true, the Image will be interactive and can respond to user input; if false, it will not block raycasts and will allow input events to pass through to elements behind it.</param>
        /// <param name="preserveAspect">A boolean value to set on the Image component's preserveAspect property, determining whether the aspect ratio of the sprite will be preserved when the Image is resized. If true, the Image will maintain the original aspect ratio of the sprite; if false, the Image may stretch to fit its RectTransform dimensions without preserving the aspect ratio.</param>
        /// <param name="name">The name of the new Image GameObject. The default is "Image".</param>
        /// <returns>The created GameObject with an Image component configured according to the provided parameters, parented to the given transform.</returns>
        public static GameObject CreateImage(Transform parent, Color color, Sprite sprite = null, Image.Type spriteType = Image.Type.Sliced, bool raycastTarget = false, bool preserveAspect = true, string name = "Image")
        {
            var go = CreateChild(parent, name, typeof(RectTransform), typeof(Image));
            return SetupImage(go.GetComponent<Image>(), color, sprite, spriteType, raycastTarget, preserveAspect).gameObject;
        }

        /// <summary>
        /// Set up an existing Image component with specified color, sprite, type, raycast target, and aspect preservation settings. This method configures the provided Image component according to the given parameters and returns the modified Image for further use or chaining.
        /// </summary>
        /// <param name="image">The Image component to be configured with the specified settings for color, sprite, type, raycast target, and aspect preservation. This Image will be modified according to the provided parameters and returned for further use or chaining.</param>
        /// <param name="color">The color to set on the Image component, which will determine the tint of the image. If a sprite is provided, this color will be multiplied with the sprite's colors; if no sprite is provided, this color will be used as a solid color for the Image.</param>
        /// <param name="sprite">The optional Sprite to set on the Image component, which will be displayed according to the specified spriteType. If null, the Image will use a solid color based on the provided color parameter.</param>
        /// <param name="spriteType">The Image.Type to set on the Image component, determining how the sprite will be rendered (e.g., Simple, Sliced, Tiled). The default is Image.Type.Sliced, which allows for scalable UI elements while maintaining borders.</param>
        /// <param name="raycastTarget">A boolean value to set on the Image component's raycastTarget property, determining whether the Image will block raycasts and receive input events. If true, the Image will be interactive and can respond to user input; if false, it will not block raycasts and will allow input events to pass through to elements behind it.</param>
        /// <param name="preserveAspect">A boolean value to set on the Image component's preserveAspect property, determining whether the aspect ratio of the sprite will be preserved when the Image is resized. If true, the Image will maintain the original aspect ratio of the sprite; if false, the Image may stretch to fit its RectTransform dimensions without preserving the aspect ratio.</param>
        /// <returns>The modified Image component configured according to the provided parameters, allowing for further use or chaining of method calls on this Image.</returns>
        public static Image SetupImage(Image image, Color color, Sprite sprite = null, Image.Type spriteType = Image.Type.Sliced, bool raycastTarget = false, bool preserveAspect = true)
        {
            image.color = color;
            image.sprite = sprite;
            image.type = spriteType;
            image.raycastTarget = raycastTarget;
            image.preserveAspect = preserveAspect;
            return image;
        }

        /// <summary>
        /// Set up a Canvas and its associated CanvasScaler with common settings for UI windows, including render mode, camera, plane distance, reference resolution, and scaling behavior.
        /// </summary>
        /// <param name="canvas">The Canvas component to configure with the specified settings for UI rendering.</param>
        /// <param name="scaler">The CanvasScaler component to configure with the specified settings for UI scaling behavior.</param>
        /// <param name="renderMode">The RenderMode to set on the Canvas, determining how it will be rendered (e.g., ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace).</param>
        /// <param name="renderCamera">The Camera to set as the worldCamera on the Canvas when using ScreenSpaceCamera render mode, defining the camera through which the UI will be rendered.</param>
        /// <param name="planeDistance">The distance from the camera to the plane on which the UI will be rendered when using ScreenSpaceCamera render mode, affecting the depth at which the UI appears in the scene.</param>
        /// <param name="referenceResolution">The reference resolution to set on the CanvasScaler, defining the base resolution for scaling the UI elements and determining how they will adapt to different screen sizes.</param>
        /// <param name="matchWidthOrHeight">The match value to set on the CanvasScaler, determining whether the scaling will prioritize width, height, or a combination of both when adapting the UI to different screen sizes (0 for width, 1 for height, or a value in between for a balanced approach).</param>
        public static void SetupCanvas(Canvas canvas, CanvasScaler scaler, RenderMode renderMode, Camera renderCamera, float planeDistance, Vector2 referenceResolution, float matchWidthOrHeight, int sortingOrder = 100)
        {
            canvas.renderMode = renderMode;
            canvas.pixelPerfect = true;
            canvas.sortingOrder = sortingOrder;
            canvas.vertexColorAlwaysGammaSpace = true;

            if (renderMode == RenderMode.ScreenSpaceCamera)
            {
                canvas.worldCamera = renderCamera ? renderCamera : Camera.main;
                canvas.planeDistance = planeDistance;
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = matchWidthOrHeight;
        }

        /// <summary>
        /// Set up a CanvasGroup with common settings for UI windows, including interaction, raycasting, and alpha transparency.
        /// </summary>
        /// <param name="canvasGroup">The CanvasGroup component to configure with the specified settings for interaction, raycasting, and alpha transparency, allowing it to control the visibility and interactivity of UI elements based on the provided parameters.</param>
        /// <param name="interactable">A boolean value to set on the CanvasGroup's interactable property, determining whether the UI elements within the CanvasGroup can be interacted with by the user (e.g., clicking buttons, selecting input fields).</param>
        /// <param name="blockRaycasts">A boolean value to set on the CanvasGroup's blocksRaycasts property, determining whether the UI elements within the CanvasGroup will block raycasts, which affects whether they can receive input events or allow them to pass through to elements behind them.</param>
        /// <param name="alpha">A float value to set on the CanvasGroup's alpha property, determining the transparency of the UI elements within the CanvasGroup, where 0 is fully transparent and 1 is fully opaque, allowing for control over the visibility of the UI elements based on this parameter.</param>
        public static void SetupCanvasGroup(CanvasGroup canvasGroup, bool interactable, bool blockRaycasts, float alpha = 1f)
        {
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = blockRaycasts;
            canvasGroup.alpha = alpha;
        }

        /// <summary>
        /// Set up a RectTransform to fill the entire screen with no anchoring or offset.
        /// </summary>
        /// <param name="rect">The RectTransform to configure to fill the entire screen, with anchors set to stretch across the full width and height, and offsets set to zero to ensure it occupies the entire area defined by its parent.</param>
        /// <param name="offset">An optional RectOffset to apply as padding to the RectTransform, which will be used to set the offsetMin and offsetMax properties, allowing for a margin around the edges of the RectTransform while still filling the screen. If null, no offset will be applied and the RectTransform will fill the entire screen without any padding.</param>
        public static void SetupRectTransformFullScreen(RectTransform rect, RectOffset offset = null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offset != null ? new Vector2(offset.left, offset.top) : Vector2.zero;
            rect.offsetMax = offset != null ? new Vector2(-offset.right, -offset.bottom) : Vector2.zero;
        }

        /// <summary>
        /// Set up a RectTransform with a centered pivot and a specified size. The anchors are set to the center of the parent, and the anchored position is set to zero, ensuring that the RectTransform is centered within its parent and has the specified size defined by the sizeDelta property.
        /// </summary>
        /// <param name="rect">The RectTransform to configure with a centered pivot and specified size, with anchors set to the center of the parent and anchored position set to zero, allowing it to be positioned at the center of its parent while maintaining the defined size through the sizeDelta property.</param>
        /// <param name="size">The size to set on the RectTransform, defined as a Vector2 representing the width and height, which will be applied to the sizeDelta property of the RectTransform to determine its dimensions while being centered within its parent.</param>
        /// <param name="offset">An optional RectOffset to apply as padding to the RectTransform, which will be used to set the offsetMin and offsetMax properties, allowing for a margin around the edges of the RectTransform while still being centered. If null, no offset will be applied and the RectTransform will be centered with the specified size without any padding.</param>
        public static void SetupRectTransformCentered(RectTransform rect, Vector2 size, RectOffset offset = null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            if (offset != null)
            {
                rect.offsetMin = new Vector2(offset.left, offset.top);
                rect.offsetMax = new Vector2(-offset.right, -offset.bottom);
            }
        }

        /// <summary>
        /// Configure a VerticalLayoutGroup with common settings for UI windows, including child control and expansion behavior, spacing, and padding.
        /// </summary>
        /// <param name="vlg">The VerticalLayoutGroup component to configure with the specified settings for arranging child elements in a vertical layout.</param>
        /// <param name="padding">The RectOffset to set as the padding on the VerticalLayoutGroup, defining the space between the edges of the layout group and its child elements, allowing for consistent spacing and alignment within the layout.</param>
        /// <param name="spacing">The spacing value to set on the VerticalLayoutGroup, determining the amount of space between child elements arranged in the vertical layout, allowing for consistent separation and visual organization of the elements within the layout group.</param>
        /// <param name="childControlHeight">A boolean value to set on the VerticalLayoutGroup's childControlHeight property, determining whether the layout group will control the height of its child elements. If true, the layout group will adjust the height of its child elements based on their preferred sizes and the available space; if false, the child elements will maintain their own heights and the layout group will not modify them.</param>
        public static void SetupVerticalLayoutGroup(VerticalLayoutGroup vlg, RectOffset padding, float spacing = 0f, bool childControlHeight = true)
        {
            vlg.childControlWidth = true;
            vlg.childControlHeight = childControlHeight;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = spacing;
            vlg.padding = padding;
        }

        /// <summary>
        /// Configure a HorizontalOrVerticalLayoutGroup with common settings for UI windows, including child control, expansion, alignment, and spacing.
        /// </summary>
        /// <param name="hvlg">The HorizontalOrVerticalLayoutGroup component to configure with the specified settings for arranging child elements in either a horizontal or vertical layout, allowing for flexible layout arrangements based on the specific needs of the UI design.</param>
        /// <param name="padding">The RectOffset to set as the padding on the HorizontalOrVerticalLayoutGroup, defining the space between the edges of the layout group and its child elements, allowing for consistent spacing and alignment within the layout regardless of whether it is arranged horizontally or vertically.</param>
        /// <param name="alignment">The TextAnchor value to set as the childAlignment on the HorizontalOrVerticalLayoutGroup, determining the alignment of child elements within the layout group, allowing for consistent positioning and visual organization of the elements based on the specified alignment setting.</param>
        /// <param name="spacing">The spacing value to set on the HorizontalOrVerticalLayoutGroup, determining the amount of space between child elements arranged in either a horizontal or vertical layout, allowing for consistent separation and visual organization of the elements within the layout group regardless of its orientation.</param>
        /// <param name="reverse">A boolean value to set on the HorizontalOrVerticalLayoutGroup's reverseArrangement property, determining whether the order of child elements will be reversed in the layout. If true, the child elements will be arranged in reverse order (e.g., right to left for horizontal layout or bottom to top for vertical layout); if false, they will be arranged in their natural order (e.g., left to right for horizontal layout or top to bottom for vertical layout).</param>
        public static void SetupHorizontalOrVerticalLayoutGroup(HorizontalOrVerticalLayoutGroup hvlg, RectOffset padding, TextAnchor alignment = TextAnchor.MiddleCenter, float spacing = 0f, bool reverse = false)
        {
            hvlg.childControlWidth = false;
            hvlg.childControlHeight = false;
            hvlg.childForceExpandWidth = false;
            hvlg.childForceExpandHeight = false;
            hvlg.childAlignment = alignment;
            hvlg.spacing = spacing;
            hvlg.padding = padding;
            hvlg.reverseArrangement = reverse;
        }

        /// <summary>
        /// Set up a ContentSizeFitter with specified horizontal and vertical fit modes.
        /// </summary>
        /// <param name="csf">The ContentSizeFitter component to configure with the specified fit modes for horizontal and vertical resizing behavior, allowing it to automatically adjust its size based on the content it contains according to the provided fit mode settings.</param>
        /// <param name="horizontalFit">The FitMode to set for horizontal resizing on the ContentSizeFitter, determining how it will adjust its width based on the content it contains (e.g., Unconstrained, MinSize, PreferredSize).</param>
        /// <param name="verticalFit">The FitMode to set for vertical resizing on the ContentSizeFitter, determining how it will adjust its height based on the content it contains (e.g., Unconstrained, MinSize, PreferredSize).</param>
        public static void SetupContentSizeFitter(ContentSizeFitter csf, ContentSizeFitter.FitMode horizontalFit, ContentSizeFitter.FitMode verticalFit)
        {
            csf.horizontalFit = horizontalFit;
            csf.verticalFit = verticalFit;
        }

        /// <summary>
        /// Set up a Button component with a target graphic, transition type, and click action. The method configures the Button's target graphic and transition settings, sets its interactable state based on whether an action is provided, and adds a click listener that invokes the specified action when the button is clicked. If no action is provided, the button will be non-interactable and will not perform any action when clicked.
        /// </summary>
        /// <param name="button">The Button component to configure with the specified target graphic, transition type, and click action, allowing it to respond to user interactions based on the provided settings.</param>
        /// <param name="targetGraphic">The Image component to set as the targetGraphic on the Button, which will be used for visual feedback during interactions (e.g., changing color on hover or click) based on the Button's transition settings.</param>
        /// <param name="transition">The Transition type to set on the Button, determining how it will visually respond to user interactions (e.g., ColorTint, SpriteSwap, Animation). The default is Selectable.Transition.ColorTint, which changes the color of the targetGraphic based on the interaction state.</param>
        /// <param name="action">The Action delegate to be invoked when the Button is clicked. If this parameter is null, the Button will be set to non-interactable and will not perform any action when clicked. If an Action is provided, it will be added as a click listener to the Button, allowing it to execute the specified behavior in response to user clicks.</param>
        /// <param name="colors">Optional ColorBlock for the ColorTint transition (normal/highlighted/pressed/selected/disabled). When null, the button keeps Unity's default block.</param>
        public static void SetupButton(Button button, Image targetGraphic, Selectable.Transition transition = Selectable.Transition.ColorTint, Action action = null, ColorBlock? colors = null)
        {
            button.targetGraphic = targetGraphic;
            button.transition = transition;
            if (colors.HasValue) button.colors = colors.Value;
            // button.interactable = action != null;
            AddClickListener(button, action);
        }

        /// <summary>
        /// Resolve the action to be performed based on the provided options, configCloseAction, and defaultAction. The method checks if optionsAction is not null and returns it; otherwise, it checks if configCloseAction is true and returns defaultAction. If neither condition is met, it returns null, indicating that no action should be performed.
        /// </summary>
        /// <param name="optionsAction">The Action delegate representing the action specified in the options, which will be returned if it is not null, indicating that this action should be performed when the relevant event occurs (e.g., a button click).</param>
        /// <param name="configCloseAction">A boolean value indicating whether the configuration specifies that a close action should be performed, which will be checked if optionsAction is null to determine whether to return the defaultAction or null, allowing for flexible behavior based on the provided options and configuration settings.</param>
        /// <param name="defaultAction">The Action delegate representing the default action to be performed if optionsAction is null and configCloseAction is true, which will be returned in this case to indicate that the default behavior should be executed when the relevant event occurs (e.g., closing a window).</param>
        /// <returns>The resolved Action delegate to be performed based on the provided options and configuration, which will be optionsAction if it is not null, defaultAction if optionsAction is null and configCloseAction is true, or null if neither condition is met, indicating that no action should be performed.</returns>
        public static Action ResolveClickAction(Action optionsAction, bool configCloseAction, Action defaultAction)
        {
            if (optionsAction != null) return optionsAction;
            return configCloseAction ? defaultAction : null;
        }

        /// <summary>
        /// Add a click listener to a button. The listener will invoke the provided action when the button is clicked. If the action is null, it will do nothing.
        /// </summary>
        /// <param name="button">The Button component to which the click listener will be added.</param>
        /// <param name="action">The Action delegate that will be invoked when the button is clicked. If this parameter is null, the click listener will not perform any action when the button is clicked.</param>
        public static void AddClickListener(Button button, Action action) => button.onClick.AddListener(() => action?.Invoke());

        /// <summary>
        /// Rebuild the layout of a RectTransform if its sizeDelta is less than the specified minHeight.
        /// </summary>
        /// <param name="rect">The RectTransform whose layout may need to be rebuilt based on its current sizeDelta and the specified minHeight. If the sizeDelta.y of this RectTransform is less than minHeight, the layout will be rebuilt to ensure that it meets the minimum height requirement.</param>
        /// <param name="minHeight"> The minimum height that the RectTransform should have. If the sizeDelta.y of the RectTransform is less than this value, the layout will be rebuilt to ensure that it meets this minimum height requirement.</param>
        public static void RebuildLayoutIfNeeded(RectTransform rect, float minHeight)
        {
            if (!(minHeight > 0)) return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            if (rect.sizeDelta.y < minHeight)
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, minHeight);
        }
    }
}
