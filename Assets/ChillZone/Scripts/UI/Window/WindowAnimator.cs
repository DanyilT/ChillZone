using System;
using ChillZone.UI.Window.Config;
using DG.Tweening;
using UnityEngine;

namespace ChillZone.UI.Window
{
    /// <summary>
    /// Plays show / hide tweens on a WindowObject's backdrop and panel.
    /// All methods are safe to call with a null config (they become no-ops).
    /// </summary>
    public static class WindowAnimator
    {
        /// <summary>
        /// Plays the show animation. Backdrop always fades in; panel uses the configured type.
        /// </summary>
        public static void PlayShow(RectTransform panel, CanvasGroup panelCg, float panelTargetAlpha, CanvasGroup backdropCg, float backdropTargetAlpha, WindowAnimationConfig config)
        {
            if (!config) return;

            KillTweens(panel, panelCg, backdropCg);

            // Backdrop: always fades in.
            backdropCg.alpha = 0f;
            backdropCg.DOFade(backdropTargetAlpha, config.showDuration * 0.6f)
                      .SetEase(Ease.OutQuad)
                      .SetUpdate(true);

            switch (config.showAnimation)
            {
                case WindowAnimationConfig.AnimationType.None:
                    panelCg.alpha = panelTargetAlpha;
                    break;

                case WindowAnimationConfig.AnimationType.Fade:
                    panelCg.alpha = 0f;
                    panelCg.DOFade(panelTargetAlpha, config.showDuration)
                           .SetEase(config.showEase)
                           .SetUpdate(true);
                    break;

                case WindowAnimationConfig.AnimationType.Pop:
                    panel.localScale = Vector3.one * 0.85f;
                    panelCg.alpha    = 0f;
                    panel.DOScale(Vector3.one, config.showDuration)
                         .SetEase(config.showEase)
                         .SetUpdate(true);
                    panelCg.DOFade(panelTargetAlpha, config.showDuration * 0.55f)
                           .SetEase(Ease.OutQuad)
                           .SetUpdate(true);
                    break;

                case WindowAnimationConfig.AnimationType.SlideFromBottom:
                    SlideShow(panel, panelCg, panelTargetAlpha, config, Vector2.down);
                    break;

                case WindowAnimationConfig.AnimationType.SlideFromTop:
                    SlideShow(panel, panelCg, panelTargetAlpha, config, Vector2.up);
                    break;

                case WindowAnimationConfig.AnimationType.SlideFromLeft:
                    SlideShow(panel, panelCg, panelTargetAlpha, config, Vector2.left);
                    break;

                case WindowAnimationConfig.AnimationType.SlideFromRight:
                    SlideShow(panel, panelCg, panelTargetAlpha, config, Vector2.right);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Plays the hide animation. Calls <paramref name="onComplete"/> when finished.
        /// If config is null or AnimationType.None, calls onComplete immediately.
        /// </summary>
        public static void PlayHide(RectTransform panel, CanvasGroup panelCg, CanvasGroup backdropCg, WindowAnimationConfig config, TweenCallback onComplete)
        {
            if (!config || config.hideAnimation == WindowAnimationConfig.AnimationType.None)
            {
                onComplete?.Invoke();
                return;
            }

            KillTweens(panel, panelCg, backdropCg);

            var seq = DOTween.Sequence().SetUpdate(true).OnComplete(onComplete);

            // Backdrop: always fades out.
            seq.Join(backdropCg.DOFade(0f, config.hideDuration).SetEase(Ease.InQuad));

            switch (config.hideAnimation)
            {
                case WindowAnimationConfig.AnimationType.Fade:
                    seq.Join(panelCg.DOFade(0f, config.hideDuration).SetEase(config.hideEase));
                    break;

                case WindowAnimationConfig.AnimationType.Pop:
                    seq.Join(panel.DOScale(Vector3.one * 0.85f, config.hideDuration).SetEase(config.hideEase));
                    seq.Join(panelCg.DOFade(0f, config.hideDuration * 0.6f).SetEase(Ease.InQuad));
                    break;

                case WindowAnimationConfig.AnimationType.SlideFromBottom:
                    SlideHide(panel, panelCg, config, Vector2.down, seq);
                    break;

                case WindowAnimationConfig.AnimationType.SlideFromTop:
                    SlideHide(panel, panelCg, config, Vector2.up, seq);
                    break;

                case WindowAnimationConfig.AnimationType.SlideFromLeft:
                    SlideHide(panel, panelCg, config, Vector2.left, seq);
                    break;

                case WindowAnimationConfig.AnimationType.SlideFromRight:
                    SlideHide(panel, panelCg, config, Vector2.right, seq);
                    break;

                case WindowAnimationConfig.AnimationType.None:
                default:
                    seq.Join(panelCg.DOFade(0f, config.hideDuration).SetEase(config.hideEase));
                    break;
            }
        }

        #region helpers

        private static void SlideShow(RectTransform panel, CanvasGroup panelCg, float targetAlpha, WindowAnimationConfig config, Vector2 direction)
        {
            var target = panel.anchoredPosition;                          // always (0,0) for centred panels
            panel.anchoredPosition = target + direction * config.slideOffset;
            panelCg.alpha = 0f;

            panel.DOAnchorPos(target, config.showDuration)
                 .SetEase(config.showEase)
                 .SetUpdate(true);
            panelCg.DOFade(targetAlpha, config.showDuration * 0.5f)
                   .SetEase(Ease.OutQuad)
                   .SetUpdate(true);
        }

        private static void SlideHide(RectTransform panel, CanvasGroup panelCg, WindowAnimationConfig config, Vector2 direction, Sequence seq)
        {
            var target = panel.anchoredPosition + direction * config.slideOffset;
            seq.Join(panel.DOAnchorPos(target, config.hideDuration).SetEase(config.hideEase));
            seq.Join(panelCg.DOFade(0f, config.hideDuration * 0.5f).SetEase(Ease.InQuad));
        }

        private static void KillTweens(RectTransform panel, CanvasGroup panelCg, CanvasGroup backdropCg)
        {
            panel?.DOKill();
            panelCg?.DOKill();
            backdropCg?.DOKill();
        }

        #endregion
    }
}
