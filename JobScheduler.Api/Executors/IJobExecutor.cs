public interface IJobExecutor
{
    public Task RunAsync(string jobId, string payload, CancellationToken ct);
}