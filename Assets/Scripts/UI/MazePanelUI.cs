// MazePanelUI.cs
// Assets/Scripts/UI/MazePanelUI.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MazePanelUI : MonoBehaviour
{
    [SerializeField] private Transform  statusList;
    [SerializeField] private GameObject explorerStatusRowPrefab;
    [SerializeField] private TMP_Text   eventLogText;
    [SerializeField] private ScrollRect eventLogScroll;
    [SerializeField] private TMP_Text  depthLabel;

    private readonly Dictionary<Explorer, ExplorerStatusRowUI> _rows = new();

    void Awake()
    {
        if (DayManager.Instance == null)
        {
            Debug.LogError("MazePanelUI: DayManager.Instance is null in Awake! " +
                        "Check script execution order.");
            return;
        }
        if (EventManager.Instance == null)
        {
            Debug.LogError("MazePanelUI: EventManager.Instance is null in Awake!");
            return;
        }

        DayManager.Instance.OnDispatchStarted        += BuildStatusList;
        DayManager.Instance.OnReturnStarted          += RefreshAllRows;
        DayManager.Instance.OnDayProgressUpdated     += OnDayProgressUpdated;
        GameManager.Instance.OnExplorerStatusChanged += RefreshRow;
        EventManager.Instance.OnEventResolved        += AppendLog;
        GameManager.Instance.OnExplorerStatsChanged  += RefreshRow;

        Debug.Log("MazePanelUI: Subscriptions registered.");
    }

    void BuildStatusList()
    {
        Debug.Log("[MazePanel] BuildStatusList");
        foreach (var row in _rows.Values) Destroy(row.gameObject);
        _rows.Clear();
        eventLogText.text = "";
        
        if (depthLabel != null) depthLabel.text = "Distance: 0m";

        foreach (var explorer in GameManager.Instance.AllExplorers)
        {
            Debug.Log("[MazePanel] instantiate explorer");
            if (explorer.Status != ExplorerStatus.Exploring) continue;

            var go  = Instantiate(explorerStatusRowPrefab, statusList);
            var row = go.GetComponent<ExplorerStatusRowUI>();
            row.Setup(explorer);
            _rows[explorer] = row;
        }
    }

    void OnDayProgressUpdated(float _)
    {
        if (depthLabel == null) return;
        int metres = Mathf.RoundToInt(DayManager.Instance.PartyDepthMeters);
        string prefix = DayManager.Instance.IsReturning ? "Returning:" : "Distance:";
        depthLabel.text = $"{prefix}  {metres}m";
    }

    void RefreshRow(Explorer explorer)
    {
        if (_rows.TryGetValue(explorer, out var row))
            row.Refresh();
    }

    void RefreshAllRows()
    {
        foreach (var row in _rows.Values)
            row.Refresh();
    }

    void AppendLog(string message)
    {
        eventLogText.text += $"\n{message}";

        // Scroll to bottom
        Canvas.ForceUpdateCanvases();
        eventLogScroll.verticalNormalizedPosition = 0f;
    }
}