using UnityEngine;
using DG.Tweening;

public class CreditsManager : MonoBehaviour
{
    [SerializeField] private GameObject creditsModal;
    private RectTransform modalRectTransform;
    [SerializeField] private float animationDuration = 0.4f;

    [SerializeField] private int num = 0;

    private void Start()
    {
        modalRectTransform = creditsModal.GetComponent<RectTransform>();
        creditsModal.SetActive(false);
    }

    public void OpenCreditsModal()
    {
        creditsModal.SetActive(true);

        if (num == 0)
        {
            modalRectTransform.anchoredPosition = new Vector2(0, -Screen.height);
            modalRectTransform.DOAnchorPosY(0, animationDuration).SetEase(Ease.OutExpo);
        }
        else
        {
            modalRectTransform.anchoredPosition = new Vector2(0, Screen.height);
            modalRectTransform.DOAnchorPosY(0, animationDuration).SetEase(Ease.OutExpo);
        }
    }

    public void CloseCreditsModal()
	{
        if (num == 0)
        {
            modalRectTransform.DOAnchorPosY(-Screen.height, animationDuration).SetEase(Ease.InExpo).OnComplete(() => {
                creditsModal.SetActive(false);
            });
        }
        else
        {
            modalRectTransform.DOAnchorPosY(Screen.height, animationDuration).SetEase(Ease.InExpo).OnComplete(() => {
                creditsModal.SetActive(false);
            });
        }
    }
}
