// TopBarUI.cs
// Assets/Scripts/UI/TopBarUI.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TopBarUI : MonoBehaviour
{
    [SerializeField] private TMP_Text  dayLabel;
    [SerializeField] private Slider    dayProgressBar;
    [SerializeField] private Slider    intelBar;
    [SerializeField] private Button    recallButton;
    [SerializeField] private TMP_Text  recallLabel;

    void Awake()
    {
        DayManager.Instance.OnBriefingStarted    += OnBriefing;
        DayManager.Instance.OnDispatchStarted    += OnDispatch;
        DayManager.Instance.OnDayProgressUpdated += p => dayProgressBar.value = p;

        GameManager.Instance.OnDayChanged    += d => dayLabel.text = $"Day {d}";
        GameManager.Instance.OnIntelChanged  += UpdateIntel;

        recallButton.onClick.AddListener(DayManager.Instance.IssueRecall);
    }
    
    void Start()
    {
        // Init
        dayLabel.text        = $"Day {GameManager.Instance.CurrentDay}";
        dayProgressBar.value = 0f;
        intelBar.minValue    = 0f;
        intelBar.maxValue    = 1f;
        intelBar.value       = 0f;
        recallButton.interactable = false;
    }

    void OnBriefing()
    {
        dayProgressBar.value      = 0f;
        recallButton.interactable = false;
        recallLabel.text          = "Recall";
        dayLabel.text             = $"Day {GameManager.Instance.CurrentDay}";
    }

    void OnDispatch()
    {
        recallButton.interactable = true;
    }

    void UpdateIntel(int total)
    {
        intelBar.value = GameManager.Instance.IntelProgress;
    }
}