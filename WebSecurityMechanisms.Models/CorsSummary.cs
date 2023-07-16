namespace WebSecurityMechanisms.Models;

public class CorsSummary
{
    public CorsSummary()
    {
        Origin = new () { Name = "Origin" };
        Method = new() { Name = "Method" };
        Headers = new() { Name = "Headers" };
    }
    
    public bool IsPreflight { get; set; }

    public CorsSummaryItem Origin { get; set; }
    
    public CorsSummaryItem Method { get; set; }
    
    public CorsSummaryItem Headers { get; set; }
}