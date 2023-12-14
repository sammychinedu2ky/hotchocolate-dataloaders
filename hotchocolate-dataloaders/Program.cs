using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ApplicationDbContext>((sp, opt) =>
{
    opt.UseSqlite("Data Source=app.db");
});

builder.Services
    .AddGraphQLServer()
    .RegisterDbContext<ApplicationDbContext>(DbContextKind.Resolver)
    .AddQueryType<Query>().AddProjections();

var app = builder.Build();


SeedDatabase(app);

app.MapGraphQL();
app.Run();



static void SeedDatabase(IApplicationBuilder app)
{
    using (var scope = app.ApplicationServices.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        context.Database.EnsureCreated();

        if (!context.Authors.Any())
        {
            var author1 = new Author { Name = "Author 1" };
            var author2 = new Author { Name = "Author 2" };
            context.Authors.AddRange(author1, author2);
            context.SaveChanges();
        }
    }
}

public class Query
{
    public async Task<Author?> GetAuthor(string Id, AuthorBatchDataLoader batchLoader)
    {
        return await batchLoader.LoadAsync(Id);
    }
    public async Task<Author?> GetAuthorWithoutBatchLoader(string Id, ApplicationDbContext context)
    {
        return await context.Authors.FindAsync(Id);
    }
    public async Task<List<Author>> GetAuthors(ApplicationDbContext context)
    {
        return await context.Authors.ToListAsync();
    }
}

public class Author
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required(ErrorMessage = "Name is required.")]
    public required string Name { get; set; }
}



public class ApplicationDbContext : DbContext
{
    public DbSet<Author> Authors { get; set; }
    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }
}

public class AuthorBatchDataLoader : BatchDataLoader<string, Author>
{
    private readonly ApplicationDbContext _context;

    public AuthorBatchDataLoader(
        ApplicationDbContext context,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        this._context = context;
    }

    protected override async Task<IReadOnlyDictionary<string, Author>> LoadBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        var authors = await _context.Authors.Where(a => keys.Contains(a.Id)).ToDictionaryAsync(a => a.Id, cancellationToken);
        return authors;
    }
}
