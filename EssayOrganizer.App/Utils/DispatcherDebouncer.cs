using System.Windows.Threading;

namespace EssayOrganizer.App.Utils;

public class DispatcherDebouncer
{
    private readonly DispatcherTimer _timer;
    private Action? _action;

    public DispatcherDebouncer(TimeSpan interval)
    {
        _timer = new DispatcherTimer { Interval = interval };
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            _action?.Invoke();
        };
    }

    public void Debounce(Action action)
    {
        _action = action;
        _timer.Stop();
        _timer.Start();
    }

    public void Cancel()
    {
        _timer.Stop();
    }
}

