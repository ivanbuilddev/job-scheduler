public interface IJobExecutor
{
    public Task RunAsync(string payload, CancellationToken ct);
}