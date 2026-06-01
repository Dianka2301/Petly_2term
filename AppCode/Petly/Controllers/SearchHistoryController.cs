using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Petly.Business.Services;

namespace Petly.Controllers;

[Authorize]
public class SearchHistoryController : Controller
{
    private readonly SearchHistoryService _searchHistoryService;

    public SearchHistoryController(SearchHistoryService searchHistoryService)
    {
        _searchHistoryService = searchHistoryService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        var history = await _searchHistoryService.GetUserSearchHistoryAsync(userId);
        return View(history);
    }

    [HttpPost]
    public async Task<IActionResult> Clear()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        await _searchHistoryService.ClearUserSearchHistoryAsync(userId);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        await _searchHistoryService.DeleteSearchHistoryAsync(id, userId);
        return RedirectToAction(nameof(Index));
    }
}

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SearchHistoryApiController : ControllerBase
{
    private readonly SearchHistoryService _searchHistoryService;

    public SearchHistoryApiController(SearchHistoryService searchHistoryService)
    {
        _searchHistoryService = searchHistoryService;
    }

    [HttpPost]
    public async Task<IActionResult> RecordSearch([FromBody] SearchQueryDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Query))
        {
            return BadRequest("Search query cannot be empty");
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        await _searchHistoryService.AddSearchHistoryAsync(userId, dto.Query);

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetSearchHistory([FromQuery] int? limit = null)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        var history = limit.HasValue
            ? await _searchHistoryService.GetUserSearchHistoryAsync(userId, limit.Value)
            : await _searchHistoryService.GetUserSearchHistoryAsync(userId);

        return Ok(history);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSearchHistory(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        await _searchHistoryService.DeleteSearchHistoryAsync(id, userId);

        return Ok();
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearSearchHistory()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        await _searchHistoryService.ClearUserSearchHistoryAsync(userId);

        return Ok();
    }
}

public class SearchQueryDto
{
    public string Query { get; set; }
}
