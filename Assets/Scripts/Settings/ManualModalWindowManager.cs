using UnityEngine;
using DG.Tweening;

public class ManualModalWindowManager : MonoBehaviour
{
    [SerializeField] private GameObject manualModal;
    private RectTransform modalRectTransform;
    [SerializeField] private float animationDuration = 0.4f;

    private void Start()
    {
        modalRectTransform = manualModal.GetComponent<RectTransform>();
        manualModal.SetActive(PlayerPrefs.GetInt("ModalWindowFirstTimeOpeningKey", 1) == 1 ? true : false);
        PlayerPrefs.SetInt("ModalWindowFirstTimeOpeningKey", 0);
    }

    public void OpenModal()
    {
        manualModal.SetActive(true);

        modalRectTransform.anchoredPosition = new Vector2(Screen.width, 0);
        modalRectTransform.DOAnchorPosX(0, animationDuration).SetEase(Ease.OutExpo);
    }

    public void CloseModal()
    {
        modalRectTransform.DOAnchorPosX(Screen.width, animationDuration).SetEase(Ease.InExpo).OnComplete(() => {
            manualModal.SetActive(false);
        });
    }
}
