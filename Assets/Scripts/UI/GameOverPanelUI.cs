// GameOverPanelUI.cs
// Assets/Scripts/UI/GameOverPanelUI.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverPanelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text resultLabel;
    [SerializeField] private TMP_Text statsLabel;
    [SerializeField] private Button   restartButton;

    void Awake()
    {
        GameManager.Instance.OnGameOver += Show;
        restartButton.onClick.AddListener(() =>
            SceneManager.LoadScene(SceneManager.GetActiveScene().name));
    }

    void Show(bool won)
    {
        resultLabel.text = won ? "THE MAZE IS MAPPED.\nYou got everyone home." 
                               : "THE MAZE WINS.\nNot enough made it back.";

        statsLabel.text  = $"Days survived: {GameManager.Instance.CurrentDay}\n" +
                           $"Intel collected: {GameManager.Instance.IntelCollected}" +
                           $" / {GameManager.Instance.IntelToWin}";
    }
}