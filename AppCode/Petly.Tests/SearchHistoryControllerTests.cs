using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Petly.Business.Services;
using Petly.Controllers;
using Petly.DataAccess.Data;
using Petly.Models;
using Xunit;

namespace Petly.Tests;

public class SearchHistoryControllerTests
{
    [Fact]
    public async Task RecordSearch_ShouldAddToHistory()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        var user = await CreateUserAsync(scope.UserManager, scope.RoleManager, "user@test.com", "pass123", "user");

        var controller = CreateApiController(scope, "user", user.Id);

        var result = await controller.RecordSearch(new SearchQueryDto { Query = "собака" });

        Assert.IsType<OkResult>(result);

        var history = await db.SearchHistories.FirstOrDefaultAsync();
        Assert.NotNull(history);
        Assert.Equal(user.Id, history.UserId);
        Assert.Equal("собака", history.SearchQuery);
    }

    [Fact]
    public async Task RecordSearch_EmptyQuery_ShouldReturnBadRequest()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        var user = await CreateUserAsync(scope.UserManager, scope.RoleManager, "user@test.com", "pass123", "user");

        var controller = CreateApiController(scope, "user", user.Id);

        var result = await controller.RecordSearch(new SearchQueryDto { Query = "" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetSearchHistory_ShouldReturnOrderedByDate()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        var user = await CreateUserAsync(scope.UserManager, scope.RoleManager, "user@test.com", "pass123", "user");

        db.SearchHistories.Add(new SearchHistory { UserId = user.Id, SearchQuery = "собака", CreatedAt = DateTime.UtcNow.AddMinutes(-10) });
        db.SearchHistories.Add(new SearchHistory { UserId = user.Id, SearchQuery = "кіт", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var controller = CreateApiController(scope, "user", user.Id);

        var result = await controller.GetSearchHistory(null);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var history = Assert.IsType<List<SearchHistory>>(okResult.Value);

        Assert.Equal(2, history.Count);
        Assert.Equal("кіт", history.First().SearchQuery);
    }

    [Fact]
    public async Task GetSearchHistory_WithLimit_ShouldReturnLimitedResults()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        var user = await CreateUserAsync(scope.UserManager, scope.RoleManager, "user@test.com", "pass123", "user");

        db.SearchHistories.Add(new SearchHistory { UserId = user.Id, SearchQuery = "собака", CreatedAt = DateTime.UtcNow.AddMinutes(-10) });
        db.SearchHistories.Add(new SearchHistory { UserId = user.Id, SearchQuery = "кіт", CreatedAt = DateTime.UtcNow.AddMinutes(-5) });
        db.SearchHistories.Add(new SearchHistory { UserId = user.Id, SearchQuery = "папуга", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var controller = CreateApiController(scope, "user", user.Id);

        var result = await controller.GetSearchHistory(2);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var history = Assert.IsType<List<SearchHistory>>(okResult.Value);

        Assert.Equal(2, history.Count);
    }

    [Fact]
    public async Task GetSearchHistory_ShouldOnlyReturnUserHistory()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        var user1 = await CreateUserAsync(scope.UserManager, scope.RoleManager, "user1@test.com", "pass123", "user");
        var user2 = await CreateUserAsync(scope.UserManager, scope.RoleManager, "user2@test.com", "pass123", "user");

        db.SearchHistories.Add(new SearchHistory { UserId = user1.Id, SearchQuery = "собака" });
        db.SearchHistories.Add(new SearchHistory { UserId = user2.Id, SearchQuery = "кіт" });
        await db.SaveChangesAsync();

        var controller = CreateApiController(scope, "user", user1.Id);

        var result = await controller.GetSearchHistory(null);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var history = Assert.IsType<List<SearchHistory>>(okResult.Value);

        Assert.Single(history);
        Assert.Equal("собака", history.First().SearchQuery);
    }

    [Fact]
    public async Task DeleteSearchHistory_ShouldRemoveRecord()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        var user = await CreateUserAsync(scope.UserManager, scope.RoleManager, "user@test.com", "pass123", "user");

        var searchHistory = new SearchHistory { UserId = user.Id, SearchQuery = "собака" };
        db.SearchHistories.Add(searchHistory);
        await db.SaveChangesAsync();

        var controller = CreateApiController(scope, "user", user.Id);

        var result = await controller.DeleteSearchHistory(searchHistory.Id);

        Assert.IsType<OkResult>(result);

        var history = await db.SearchHistories.FirstOrDefaultAsync();
        Assert.Null(history);
    }

    [Fact]
    public async Task DeleteSearchHistory_WrongUser_ShouldNotDelete()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        var user1 = await CreateUserAsync(scope.UserManager, scope.RoleManager, "user1@test.com", "pass123", "user");
        var user2 = await CreateUserAsync(scope.UserManager, scope.RoleManager, "user2@test.com", "pass123", "user");

        var searchHistory = new SearchHistory { UserId = user1.Id, SearchQuery = "собака" };
        db.SearchHistories.Add(searchHistory);
        await db.SaveChangesAsync();

        var controller = CreateApiController(scope, "user", user2.Id);

        var result = await controller.DeleteSearchHistory(searchHistory.Id);

        Assert.IsType<OkResult>(result);

        var history = await db.SearchHistories.FirstOrDefaultAsync();
        Assert.NotNull(history);
    }

    [Fact]
    public async Task ClearSearchHistory_ShouldRemoveAllUserRecords()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        var user1 = await CreateUserAsync(scope.UserManager, scope.RoleManager, "user1@test.com", "pass123", "user");
        var user2 = await CreateUserAsync(scope.UserManager, scope.RoleManager, "user2@test.com", "pass123", "user");

        db.SearchHistories.Add(new SearchHistory { UserId = user1.Id, SearchQuery = "собака" });
        db.SearchHistories.Add(new SearchHistory { UserId = user1.Id, SearchQuery = "кіт" });
        db.SearchHistories.Add(new SearchHistory { UserId = user2.Id, SearchQuery = "папуга" });
        await db.SaveChangesAsync();

        var controller = CreateApiController(scope, "user", user1.Id);

        var result = await controller.ClearSearchHistory();

        Assert.IsType<OkResult>(result);

        var user1History = await db.SearchHistories.Where(sh => sh.UserId == user1.Id).ToListAsync();
        var user2History = await db.SearchHistories.Where(sh => sh.UserId == user2.Id).ToListAsync();

        Assert.Empty(user1History);
        Assert.Single(user2History);
    }

    [Fact]
    public void Controller_RequiresAuthorization()
    {
        var attr = typeof(SearchHistoryController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attr);
    }

    [Fact]
    public void ApiController_RequiresAuthorization()
    {
        var attr = typeof(SearchHistoryApiController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attr);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static TestIdentityScope CreateIdentityScope(ApplicationDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton<IOptions<IdentityOptions>>(Options.Create(new IdentityOptions()));
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
        services.AddSingleton<ILookupNormalizer, UpperInvariantLookupNormalizer>();
        services.AddSingleton<IdentityErrorDescriber>();
        services.AddSingleton<IUserStore<ApplicationUser>, UserStore<ApplicationUser, IdentityRole<int>, ApplicationDbContext, int>>();
        services.AddSingleton<IRoleStore<IdentityRole<int>>, RoleStore<IdentityRole<int>, ApplicationDbContext, int>>();
        services.AddSingleton<ILogger<UserManager<ApplicationUser>>>(NullLogger<UserManager<ApplicationUser>>.Instance);
        services.AddSingleton<ILogger<RoleManager<IdentityRole<int>>>>(NullLogger<RoleManager<IdentityRole<int>>>.Instance);
        services.AddSingleton<UserManager<ApplicationUser>>();
        services.AddSingleton<RoleManager<IdentityRole<int>>>();
        services.AddSingleton<SearchHistoryService>();
        services.AddTransient<SearchHistoryApiController>();

        var serviceProvider = services.BuildServiceProvider();
        return new TestIdentityScope(
            serviceProvider,
            serviceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
            serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>());
    }

    private static SearchHistoryApiController CreateApiController(TestIdentityScope scope, string? role = null, int? userId = null)
    {
        var controller = scope.ServiceProvider.GetRequiredService<SearchHistoryApiController>();
        var httpContext = new DefaultHttpContext();

        if (userId.HasValue)
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.Value.ToString()) };
            if (!string.IsNullOrEmpty(role)) claims.Add(new Claim(ClaimTypes.Role, role));
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    private static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<int>> roleManager, string email, string password, string role)
    {
        if (!await roleManager.RoleExistsAsync(role)) await roleManager.CreateAsync(new IdentityRole<int>(role));
        var user = new ApplicationUser { UserName = email, Email = email, RegistrationDate = DateTime.UtcNow, Status = "Активний" };
        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    private sealed record TestIdentityScope(ServiceProvider ServiceProvider, UserManager<ApplicationUser> UserManager, RoleManager<IdentityRole<int>> RoleManager);
}
