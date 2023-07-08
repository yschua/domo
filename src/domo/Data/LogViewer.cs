namespace domo.Data;

public class LogViewer : IDisposable
{
    private readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(5);
    private const int MaxMessages = 500;
    private readonly LinkedList<string> _messages = new();
    private bool _messagesChanged;
    private readonly System.Timers.Timer _timer;

    public LogViewer()
    {
        _timer = new System.Timers.Timer(RefreshInterval);
        _timer.Elapsed += (_, _) =>
        {
            lock (_messages)
            {
                if (_messagesChanged)
                {
                    Published?.Invoke(this, new EventArgs());
                    _messagesChanged = false;
                }
            }
        };
        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    public event EventHandler? Published;

    public IEnumerable<string> Entries
    {
        get
        {
            LinkedList<string> copy;
            lock (_messages)
            {
                copy = new LinkedList<string>(_messages);
            }
            return copy;
        }
    }

    public void Publish(string message)
    {
        lock (_messages)
        {
            _messages.AddFirst(message);
            if (_messages.Count > MaxMessages)
            {
                _messages.RemoveLast();
            }
            _messagesChanged = true;
        }
    }
}
