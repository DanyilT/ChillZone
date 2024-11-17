using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GiftCodeManager : MonoBehaviour
{
    [SerializeField] private InputField giftCodeInputField;
    [SerializeField] private Button submitButton;

    private void Start()
    {
        submitButton.onClick.AddListener(OnSubmitButtonClick);
        giftCodeInputField.onEndEdit.AddListener(OnInputFieldEndEdit);
    }

    private void OnSubmitButtonClick()
    {
        string giftCode = giftCodeInputField.text;
        ValidateGiftCode(giftCode);
    }

    private void OnInputFieldEndEdit(string giftCode)
    {
        ValidateGiftCode(giftCode);
    }



    private void ValidateGiftCode(string giftCode)
    {
        // Add your validation logic here, for example:
        if (giftCode == "qwerty")
        {
            Debug.Log("Gift code is valid, granting gift.");
            // Grant the gift to the user, for example, by adding items to their inventory

            PlayerPrefs.SetInt("ScoreKey", 933476);
        }
        else
        {
            Debug.Log("Invalid gift code.");
            // Show an error message to the user
        }
    }
}
