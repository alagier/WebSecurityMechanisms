namespace WebSecurityMechanisms.Models;

public class Preset
{
    public string Name { get; set; }

    public string Key { get; set; }

    public override bool Equals(object o)
    {
        var other = o as Preset;
        return other?.Key == Key;
    }

    public override int GetHashCode() => Key.GetHashCode();
}