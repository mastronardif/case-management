public class MyMarker
{
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }

    public void Start()
    {
        StartTime = DateTime.Now;
        Console.WriteLine($"[Marker] Start: {StartTime:HH:mm:ss.fff}");
    }

    public void End()
    {
        EndTime = DateTime.Now;
        Console.WriteLine($"[Marker] End: {EndTime:HH:mm:ss.fff}");
        var duration = EndTime - StartTime;
        Console.WriteLine($"[Marker] Duration: {duration.TotalMilliseconds} ms");
    }

    public string Report()
    {
        var duration = EndTime - StartTime;
        return $"Start: {StartTime:HH:mm:ss.fff}, End: {EndTime:HH:mm:ss.fff}, Duration: {duration.TotalMilliseconds} ms";
    }
}
