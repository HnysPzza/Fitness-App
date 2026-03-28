namespace Fitness_App.Services;

/// <summary>Raised when a workout is saved so Home/You can refresh without tab-scoped messenger registration.</summary>
public interface IActivitySaveNotifier
{
    event EventHandler? ActivitySaved;
    void NotifyActivitySaved();
}

public sealed class ActivitySaveNotifier : IActivitySaveNotifier
{
    public event EventHandler? ActivitySaved;

    public void NotifyActivitySaved()
    {
        ActivitySaved?.Invoke(this, EventArgs.Empty);
    }
}
