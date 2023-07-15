namespace WebSecurityMechanisms.Models;

public class Endpoint
{
    public Endpoint(string path)
    {
        Path = path;
    }
    
    public string Path { get; }
    
    public override bool Equals(object? o)
    {
        var other = o as Endpoint;
        return other?.Path == Path;
    }

    public override int GetHashCode() => Path.GetHashCode();
}