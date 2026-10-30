namespace IMS.Blazor.Services;

public enum ToastKind
{
    Success,
    Error,
    Info
}

public record ToastMessage(Guid Id, string Text, ToastKind Kind);

/// <summary>Small pop-up messages (top right). Any page can inject it and call Success / Error / Info.</summary>
public class ToastService
{
    private readonly List<ToastMessage> _messages = new();

    public IReadOnlyList<ToastMessage> Messages => _messages;

    public event Action? Changed;

    public void Success(string text) => Add(text, ToastKind.Success);

    public void Error(string text) => Add(text, ToastKind.Error);

    public void Info(string text) => Add(text, ToastKind.Info);

    public void Dismiss(ToastMessage message)
    {
        if (_messages.Remove(message))
        {
            Changed?.Invoke();
        }
    }

    private void Add(string text, ToastKind kind)
    {
        var message = new ToastMessage(Guid.NewGuid(), text, kind);

        _messages.Add(message);
        Changed?.Invoke();

        _ = DismissLaterAsync(message);
    }

    private async Task DismissLaterAsync(ToastMessage message)
    {
        await Task.Delay(4000);
        Dismiss(message);
    }
}
