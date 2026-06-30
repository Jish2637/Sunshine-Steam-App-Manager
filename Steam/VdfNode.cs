namespace SunshineSteamAppManager.Steam;

public sealed class VdfNode
{
    private readonly Dictionary<string, VdfValue> _values = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, VdfValue> Values => _values;

    public void Set(string key, VdfValue value)
    {
        _values[key] = value;
    }

    public string? GetString(string key)
    {
        return _values.TryGetValue(key, out var value) ? value.StringValue : null;
    }

    public VdfNode? GetObject(string key)
    {
        return _values.TryGetValue(key, out var value) ? value.ObjectValue : null;
    }
}

public sealed class VdfValue
{
    private VdfValue(string? stringValue, VdfNode? objectValue)
    {
        StringValue = stringValue;
        ObjectValue = objectValue;
    }

    public string? StringValue { get; }
    public VdfNode? ObjectValue { get; }
    public bool IsObject => ObjectValue is not null;

    public static VdfValue FromString(string value) => new(value, null);
    public static VdfValue FromObject(VdfNode value) => new(null, value);
}
