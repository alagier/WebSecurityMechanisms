namespace WebSecurityMechanisms.Models;

public class Preset
{
    public Preset(string key)
    {
        Key = key;
    }

    public string? Name { get; set; }

    public string Key { get; }

    public override bool Equals(object? o)
    {
        if (o == null) throw new ArgumentNullException(nameof(o));
        var other = o as Preset;
        return other?.Key == Key;
    }

    public override int GetHashCode() => Key.GetHashCode();
}