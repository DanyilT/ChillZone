using UnityEngine;
using System.Collections;

public class iOSSettings : MonoBehaviour
{
    // Use this for initialization
    void Start()
	{
		GameObject iOS = GameObject.FindGameObjectWithTag("iOS");
		GameObject Android = GameObject.FindGameObjectWithTag("Android");

		#if UNITY_IOS
			iOS.SetActive(true);
			Android.SetActive(false);
		#else
			iOS.SetActive(false);
			Android.SetActive(true);
		#endif
	}
}

