using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SoundEffectForButtonClick : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] int num;

    public void OnPointerClick(PointerEventData eventData)
    {
        AudioManager.Instance.PlayButtonClick(num);
    }
}
