using System.Security.Cryptography;
using System.Text.Json;

public class FileHasherExecutor : IJobExecutor
{
    public async Task RunAsync(string jobId, string payload, CancellationToken ct)
    {
        var p = JsonSerializer.Deserialize<FileHasherParams>(payload)!;

        var workingDir = Path.Combine(AppContext.BaseDirectory, p.DirectoryPath);
        Directory.CreateDirectory(workingDir);

        var outputDir = Path.Combine(AppContext.BaseDirectory, jobId.ToString());
        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, p.FileToSave + ".txt");

        var createdFiles = new List<string>();
        var random = new Random();

        for (int i = 0; i < 50; i++)
        {
            ct.ThrowIfCancellationRequested();

            var filePath = Path.Combine(workingDir, $"file-{i}.txt");
            var lines = Enumerable.Range(0, random.Next(10, 100))
                .Select(_ => Guid.NewGuid().ToString());

            await File.WriteAllLinesAsync(filePath, lines, ct);
            createdFiles.Add(filePath);
        }

        var tasks = createdFiles.Select(async file =>
        {
            ct.ThrowIfCancellationRequested();

            return await Task.Run(() =>
            {
                using var h = p.Algorithm switch
                {
                    "SHA256" => (HashAlgorithm)SHA256.Create(),
                    "SHA512" => SHA512.Create(),
                    "MD5"    => MD5.Create(),
                    _ => throw new InvalidOperationException($"Unknown algorithm: {p.Algorithm}")
                };

                using var stream = File.OpenRead(file);
                var hash = h.ComputeHash(stream);
                var hex = Convert.ToHexString(hash);
                return $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {hex}  {Path.GetFileName(file)}";
            }, ct);
        });

        var results = await Task.WhenAll(tasks);

        foreach (var file in createdFiles)
        {
            File.Delete(file);
        }

        await File.AppendAllLinesAsync(outputPath, results, ct);
    }
}