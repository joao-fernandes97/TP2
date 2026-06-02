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

    private string _lossReason = "";

    void Awake()
    {
        GameManager.Instance.OnGameOver        += Show;
        GameManager.Instance.OnMilestoneChecked += OnMilestone;
        restartButton.onClick.AddListener(() =>
            SceneManager.LoadScene(SceneManager.GetActiveScene().name));
    }

    void OnMilestone(int day, int target, bool passed)
    {
        if (!passed)
            _lossReason = $"You needed {target} intel by Day {day}.\n" +
                        $"You had {GameManager.Instance.IntelCollected}.";
    }

    void Show(bool won)
    {
        resultLabel.text = won
            ? "THE MAZE IS MAPPED.\nYou got everyone home."
            : "THE MAZE WINS.\nNot enough made it back.";

        string milestoneNote = string.IsNullOrEmpty(_lossReason) ? "" : $"\n\n{_lossReason}";

        statsLabel.text = $"Days survived: {GameManager.Instance.CurrentDay}\n" +
                        $"Intel collected: {GameManager.Instance.IntelCollected}" +
                        $" / {GameManager.Instance.IntelToWin}" +
                        milestoneNote;

        _lossReason = ""; // reset for next run
    }
}