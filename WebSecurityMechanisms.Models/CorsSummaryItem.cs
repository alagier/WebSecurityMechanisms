namespace WebSecurityMechanisms.Models;

public class CorsSummaryItem
{
    public string Name { get; set; }

    public string? Requested { get; set; }

    public string? Received { get; set; }

    public bool isValid { get; set; }
}