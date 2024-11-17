using UnityEngine;
using System.Collections;
using TMPro;
using System.Globalization;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine.UI;
using DG.Tweening;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI maxScoreText;

    [SerializeField] private GameObject modalWindow;
    [SerializeField] private float animationDuration = 0.3f;

    public bool show;

    private void Update()
	{
        gameObject.GetComponent<TextMeshProUGUI>().text = PlayerPrefs.GetInt("ScoreKey", 0) + "";

        if (PlayerPrefs.GetInt("ScoreKey") > PlayerPrefs.GetInt("MaxScoreKey", 0))
        {
            PlayerPrefs.SetInt("MaxScoreKey", PlayerPrefs.GetInt("ScoreKey", 0));
        }
    }

    private void Start()
    {
        // Hide the modal window by setting its scale to zero
        modalWindow.GetComponent<RectTransform>().localScale = Vector3.zero;

        show = false;
    }

    public void ShowModalWindow()
    {
        show = !show;

        if (show)
        {
            // Scale in the modal window
            modalWindow.GetComponent<RectTransform>().DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
            maxScoreText.text = PlayerPrefs.GetInt("MaxScoreKey", 0) + "";
        }
        else
        {
            // Scale in the modal window
            modalWindow.GetComponent<RectTransform>().DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack);
        }
    }
}
