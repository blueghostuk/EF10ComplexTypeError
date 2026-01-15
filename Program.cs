// See https://aka.ms/new-console-template for more information
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = factory.CreateLogger("Program");

Console.WriteLine("Hello, World!");

await using var context = new JobContext();
await context.Database.EnsureDeletedAsync();
await context.Database.EnsureCreatedAsync();

context.Jobs.Add(new() { Id = Guid.NewGuid(), Name = "Job with No Error" });
context.Jobs.Add(new()
{
  Id = Guid.NewGuid(),
  Name = "Job with Error + Inner Error",
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
  Name = "Job with Error only",
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
  logger.LogInformation("Processing Job: {Job}", job);
  try
  {
    var original = context.Entry(job).OriginalValues.ToObject() as Job;
    logger.LogInformation("Retrieved Original Values for Job: {Job}", job);
  }
  catch (Exception ex)
  {
    logger.LogError(ex, "Error retrieving original values for Job: {Job}", job);
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
  public required String Name { get; set; }
  public Error? Error { get; set; }

  public override string ToString()
  {
    return $"Job(Id={Id}, Name={Name})";
  }
}

public class Error
{
  public required string Code { get; set; }
  public required string Message { get; set; }
  public Error? InnerError { get; set; }

}