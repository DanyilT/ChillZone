using UnityEngine;
using UnityEngine.UI;

public class SkinsManager : MonoBehaviour
{
    public GameObject[] balls;
    public Button[] buttons;
    public Counting ballManager;
    public GameObject[] ballPrefab;
    public Button resetButton;

    void Start()
    {
        // Прив'язка функції до кнопок
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i; // Локальна копія змінної для замикання
            buttons[i].onClick.AddListener(() => SelectBall(index));
        }

        // Активуємо перший м'яч за замовчуванням
        SelectBall(0);
    }

    void SelectBall(int index)
    {
        // Вимкнення всіх м'ячів
        foreach (GameObject ball in balls)
        {
            ball.SetActive(false);
        }

        // Активуємо вибраний м'яч і змінюємо змінну в Counting
        if (index >= 0 && index < balls.Length)
        {
            balls[index].SetActive(true);
            ballManager.ball = ballPrefab[index];
        }

        // Налаштування кнопки скидання
        resetButton.onClick.RemoveAllListeners(); // Очищення існуючих слухачів
        BallController ballController = balls[index].GetComponent<BallController>();
        if (ballController != null)
        {
            resetButton.onClick.AddListener(() => ballController.Reset(true));
        }
    }
}
