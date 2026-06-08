using System.Diagnostics;
using System.Text.Json;

public class HttpPollerExecutor : IJobExecutor
{
    private readonly List<string> _urls = new List<string>
    {
        "https://httpbin.org/get",
        "https://httpbin.org/delay/2",
        "https://jsonplaceholder.typicode.com/posts",
        "https://reqres.in/api/users",
        "https://api.github.com",
        "https://catfact.ninja/fact",
        "https://dog.ceo/api/breeds/image/random",
        "https://api.coindesk.com/v1/bpi/currentprice.json",
        "https://www.google.com",
        "https://www.cloudflare.com",
        "https://one.one.one.one"
    };
    private const int _timeoutSeconds = 10;
    public async Task RunAsync(string jobId, string payload, CancellationToken ct)
    {
        var parms = JsonSerializer.Deserialize<HttpPollerParams>(payload)!;

        var dir = Path.Combine(AppContext.BaseDirectory, jobId.ToString());
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, parms.FileToSave + ".txt");

        using var http = new HttpClient();
        http.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);

        var tasks = _urls.Select(async url =>
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var response = await http.GetAsync(url, ct);
                stopwatch.Stop();
                return $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {url} — {(int)response.StatusCode} {response.StatusCode} — {stopwatch.ElapsedMilliseconds}ms";
            }
            catch (TaskCanceledException)
            {
                return $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {url} — TIMEOUT after {_timeoutSeconds}s";
            }
            catch (Exception ex)
            {
                return $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {url} — ERROR {ex.Message}";
            }
        });

        var results = await Task.WhenAll(tasks);

        await File.AppendAllLinesAsync(path, results, ct);
    }
}