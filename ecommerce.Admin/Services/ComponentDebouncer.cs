using Timer = System.Timers.Timer;

namespace ecommerce.Admin.Services;

public class ComponentDebouncer: IDisposable
{
    private Timer? timer;
    private DateTime timerStarted { get; set; } = DateTime.UtcNow.AddYears(-1);

    public void Debounce(int interval, Func<Task> action)
    {
        timer?.Stop();
        timer = null;

        timer = new Timer() { Interval = interval, Enabled = false, AutoReset = false };
        timer.Elapsed += (s, e) =>
        {
            if (timer == null)
            {
                return;
            }

            timer?.Stop();
            timer = null;

            try
            {
                Task.Run(action);
            }
            catch (TaskCanceledException)
            {
                //
            }
        };

        timer.Start();
    }

    public void Throttle(int interval, Func<Task> action)
    {
        timer?.Stop();
        timer = null;

        var curTime = DateTime.UtcNow;

        if (curTime.Subtract(timerStarted).TotalMilliseconds < interval)
        {
            interval -= (int) curTime.Subtract(timerStarted).TotalMilliseconds;
        }

        timer = new Timer() { Interval = interval, Enabled = false, AutoReset = false };
        timer.Elapsed += (s, e) =>
        {
            if (timer == null)
            {
                return;
            }

            timer?.Stop();
            timer = null;

            try
            {
                Task.Run(action);
            }
            catch (TaskCanceledException)
            {
                //
            }
        };

        timer.Start();
        timerStarted = curTime;
    }

    public void Dispose()
    {
        timer?.Dispose();
    }
}