using UnityEngine;

public class OpenURL : MonoBehaviour
{
    private string urlOnBuyMeACoffee = "https://www.buymeacoffee.com/DanyT"; // The URL to open in a web browser
    private string urlOnComeBackAvile = "https://savelife.in.ua/en/"; // The URL to open in a web browser
    private string urlOnAppInGooglePlay = "https://play.google.com/store/apps/details?id=com.DanyT.ChillZone"; // The URL to open in a web browser
    private string urlOnAppInAppStore = "https://apps.apple.com/us/app/chillzone-danyt/id6448560219"; // The URL to open in a web browser
    private string urlOnTestingForm = "https://yo2aqdvjya1.typeform.com/to/lt7dIQwd"; // The URL to open in a web browser

    public void OpenURLOnByMeACoffee()
    {
        #if UNITY_IOS
            // Open the URL in a web browser
            Application.OpenURL("https://danyt-chillzone.com");
        #else
            // Open the URL in a web browser
            Application.OpenURL(urlOnBuyMeACoffee);
        #endif
    }

    public void OpenURLOnComeBackAvile()
    {
        // Open the URL in a web browser
        Application.OpenURL(urlOnComeBackAvile);
    }

    public void OpenURLOnAppPage()
    {
        #if UNITY_ANDROID
            // Open the URL in a web browser
            Application.OpenURL(urlOnAppInGooglePlay);
        #elif UNITY_IOS
            // Open the URL in a web browser
            Application.OpenURL(urlOnAppInAppStore);
        #else
            // Open the URL in a web browser
            Application.OpenURL("https://danyt-chillzone.com");
        #endif
    }

    public void OpenURLOnTestingForm()
    {
        // Open the URL in a web browser
        Application.OpenURL(urlOnTestingForm);
    }
}
