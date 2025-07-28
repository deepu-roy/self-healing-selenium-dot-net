namespace framework.Types;

public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public DateTime? OldestEntry { get; set; }
    public DateTime? NewestEntry { get; set; }
}