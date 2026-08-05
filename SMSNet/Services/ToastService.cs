namespace SMSNet.Services;

public enum ToastTone { Info, Good, Warn, Bad }

public sealed record Toast(Guid Id, string Message, ToastTone Tone);

/// <summary>
/// Transient feedback for the current circuit. Registered scoped, so each
/// browser connection has its own queue.
/// </summary>
public sealed class ToastService
{
    private readonly List<Toast> _items = new();

    public IReadOnlyList<Toast> Items => _items;

    public event Action? Changed;

    public void Show(string message, ToastTone tone = ToastTone.Info)
    {
        _items.Add(new Toast(Guid.NewGuid(), message, tone));
        Changed?.Invoke();
    }

    public void Success(string message) => Show(message, ToastTone.Good);

    public void Error(string message) => Show(message, ToastTone.Bad);

    public void Warn(string message) => Show(message, ToastTone.Warn);

    public void Dismiss(Guid id)
    {
        if (_items.RemoveAll(t => t.Id == id) > 0)
        {
            Changed?.Invoke();
        }
    }
}
