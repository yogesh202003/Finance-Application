using FinanceManagementApp.API.Data;
using FinanceManagementApp.API.DTOs;
using FinanceManagementApp.API.Middleware;
using FinanceManagementApp.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceManagementApp.API.Controllers;

[ApiController]
[Route("api/expense-categories")]
[Authorize]
public class ExpenseCategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ExpenseCategoriesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ExpenseCategoryDto>>>> Get()
    {
        var items = await _db.ExpenseCategories.OrderBy(c => c.Name)
            .Select(c => new ExpenseCategoryDto
            {
                Id = c.Id, Name = c.Name, Description = c.Description, IsActive = c.IsActive, CreatedAt = c.CreatedAt
            }).ToListAsync();
        return Ok(ApiResponse<List<ExpenseCategoryDto>>.Ok(items));
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ApiResponse<ExpenseCategoryDto>>> Create([FromBody] CreateExpenseCategoryDto dto)
    {
        if (await _db.ExpenseCategories.AnyAsync(c => c.Name == dto.Name))
            return BadRequest(ApiResponse<ExpenseCategoryDto>.Fail("Category already exists"));
        var cat = new Models.ExpenseCategory { Name = dto.Name.Trim(), Description = dto.Description, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.ExpenseCategories.Add(cat);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<ExpenseCategoryDto>.Ok(new ExpenseCategoryDto
        {
            Id = cat.Id, Name = cat.Name, Description = cat.Description, IsActive = cat.IsActive, CreatedAt = cat.CreatedAt
        }));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ApiResponse<ExpenseCategoryDto>>> Update(int id, [FromBody] UpdateExpenseCategoryDto dto)
    {
        var cat = await _db.ExpenseCategories.FindAsync(id);
        if (cat is null) return NotFound(ApiResponse<ExpenseCategoryDto>.Fail("Not found"));
        cat.Name = dto.Name.Trim();
        cat.Description = dto.Description;
        cat.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<ExpenseCategoryDto>.Ok(new ExpenseCategoryDto
        {
            Id = cat.Id, Name = cat.Name, Description = cat.Description, IsActive = cat.IsActive, CreatedAt = cat.CreatedAt
        }));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        var cat = await _db.ExpenseCategories.FindAsync(id);
        if (cat is null) return NotFound(ApiResponse<object>.Fail("Not found"));
        if (await _db.Expenses.AnyAsync(e => e.CategoryId == id))
        {
            cat.IsActive = false;
            await _db.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(new { }, "Category deactivated because expenses exist"));
        }
        _db.ExpenseCategories.Remove(cat);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { }, "Deleted"));
    }
}

[ApiController]
[Route("api/expenses")]
[Authorize]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenses;

    public ExpensesController(IExpenseService expenses) => _expenses = expenses;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ExpenseDto>>>> Get(
        [FromQuery] int? employeeId, [FromQuery] int? categoryId, [FromQuery] string? status,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var restrict = User.IsInRole("ADMIN") ? null : User.GetEmployeeId();
        if (!User.IsInRole("ADMIN") && restrict is null) return Forbid();
        var result = await _expenses.GetAsync(employeeId, categoryId, status, from, to, search, page, pageSize, restrict);
        return Ok(ApiResponse<PagedResult<ExpenseDto>>.Ok(result));
    }

    [HttpGet("my")]
    [Authorize(Roles = "EMPLOYEE")]
    public async Task<ActionResult<ApiResponse<PagedResult<ExpenseDto>>>> My(
        [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var employeeId = User.GetEmployeeId();
        if (employeeId is null) return Forbid();
        var result = await _expenses.GetAsync(null, null, status, null, null, null, page, pageSize, employeeId);
        return Ok(ApiResponse<PagedResult<ExpenseDto>>.Ok(result));
    }

    [HttpGet("pending")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ApiResponse<PagedResult<ExpenseDto>>>> Pending(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _expenses.GetAsync(null, null, "PENDING", null, null, null, page, pageSize, null);
        return Ok(ApiResponse<PagedResult<ExpenseDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> GetById(int id)
    {
        var restrict = User.IsInRole("ADMIN") ? null : User.GetEmployeeId();
        var expense = await _expenses.GetByIdAsync(id, restrict);
        if (expense is null) return NotFound(ApiResponse<ExpenseDto>.Fail("Expense not found"));
        return Ok(ApiResponse<ExpenseDto>.Ok(expense));
    }

    [HttpPost]
    [Authorize(Roles = "EMPLOYEE")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> Create([FromForm] CreateExpenseDto dto, IFormFile? receipt)
    {
        var employeeId = User.GetEmployeeId();
        if (employeeId is null) return Forbid();
        var (success, message, data) = await _expenses.CreateAsync(employeeId.Value, dto, receipt, User.GetUserId());
        if (!success) return BadRequest(ApiResponse<ExpenseDto>.Fail(message));
        return Ok(ApiResponse<ExpenseDto>.Ok(data!, message));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "EMPLOYEE")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> Update(int id, [FromForm] UpdateExpenseDto dto, IFormFile? receipt)
    {
        var (success, message, data) = await _expenses.UpdateAsync(id, dto, receipt, User.GetUserId(), User.GetEmployeeId());
        if (!success) return BadRequest(ApiResponse<ExpenseDto>.Fail(message));
        return Ok(ApiResponse<ExpenseDto>.Ok(data!, message));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        var restrict = User.IsInRole("ADMIN") ? null : User.GetEmployeeId();
        var (success, message) = await _expenses.DeleteAsync(id, User.GetUserId(), restrict);
        if (!success) return BadRequest(ApiResponse<object>.Fail(message));
        return Ok(ApiResponse<object>.Ok(new { }, message));
    }

    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> Approve(int id, [FromBody] ApproveExpenseDto? dto)
    {
        var (success, message, data) = await _expenses.ApproveAsync(id, User.GetUserId(), dto?.Comments);
        if (!success) return BadRequest(ApiResponse<ExpenseDto>.Fail(message));
        return Ok(ApiResponse<ExpenseDto>.Ok(data!, message));
    }

    [HttpPut("{id:int}/reject")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> Reject(int id, [FromBody] RejectExpenseDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse<ExpenseDto>.Fail("Validation failed"));
        var (success, message, data) = await _expenses.RejectAsync(id, User.GetUserId(), dto.Comments);
        if (!success) return BadRequest(ApiResponse<ExpenseDto>.Fail(message));
        return Ok(ApiResponse<ExpenseDto>.Ok(data!, message));
    }
}

[ApiController]
[Route("api/transactions")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TransactionsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<TransactionDto>>>> Get(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Transactions.Include(t => t.Employee).AsQueryable();
        if (!User.IsInRole("ADMIN"))
        {
            var employeeId = User.GetEmployeeId();
            if (employeeId is null) return Forbid();
            query = query.Where(t => t.EmployeeId == employeeId);
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(t => t.TransactionDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                EmployeeId = t.EmployeeId,
                EmployeeName = t.Employee.FirstName + " " + t.Employee.LastName,
                ExpenseId = t.ExpenseId,
                TransactionType = t.TransactionType,
                Amount = t.Amount,
                Description = t.Description,
                TransactionDate = t.TransactionDate,
                ReferenceNumber = t.ReferenceNumber
            }).ToListAsync();

        return Ok(ApiResponse<PagedResult<TransactionDto>>.Ok(new PagedResult<TransactionDto>
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        }));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> GetById(int id)
    {
        var t = await _db.Transactions.Include(x => x.Employee).FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound(ApiResponse<TransactionDto>.Fail("Not found"));
        if (!User.IsInRole("ADMIN") && t.EmployeeId != User.GetEmployeeId())
            return Forbid();

        return Ok(ApiResponse<TransactionDto>.Ok(new TransactionDto
        {
            Id = t.Id,
            EmployeeId = t.EmployeeId,
            EmployeeName = $"{t.Employee.FirstName} {t.Employee.LastName}",
            ExpenseId = t.ExpenseId,
            TransactionType = t.TransactionType,
            Amount = t.Amount,
            Description = t.Description,
            TransactionDate = t.TransactionDate,
            ReferenceNumber = t.ReferenceNumber
        }));
    }
}
