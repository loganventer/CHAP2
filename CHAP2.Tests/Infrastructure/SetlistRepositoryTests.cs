using CHAP2.Domain.Entities;
using CHAP2.Infrastructure.Identity;
using CHAP2.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CHAP2.Tests.Infrastructure;

[TestFixture]
public class SetlistRepositoryTests
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        using var ctx = NewContext();
        ctx.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown()
    {
        _connection.Dispose();
    }

    [Test]
    public async Task Add_Get_RoundTripsSetlistWithItems()
    {
        await SeedUserAsync("user-1");
        var setlist = Setlist.Create("user-1", "Sunday");
        setlist.AppendChorus(Guid.NewGuid());
        setlist.AppendChorus(Guid.NewGuid());

        await using (var addCtx = NewContext())
        {
            await new SetlistRepository(addCtx).AddAsync(setlist);
        }

        await using var readCtx = NewContext();
        var loaded = await new SetlistRepository(readCtx).GetByIdAsync(setlist.Id);
        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("Sunday");
        loaded.Items.Should().HaveCount(2);
        loaded.Items.Select(i => i.Position).Should().Equal(0, 1);
    }

    [Test]
    public async Task GetByOwner_ReturnsOnlyOwnerSetlists()
    {
        await SeedUserAsync("a");
        await SeedUserAsync("b");
        await using (var ctx = NewContext())
        {
            var repo = new SetlistRepository(ctx);
            await repo.AddAsync(Setlist.Create("a", "A1"));
            await repo.AddAsync(Setlist.Create("a", "A2"));
            await repo.AddAsync(Setlist.Create("b", "B1"));
        }

        await using var readCtx = NewContext();
        var aSets = await new SetlistRepository(readCtx).GetByOwnerAsync("a");

        aSets.Select(s => s.Name).Should().BeEquivalentTo(new[] { "A1", "A2" });
    }

    [Test]
    public async Task Update_PersistsRenameAndItemMutations()
    {
        await SeedUserAsync("user-1");
        var seed = Setlist.Create("user-1", "Original");
        seed.AppendChorus(Guid.NewGuid());
        await using (var ctx = NewContext()) await new SetlistRepository(ctx).AddAsync(seed);

        await using (var ctx = NewContext())
        {
            var repo = new SetlistRepository(ctx);
            var loaded = await repo.GetByIdAsync(seed.Id);
            loaded!.Rename("Renamed");
            loaded.AppendChorus(Guid.NewGuid());
            await repo.UpdateAsync(loaded);
        }

        await using var readCtx = NewContext();
        var reloaded = await new SetlistRepository(readCtx).GetByIdAsync(seed.Id);
        reloaded!.Name.Should().Be("Renamed");
        reloaded.Items.Should().HaveCount(2);
    }

    [Test]
    public async Task Delete_RemovesSetlistAndCascadesItems()
    {
        await SeedUserAsync("user-1");
        var seed = Setlist.Create("user-1", "Doomed");
        seed.AppendChorus(Guid.NewGuid());
        await using (var ctx = NewContext()) await new SetlistRepository(ctx).AddAsync(seed);

        await using (var ctx = NewContext()) await new SetlistRepository(ctx).DeleteAsync(seed.Id);

        await using var verifyCtx = NewContext();
        (await new SetlistRepository(verifyCtx).GetByIdAsync(seed.Id)).Should().BeNull();
        (await verifyCtx.SetlistItems.CountAsync()).Should().Be(0);
    }

    private ApplicationDbContext NewContext() => new(_options);

    private async Task SeedUserAsync(string id)
    {
        await using var ctx = NewContext();
        ctx.Users.Add(new ApplicationUser
        {
            Id = id,
            UserName = id,
            NormalizedUserName = id.ToUpperInvariant(),
            Email = $"{id}@example.com",
            NormalizedEmail = $"{id}@example.com".ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString(),
        });
        await ctx.SaveChangesAsync();
    }
}
