using Microsoft.EntityFrameworkCore;

public static class JobsEndpoints
{
    public static void MapJobEndpoints(this WebApplication app)
    {
        app.MapPost("/jobs", async(CreateJobRequest req, AppDbContext db) => {
            Job job = new Job
            {
              Status = "Pending",
              Type = req.Type,
              Payload = req.Payload  
            };

            db.Jobs.Add(job);
            await db.SaveChangesAsync();
            return Results.Ok(new {job.Id});
        });

        app.MapGet("/jobs/{id}", async (int id, AppDbContext db) =>
        {
            Job? job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id);
            if(job == null) return Results.NotFound();
            return Results.Ok(job);
        });

        app.MapGet("/jobs", async (AppDbContext db) =>
        {
            List<Job> jobs = await db.Jobs.ToListAsync();
            return Results.Ok(jobs);
        });

        app.MapDelete("/jobs/{id}", async (int id, AppDbContext db) =>
        {
            int rowsAffected = await db.Database.ExecuteSqlRawAsync("""
                UPDATE JOBS
                SET Status = 'Cancelled'
                WHERE Id = {0} AND STATUS = 'Pending'
            """, id);

            return rowsAffected == 1 ? Results.Ok() : Results.BadRequest("Job could not be cancelled");
        });
    }
}