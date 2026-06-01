using Microsoft.EntityFrameworkCore;
using Petly.DataAccess.Data;
using Petly.Models;

namespace Petly.Business.Services;

public class SearchHistoryService
{
    private readonly ApplicationDbContext _context;

    public SearchHistoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddSearchHistoryAsync(int userId, string searchQuery)
    {
        var searchHistory = new SearchHistory
        {
            UserId = userId,
            SearchQuery = searchQuery,
            CreatedAt = DateTime.UtcNow
        };

        _context.SearchHistories.Add(searchHistory);
        await _context.SaveChangesAsync();
    }

    public async Task<List<SearchHistory>> GetUserSearchHistoryAsync(int userId)
    {
        return await _context.SearchHistories
            .Where(sh => sh.UserId == userId)
            .OrderByDescending(sh => sh.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<SearchHistory>> GetUserSearchHistoryAsync(int userId, int limit)
    {
        return await _context.SearchHistories
            .Where(sh => sh.UserId == userId)
            .OrderByDescending(sh => sh.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task ClearUserSearchHistoryAsync(int userId)
    {
        var userHistory = await _context.SearchHistories
            .Where(sh => sh.UserId == userId)
            .ToListAsync();

        _context.SearchHistories.RemoveRange(userHistory);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteSearchHistoryAsync(int searchHistoryId, int userId)
    {
        var searchHistory = await _context.SearchHistories
            .FirstOrDefaultAsync(sh => sh.Id == searchHistoryId && sh.UserId == userId);

        if (searchHistory != null)
        {
            _context.SearchHistories.Remove(searchHistory);
            await _context.SaveChangesAsync();
        }
    }
}
