// See https://aka.ms/new-console-template for more information
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = factory.CreateLogger("Program");

Console.WriteLine("Hello, World!");

await using var context = new JobContext();
await context.Database.EnsureDeletedAsync();
await context.Database.EnsureCreatedAsync();

context.Jobs.Add(new() { Id = Guid.NewGuid() });
context.Jobs.Add(new()
{
  Id = Guid.NewGuid(),
  Error = new()
  {
    Code = "500",
    Message = "Internal Server Error",
    InnerError = new()
    {
      Code = "501",
      Message = "Not Implemented"
    }
  }
});
context.Jobs.Add(new()
{
  Id = Guid.NewGuid(),
  Error = new()
  {
    Code = "400",
    Message = "Bad Request"
  }
});
await context.SaveChangesAsync();

context.ChangeTracker.Clear();

var jobs = await context.Jobs.ToListAsync();
foreach (var job in jobs)
{
  logger.LogInformation("Processing Job: {JobId}", job.Id);
  if (job.Error is not null)
  {
    logger.LogWarning("Job {JobId} has an error", job.Id);
    logger.LogWarning("  Error Code: {ErrorCode}", job.Error.Code);
    logger.LogWarning("  Error Message: {ErrorMessage}", job.Error.Message);
    if (job.Error.InnerError is not null)
    {
      logger.LogWarning("    Inner Error Code: {InnerErrorCode}", job.Error.InnerError.Code);
      logger.LogWarning("    Inner Error Message: {InnerErrorMessage}", job.Error.InnerError.Message);
    }
  }
  try
  {
    var original = context.Entry(job).OriginalValues.ToObject() as Job;
    logger.LogInformation("Retrieve Original Values for Job: {JobId}", job.Id);
  }
  catch (Exception ex)
  {
    logger.LogError(ex, "Error retrieving original values for Job: {JobId}", job.Id);
  }
}

Console.WriteLine("Completed");
Console.WriteLine("Press any key to exit...");
Console.ReadKey();

public class JobContext : DbContext
{ 
  public DbSet<Job> Jobs { get; set; }

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      => optionsBuilder
          .UseSqlServer("Data Source=.;Initial Catalog=ComplextTest;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=true;")
          .LogTo(Console.WriteLine, LogLevel.Information)
          .EnableSensitiveDataLogging();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
      => modelBuilder.Entity<Job>().ComplexProperty(x => x.Error, x => x.ToJson());
}

public class Job
{
  public Guid Id { get; set; }
  public Error? Error { get; set; }
}

public class Error
{
  public required string Code { get; set; }
  public required string Message { get; set; }
  public Error? InnerError { get; set; }

}