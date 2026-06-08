public class Job
{
    public int Id {get; set;}
    public string Status {get; set;} = "Pending";
    public string? LockedBy {get; set;}
    public DateTime? LockedAt {get; set;}
    public int Attempts {get; set;} = 0;
    public string? LastError {get; set;}
    public string Type {get; set;} = string.Empty;
    public string Payload {get; set;} = string.Empty;
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
    public DateTime? CompletedAt {get; set;}
}