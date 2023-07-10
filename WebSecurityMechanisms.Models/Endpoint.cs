namespace WebSecurityMechanisms.Models;

public class Endpoint
{
    public string Path { get; set; }
    
    public override bool Equals(object o)
    {
        var other = o as Endpoint;
        return other?.Path == Path;
    }

    public override int GetHashCode() => Path.GetHashCode();
}