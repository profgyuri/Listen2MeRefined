namespace Listen2MeRefined.Infrastructure.Media;

public sealed class PlaylistStore : IPlaylistStore
{
    private readonly object _gate = new();
    private readonly List<AudioModel> _items = new();

    public event EventHandler? Changed;

    public IReadOnlyList<AudioModel> Snapshot()
    {
        lock (_gate)
            return _items.ToArray();
    }

    public void ReplaceAll(IEnumerable<AudioModel> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        lock (_gate)
        {
            _items.Clear();
            _items.AddRange(items);
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void AddRange(IEnumerable<AudioModel> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        lock (_gate)
        {
            _items.AddRange(items);
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool RemoveByPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        bool removed;
        lock (_gate)
        {
            removed = _items.RemoveAll(x =>
                x.Path is not null &&
                StringComparer.OrdinalIgnoreCase.Equals(x.Path, path)) > 0;
        }

        if (removed) Changed?.Invoke(this, EventArgs.Empty);
        return removed;
    }

    public bool MoveByPath(string path, int newIndex)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        lock (_gate)
        {
            var idx = _items.FindIndex(x =>
                x.Path is not null && StringComparer.OrdinalIgnoreCase.Equals(x.Path, path));

            if (idx < 0) return false;

            newIndex = Math.Clamp(newIndex, 0, _items.Count - 1);
            if (idx == newIndex) return true;

            var item = _items[idx];
            _items.RemoveAt(idx);
            _items.Insert(newIndex, item);
        }

        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void Clear()
    {
        lock (_gate)
        {
            if (_items.Count == 0) return;
            _items.Clear();
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Shuffle(bool keepFirstByPath = false, string? firstPath = null)
    {
        lock (_gate)
        {
            if (_items.Count <= 1) return;

            AudioModel? keepFirst = null;

            if (keepFirstByPath && !string.IsNullOrWhiteSpace(firstPath))
            {
                var keepIndex = _items.FindIndex(x =>
                    x.Path is not null && StringComparer.OrdinalIgnoreCase.Equals(x.Path, firstPath));
                if (keepIndex >= 0)
                    keepFirst = _items[keepIndex];
            }

            // Fisher–Yates
            var rng = Random.Shared;
            for (var i = _items.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (_items[i], _items[j]) = (_items[j], _items[i]);
            }

            if (keepFirst is not null)
            {
                // move kept item to front
                _items.Remove(keepFirst);
                _items.Insert(0, keepFirst);
            }
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }
}