using Microsoft.EntityFrameworkCore;

public class JobWorkerManager : IHostedService
{
    private readonly int _workerCount;
    private readonly int _pollIntervalSeconds;
    private readonly int _maxTryCount;
    private readonly int _maxMinutesBeforeRetry;
    private List<Task> _workers = new List<Task>();
    private CancellationTokenSource _cts = new();
    private IServiceProvider _serviceProvider;
    private readonly Dictionary<int, CancellationTokenSource> _runningJobs = new();


    public JobWorkerManager(IConfiguration config, IServiceProvider serviceProvider)
    {
        _workerCount = config.GetValue<int>("Jobs:WorkerCount");
        _pollIntervalSeconds = config.GetValue<int>("Jobs:PollIntervalSeconds");
        _maxTryCount = config.GetValue<int>("Jobs:MaxTryCount");
        _maxMinutesBeforeRetry = config.GetValue<int>("Jobs:MaxMinutesBeforeRetry");
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken ct)
    {
        for(int i = 0; i < _workerCount; i++)
        {
            string workerId = "worker" + i;
            _workers.Add(RunWorkerLoop(workerId, _cts.Token));
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _cts.Cancel();
        await Task.WhenAll(_workers);
    }

    private async Task RunWorkerLoop(string workerId, CancellationToken ct)
    {
        while(!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var router = scope.ServiceProvider.GetRequiredService<JobExecutorRouter>();

                await ResetStuckJobs(db);
                await ClaimAndExecute(db, router, workerId, ct);

                await Task.Delay(TimeSpan.FromSeconds(_pollIntervalSeconds), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                
            }
        }
    }

    private async Task ResetStuckJobs(AppDbContext db)
    {
        using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.RepeatableRead
        );

        try{
            var stuckJobs = await db.Jobs
                .Where(j => j.Status == "Running" && j.LockedAt < DateTime.UtcNow.AddMinutes(_maxMinutesBeforeRetry * -1))
                .ToListAsync();

            foreach (var job in stuckJobs)
            {
                if (_runningJobs.TryGetValue(job.Id, out var jobCts))
                {
                    jobCts.Cancel();
                }

                if (job.Attempts < _maxTryCount)
                {
                    job.Status = "Pending";
                    job.LockedBy = null;
                    job.LockedAt = null;
                    job.Attempts++;
                }
                else
                {
                    job.Status = "Failed";
                    job.LastError = "Max retry attempts reached";
                }
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task ClaimAndExecute(AppDbContext db, JobExecutorRouter router, string workerId, CancellationToken ct)
    {
        var rowsAffected = await db.Database.ExecuteSqlRawAsync("""
            UPDATE Jobs
            SET Status = 'Running', LockedBy = {0}, LockedAt = {1}
            WHERE Id = (
                SELECT TOP 1 Id FROM Jobs
                WHERE Status = 'Pending'
                ORDER BY CreatedAt ASC
            )
            """, workerId, DateTime.UtcNow);

        if (rowsAffected == 0) return;

        var job = await db.Jobs.FirstAsync(j => j.LockedBy == workerId && j.Status == "Running", ct);

        var jobCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _runningJobs[job.Id] = jobCts;

        try
        {
            var executor = router.Resolve(job.Type);
            await executor.RunAsync(job.Payload, jobCts.Token);

            job.Status = "Completed";
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            job.Status = "Failed";
            job.LastError = ex.Message;
            job.Attempts++;
        }
        finally
        {
            _runningJobs.Remove(job.Id);
            await db.SaveChangesAsync(ct);
        }
    }
}