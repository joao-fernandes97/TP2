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

    void RefreshRow(Explorer explorer)
    {
        if (_rows.TryGetValue(explorer, out var row))
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