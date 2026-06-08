public class JobExecutorRouter
{
    private readonly Dictionary<string, IJobExecutor> _executors = new()
    {
      ["FileHasher"] = new FileHasherExecutor(),
      ["HttpPoller"] = new HttpPollerExecutor(),
      ["PrimeSieve"] = new PrimeSieveExecutor()
    };

    public IJobExecutor Resolve(string jobType) => _executors.TryGetValue(jobType, out var executor) ? executor : throw new InvalidOperationException($"Unkown job type: {jobType}");
}