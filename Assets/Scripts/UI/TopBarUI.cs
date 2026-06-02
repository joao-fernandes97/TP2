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
    [SerializeField] private TMP_Text  intelLabel;
    [SerializeField] private Button    recallButton;
    [SerializeField] private TMP_Text  recallLabel;
    

    void Awake()
    {
        DayManager.Instance.OnBriefingStarted    += OnBriefing;
        DayManager.Instance.OnDispatchStarted    += OnDispatch;
        DayManager.Instance.OnReturnStarted      += OnReturn;
        DayManager.Instance.OnDayProgressUpdated += OnDayProgressUpdated;

        GameManager.Instance.OnDayChanged    += d => dayLabel.text = $"Day {d}";
        GameManager.Instance.OnIntelChanged  += UpdateIntel;
        GameManager.Instance.OnMilestoneChecked += (day, target, passed) =>
        {
            if (!passed) intelLabel.text = $"TARGET FAILED";
        };

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
        intelLabel.text = $"{GameManager.Instance.IntelCollected}/{GameManager.Instance.Day5MilestoneIntel} by Day 5";
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

    void OnReturn()
    {
        recallButton.interactable = false;
        recallLabel.text          = "Returning…";
    }

    void OnDayProgressUpdated(float dayProgress)
    {
        dayProgressBar.value = dayProgress;
    }

    void UpdateIntel(int total)
    {
        intelBar.value = GameManager.Instance.IntelProgress;
        
        int day = GameManager.Instance.CurrentDay;
        if (day <= 5)
            intelLabel.text = $"{GameManager.Instance.IntelCollected}/{GameManager.Instance.Day5MilestoneIntel} by Day 5";
        else if (day <= 10)
            intelLabel.text = $"{GameManager.Instance.IntelCollected}/{GameManager.Instance.Day10MilestoneIntel} by Day 10";
        else
            intelLabel.text = $"{GameManager.Instance.IntelCollected}/{GameManager.Instance.IntelToWin} by Day 15";
    }
}