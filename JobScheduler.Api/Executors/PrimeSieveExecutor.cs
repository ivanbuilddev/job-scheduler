using System.Text.Json;

public class PrimeSieveExecutor : IJobExecutor
{
    public async Task RunAsync(string jobId, string payload, CancellationToken ct)
    {
        var parms = JsonSerializer.Deserialize<PrimeSieveParams>(payload);

        if(parms == null) return;


        var primes = await Task.Run(() => Sieve(parms.UpperBound, ct), ct);

        string dir = Path.Combine(AppContext.BaseDirectory, jobId.ToString());
        Directory.CreateDirectory(dir);

        string path = Path.Combine(dir, parms.FileToSave + ".txt");
        await File.AppendAllTextAsync(path, $"Found {primes.Count} primes up to {parms.UpperBound}\n", ct);
        await File.AppendAllLinesAsync(path, primes.Select(p => p.ToString()), ct);
    }

    private List<long> Sieve(long upperBound, CancellationToken ct)
    {
        var isPrime = new bool[upperBound + 1];
        Array.Fill(isPrime, true);
        isPrime[0] = false;
        isPrime[1] = false;

        for (long i = 2; i * i <= upperBound; i++)
        {
            ct.ThrowIfCancellationRequested();

            if (!isPrime[i]) continue;

            for (long j = i * i; j <= upperBound; j += i)
            {
                isPrime[j] = false;
            }
        }

        var primes = new List<long>();
        for (long i = 2; i <= upperBound; i++)
        {
            if (isPrime[i]) primes.Add(i);
        }

        return primes;
    }
}