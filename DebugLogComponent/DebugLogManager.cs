using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

[Serializable]
public class DebugLogEntry
{
    public DateTime Time { get; private set; }
    public LogType Type { get; private set; }
    public string Message { get; private set; }
    public string StackTrace { get; private set; }

    public DebugLogEntry(DateTime time, LogType type, string message, string stackTrace)
    {
        Time = time;
        Type = type;
        Message = message;
        StackTrace = stackTrace;
    }

    // ── 优化：副本复制 ────────────────────────────────
    // 从 _logs 中的 canonical 对象复制字段到池中的副本对象
    public void CopyFrom(DebugLogEntry other)
    {
        Time = other.Time;
        Type = other.Type;
        Message = other.Message;
        StackTrace = other.StackTrace;
    }

    public override string ToString()
    {
        return $"[{Time:HH:mm:ss.fff}][{Type}] {Message}";
    }

    public string ToDetailedString()
    {
        return $"[{Time:yyyy-MM-dd HH:mm:ss.fff}][{Type}] {Message}\n{StackTrace}";
    }

    public string ToCsvLine()
    {
        string msg = Message.Replace("\"", "\"\"");
        string stack = StackTrace.Replace("\"", "\"\"");
        return $"\"{Time:yyyy-MM-dd HH:mm:ss.fff}\",\"{Type}\",\"{msg}\",\"{stack}\"";
    }

    public static string CsvHeader => "Time,Type,Message,StackTrace";
}

public class DebugLogManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _maxLogCount = 2000;

    private static DebugLogManager _instance;
    public static DebugLogManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DebugLogManager>();
                if (_instance == null)
                {
                    var go = new GameObject("[DebugLogManager]");
                    _instance = go.AddComponent<DebugLogManager>();
                }
            }
            return _instance;
        }
    }

    // _logs：主列表，存所有已处理的日志（canonical 数据）
    // _pendingLogs：线程安全队列，接收子线程的日志
    // 对象池已移至 DebugLogPanel（可见窗口回收）
    private readonly List<DebugLogEntry> _logs = new List<DebugLogEntry>();
    private readonly ConcurrentQueue<DebugLogEntry> _pendingLogs = new ConcurrentQueue<DebugLogEntry>();

    public event Action<DebugLogEntry> OnLogReceived;
    public IReadOnlyList<DebugLogEntry> AllLogs => _logs;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(_instance.gameObject);
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        Application.logMessageReceivedThreaded += OnLogMessageReceived;
    }

    private void OnDestroy()
    {
        Application.logMessageReceivedThreaded -= OnLogMessageReceived;
    }

    // 每个日志消息创建一个 canonical 对象，不涉及池
    private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        _pendingLogs.Enqueue(new DebugLogEntry(DateTime.Now, type, condition, stackTrace));
    }

    // 主线程 Drain：超过 _maxLogCount 时从头部移除旧日志
    private void Update()
    {
        while (_pendingLogs.TryDequeue(out var entry))
        {
            if (_logs.Count >= _maxLogCount)
                _logs.RemoveAt(0);
            _logs.Add(entry);
            OnLogReceived?.Invoke(entry);
        }
    }

    public List<DebugLogEntry> GetFilteredLogs(string keyword = null, LogType? typeFilter = null)
    {
        IEnumerable<DebugLogEntry> query = _logs;
        if (typeFilter.HasValue)
            query = query.Where(e => e.Type == typeFilter.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(e => e.Message.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
        return query.ToList();
    }

    public void ClearLogs()
    {
        _logs.Clear();
    }

    public static string DefaultExportPath
    {
        get
        {
            string path = Path.Combine(Application.persistentDataPath, "DebugLogs");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }
    }

    public string ExportToCsv(string folderPath = null)
    {
        folderPath ??= DefaultExportPath;
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
        string filePath = Path.Combine(folderPath, $"debug_log_{timestamp}.csv");
        var sb = new StringBuilder();
        sb.AppendLine(DebugLogEntry.CsvHeader);
        foreach (var log in _logs)
            sb.AppendLine(log.ToCsvLine());
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"[DebugLogManager] Exported {_logs.Count} log entries to: {filePath}");
        return filePath;
    }

    public string ExportToCsv(IReadOnlyList<DebugLogEntry> logs, string folderPath = null, string suffix = "")
    {
        folderPath ??= DefaultExportPath;
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
        string filePath = Path.Combine(folderPath, $"debug_log_{timestamp}{suffix}.csv");
        var sb = new StringBuilder();
        sb.AppendLine("Index," + DebugLogEntry.CsvHeader);
        for (int i = 0; i < logs.Count; i++)
            sb.AppendLine($"#{i + 1},{logs[i].ToCsvLine()}");
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"[DebugLogManager] Exported {logs.Count} log entries to: {filePath}");
        return filePath;
    }

    public string ExportToTxt(string folderPath = null, string keyword = null)
    {
        folderPath ??= DefaultExportPath;
        var logs = string.IsNullOrWhiteSpace(keyword)
            ? (IReadOnlyList<DebugLogEntry>)_logs
            : GetFilteredLogs(keyword);
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
        string suffix = string.IsNullOrWhiteSpace(keyword) ? "" : $"_filtered_{SanitizeFileName(keyword)}";
        string filePath = Path.Combine(folderPath, $"debug_log_{timestamp}{suffix}.txt");
        var sb = new StringBuilder();
        sb.AppendLine($"Debug Log Export - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Total Entries: {logs.Count}");
        if (!string.IsNullOrWhiteSpace(keyword))
            sb.AppendLine($"Filter Keyword: {keyword}");
        sb.AppendLine(new string('=', 60));
        foreach (var log in logs)
            sb.AppendLine(log.ToDetailedString() + "\n");
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"[DebugLogManager] Exported {logs.Count} log entries to: {filePath}");
        return filePath;
    }

    public string ExportToTxt(IReadOnlyList<DebugLogEntry> logs, string folderPath = null, string suffix = "")
    {
        folderPath ??= DefaultExportPath;
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
        string filePath = Path.Combine(folderPath, $"debug_log_{timestamp}{suffix}.txt");
        var sb = new StringBuilder();
        sb.AppendLine($"Debug Log Export - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Total Entries: {logs.Count}");
        sb.AppendLine(new string('=', 60));
        for (int i = 0; i < logs.Count; i++)
            sb.AppendLine($"#{i + 1}:\n{logs[i].ToDetailedString()}\n");
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"[DebugLogManager] Exported {logs.Count} log entries to: {filePath}");
        return filePath;
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Length > 50 ? name[..50] : name;
    }
}
