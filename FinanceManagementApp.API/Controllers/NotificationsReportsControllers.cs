using FinanceManagementApp.API.Data;
using FinanceManagementApp.API.DTOs;
using FinanceManagementApp.API.Middleware;
using FinanceManagementApp.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceManagementApp.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public NotificationsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var userId = User.GetUserId();
        var query = _db.Notifications.Where(n => n.UserId == userId);
        var unread = await query.CountAsync(n => !n.IsRead);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id, Title = n.Title, Message = n.Message, IsRead = n.IsRead, CreatedAt = n.CreatedAt
            }).ToListAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            unreadCount = unread,
            items,
            page,
            pageSize,
            totalCount = total,
            totalPages = (int)Math.Ceiling(total / (double)pageSize)
        }));
    }

    [HttpPut("{id:int}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkRead(int id)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == User.GetUserId());
        if (n is null) return NotFound(ApiResponse<object>.Fail("Not found"));
        n.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { }, "Marked as read"));
    }

    [HttpPut("read-all")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllRead()
    {
        var items = await _db.Notifications.Where(n => n.UserId == User.GetUserId() && !n.IsRead).ToListAsync();
        foreach (var n in items) n.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { }, "All notifications marked as read"));
    }
}

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "ADMIN")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    [HttpGet("monthly")]
    public async Task<ActionResult> Monthly([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? format)
    {
        var report = await _reports.MonthlyAsync(from, to);
        return await ExportOrJson(report, format);
    }

    [HttpGet("employee")]
    public async Task<ActionResult> Employee([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? employeeId, [FromQuery] string? format)
    {
        var report = await _reports.EmployeeAsync(from, to, employeeId);
        return await ExportOrJson(report, format);
    }

    [HttpGet("department")]
    public async Task<ActionResult> Department([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? departmentId, [FromQuery] string? format)
    {
        var report = await _reports.DepartmentAsync(from, to, departmentId);
        return await ExportOrJson(report, format);
    }

    [HttpGet("category")]
    public async Task<ActionResult> Category([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? categoryId, [FromQuery] string? status, [FromQuery] string? format)
    {
        var report = await _reports.CategoryAsync(from, to, categoryId, status);
        return await ExportOrJson(report, format);
    }

    private async Task<ActionResult> ExportOrJson(ReportDto report, string? format)
    {
        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = await _reports.ExportCsvAsync(report);
            return File(bytes, "text/csv", $"{report.Title.Replace(' ', '_')}.csv");
        }
        if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = await _reports.ExportPdfAsync(report);
            return File(bytes, "application/pdf", $"{report.Title.Replace(' ', '_')}.pdf");
        }
        return Ok(ApiResponse<ReportDto>.Ok(report));
    }
}

[ApiController]
[Route("api/auditlogs")]
[Authorize(Roles = "ADMIN")]
public class AuditLogsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuditLogsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogDto>>>> Get(
        [FromQuery] string? action, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.AuditLogs.Include(a => a.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action.Contains(action));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserEmail = a.User != null ? a.User.Email : null,
                Action = a.Action,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                Description = a.Description,
                CreatedAt = a.CreatedAt
            }).ToListAsync();

        return Ok(ApiResponse<PagedResult<AuditLogDto>>.Ok(new PagedResult<AuditLogDto>
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        }));
    }
}

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public ProfileController(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<ProfileDto>>> Get()
    {
        var user = await _db.Users.Include(u => u.Role).Include(u => u.Employee).ThenInclude(e => e!.Department)
            .FirstOrDefaultAsync(u => u.Id == User.GetUserId());
        if (user is null) return NotFound(ApiResponse<ProfileDto>.Fail("User not found"));

        return Ok(ApiResponse<ProfileDto>.Ok(new ProfileDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role.Name,
            EmployeeId = user.EmployeeId,
            FirstName = user.Employee?.FirstName,
            LastName = user.Employee?.LastName,
            Phone = user.Employee?.Phone,
            DepartmentName = user.Employee?.Department?.Name,
            Designation = user.Employee?.Designation
        }));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<ProfileDto>>> Update([FromBody] UpdateProfileDto dto)
    {
        var user = await _db.Users.Include(u => u.Role).Include(u => u.Employee).ThenInclude(e => e!.Department)
            .FirstOrDefaultAsync(u => u.Id == User.GetUserId());
        if (user is null) return NotFound(ApiResponse<ProfileDto>.Fail("User not found"));

        if (user.Employee is not null)
        {
            user.Employee.FirstName = dto.FirstName.Trim();
            user.Employee.LastName = dto.LastName.Trim();
            user.Employee.Phone = dto.Phone;
            user.Employee.UpdatedAt = DateTime.UtcNow;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(user.Id, "PROFILE_UPDATED", "User", user.Id.ToString(), "Profile updated");

        return await Get();
    }
}
