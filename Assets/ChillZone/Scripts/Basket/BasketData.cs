using UnityEngine;

namespace ChillZone.Basket
{
    [CreateAssetMenu(fileName = "Basket_New", menuName = "ChillZone/Content/Basket Data")]
    public class BasketData : Content.UnlockableContent
    {
        public override Content.ContentTypes ContentType => Content.ContentTypes.Basket;

        [Header("Basket Visuals")]
        [Tooltip("The basket prefab. Must carry a BasketController and a ScoreZone-tagged trigger collider at the hoop opening.")]
        public GameObject prefab;

        [Header("Scoring")]
        [Tooltip("Multiplier applied to every basket scored with this hoop (e.g. >1 for a smaller / harder target).")]
        public float scoreMultiplier = 1f;

#if UNITY_EDITOR
        private void Reset() => id = "basket-";
#endif
    }
}
