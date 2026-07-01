using DG.Tweening;
using UnityEngine;

namespace ChillZone.UI.Window.Config
{
    [CreateAssetMenu(fileName = "WindowAnimationConfig", menuName = "ChillZone/UI/Window Animation Config", order = 025)]
    public class WindowAnimationConfig : ScriptableObject
    {
        [Header("Show")]
        public AnimationType showAnimation = AnimationType.Pop;
        public float showDuration = 0.28f;
        public Ease showEase = Ease.OutBack;

        [Header("Hide")]
        public AnimationType hideAnimation = AnimationType.Fade;
        public float hideDuration = 0.18f;
        public Ease hideEase = Ease.InCubic;

        [Header("Slide Configuration"), Tooltip("How far off-centre the panel starts for slide animations (canvas units).")]
        public float slideOffset = 900f;

        public enum AnimationType
        {
            None,
            Fade,
            SlideFromBottom,
            SlideFromTop,
            SlideFromLeft,
            SlideFromRight,
            Pop,
        }
    }
}
