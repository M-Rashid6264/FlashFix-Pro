namespace FlashFixPro.Core;

public sealed class OperationJournal : IOperationJournal
{
    private readonly List<string> _entries = [];

    public IReadOnlyList<string> Entries => _entries.AsReadOnly();

    public void Append(string message)
    {
        _entries.Add($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}  {message}");
    }
}
