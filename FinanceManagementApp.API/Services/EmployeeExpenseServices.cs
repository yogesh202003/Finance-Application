using FinanceManagementApp.API.Data;
using FinanceManagementApp.API.DTOs;
using FinanceManagementApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceManagementApp.API.Services;

public interface IEmployeeService
{
    Task<PagedResult<EmployeeDto>> GetAsync(string? search, string? status, int? departmentId, int page, int pageSize);
    Task<EmployeeDto?> GetByIdAsync(int id);
    Task<(bool Success, string Message, EmployeeDto? Data)> CreateAsync(CreateEmployeeDto dto, int actorUserId);
    Task<(bool Success, string Message, EmployeeDto? Data)> UpdateAsync(int id, UpdateEmployeeDto dto, int actorUserId);
    Task<(bool Success, string Message)> DeleteAsync(int id, int actorUserId);
    Task<(bool Success, string Message)> UpdateStatusAsync(int id, string status, int actorUserId);
    Task<(bool Success, string Message)> ResetPasswordAsync(int id, string newPassword, int actorUserId);
}

public class EmployeeService : IEmployeeService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public EmployeeService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<PagedResult<EmployeeDto>> GetAsync(string? search, string? status, int? departmentId, int page, int pageSize)
    {
        var query = _db.Employees.Include(e => e.Department).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(e =>
                e.EmployeeCode.ToLower().Contains(s) ||
                e.FirstName.ToLower().Contains(s) ||
                e.LastName.ToLower().Contains(s) ||
                e.Email.ToLower().Contains(s) ||
                (e.FirstName + " " + e.LastName).ToLower().Contains(s));
        }
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(e => e.Status == status);
        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentId == departmentId);

        var total = await query.CountAsync();
        var entities = await query.OrderBy(e => e.EmployeeCode)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();
        var items = entities.Select(Map).ToList();

        return new PagedResult<EmployeeDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<EmployeeDto?> GetByIdAsync(int id)
    {
        var e = await _db.Employees.Include(x => x.Department).FirstOrDefaultAsync(x => x.Id == id);
        return e is null ? null : Map(e);
    }

    public async Task<(bool Success, string Message, EmployeeDto? Data)> CreateAsync(CreateEmployeeDto dto, int actorUserId)
    {
        if (!await _db.Departments.AnyAsync(d => d.Id == dto.DepartmentId && d.IsActive))
            return (false, "Invalid department", null);
        if (await _db.Employees.AnyAsync(e => e.EmployeeCode == dto.EmployeeCode))
            return (false, "Employee code already exists", null);
        if (await _db.Employees.AnyAsync(e => e.Email == dto.Email) || await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return (false, "Email already exists", null);

        var employeeRole = await _db.Roles.FirstAsync(r => r.Name == "EMPLOYEE");
        var emp = new Employee
        {
            EmployeeCode = dto.EmployeeCode.Trim(),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim(),
            Phone = dto.Phone,
            DepartmentId = dto.DepartmentId,
            Designation = dto.Designation.Trim(),
            JoiningDate = dto.JoiningDate,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        _db.Employees.Add(emp);
        await _db.SaveChangesAsync();

        _db.Users.Add(new User
        {
            Username = dto.EmployeeCode.Trim().ToLowerInvariant(),
            Email = dto.Email.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId = employeeRole.Id,
            EmployeeId = emp.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        await _audit.LogAsync(actorUserId, "EMPLOYEE_CREATED", "Employee", emp.Id.ToString(), $"Created {emp.EmployeeCode}");

        var created = await GetByIdAsync(emp.Id);
        return (true, "Employee created", created);
    }

    public async Task<(bool Success, string Message, EmployeeDto? Data)> UpdateAsync(int id, UpdateEmployeeDto dto, int actorUserId)
    {
        var emp = await _db.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == id);
        if (emp is null) return (false, "Employee not found", null);
        if (!await _db.Departments.AnyAsync(d => d.Id == dto.DepartmentId))
            return (false, "Invalid department", null);
        if (await _db.Employees.AnyAsync(e => e.Email == dto.Email && e.Id != id))
            return (false, "Email already exists", null);

        emp.FirstName = dto.FirstName.Trim();
        emp.LastName = dto.LastName.Trim();
        emp.Email = dto.Email.Trim();
        emp.Phone = dto.Phone;
        emp.DepartmentId = dto.DepartmentId;
        emp.Designation = dto.Designation.Trim();
        emp.JoiningDate = dto.JoiningDate;
        emp.UpdatedAt = DateTime.UtcNow;

        if (emp.User is not null)
        {
            emp.User.Email = dto.Email.Trim();
            emp.User.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(actorUserId, "EMPLOYEE_UPDATED", "Employee", id.ToString(), $"Updated {emp.EmployeeCode}");
        return (true, "Employee updated", await GetByIdAsync(id));
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id, int actorUserId)
    {
        // Soft-delete: deactivate instead of hard delete to preserve financial history
        return await UpdateStatusAsync(id, "INACTIVE", actorUserId);
    }

    public async Task<(bool Success, string Message)> UpdateStatusAsync(int id, string status, int actorUserId)
    {
        status = status.ToUpperInvariant();
        if (status is not ("ACTIVE" or "INACTIVE"))
            return (false, "Status must be ACTIVE or INACTIVE");

        var emp = await _db.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == id);
        if (emp is null) return (false, "Employee not found");

        emp.Status = status;
        emp.UpdatedAt = DateTime.UtcNow;
        if (emp.User is not null)
        {
            emp.User.IsActive = status == "ACTIVE";
            emp.User.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        await _audit.LogAsync(actorUserId,
            status == "ACTIVE" ? "EMPLOYEE_ACTIVATED" : "EMPLOYEE_DEACTIVATED",
            "Employee", id.ToString(), $"{emp.EmployeeCode} set to {status}");
        return (true, $"Employee {status.ToLowerInvariant()}");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(int id, string newPassword, int actorUserId)
    {
        var emp = await _db.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == id);
        if (emp?.User is null) return (false, "Employee user account not found");
        emp.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        emp.User.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(actorUserId, "PASSWORD_CHANGED", "Employee", id.ToString(), $"Password reset for {emp.EmployeeCode}");
        return (true, "Password reset successfully");
    }

    private static EmployeeDto Map(Employee e) => new()
    {
        Id = e.Id,
        EmployeeCode = e.EmployeeCode,
        FirstName = e.FirstName,
        LastName = e.LastName,
        Email = e.Email,
        Phone = e.Phone,
        DepartmentId = e.DepartmentId,
        DepartmentName = e.Department?.Name ?? string.Empty,
        Designation = e.Designation,
        JoiningDate = e.JoiningDate,
        Status = e.Status,
        CreatedAt = e.CreatedAt
    };
}

public interface IExpenseService
{
    Task<PagedResult<ExpenseDto>> GetAsync(int? employeeId, int? categoryId, string? status, DateTime? from, DateTime? to, string? search, int page, int pageSize, int? restrictToEmployeeId);
    Task<ExpenseDto?> GetByIdAsync(int id, int? restrictToEmployeeId);
    Task<(bool Success, string Message, ExpenseDto? Data)> CreateAsync(int employeeId, CreateExpenseDto dto, IFormFile? receipt, int actorUserId);
    Task<(bool Success, string Message, ExpenseDto? Data)> UpdateAsync(int id, UpdateExpenseDto dto, IFormFile? receipt, int actorUserId, int? restrictToEmployeeId);
    Task<(bool Success, string Message)> DeleteAsync(int id, int actorUserId, int? restrictToEmployeeId);
    Task<(bool Success, string Message, ExpenseDto? Data)> ApproveAsync(int id, int adminUserId, string? comments);
    Task<(bool Success, string Message, ExpenseDto? Data)> RejectAsync(int id, int adminUserId, string comments);
}

public class ExpenseService : IExpenseService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private readonly INotificationService _notifications;
    private readonly IFileStorageService _files;

    public ExpenseService(AppDbContext db, IAuditService audit, INotificationService notifications, IFileStorageService files)
    {
        _db = db;
        _audit = audit;
        _notifications = notifications;
        _files = files;
    }

    public async Task<PagedResult<ExpenseDto>> GetAsync(int? employeeId, int? categoryId, string? status, DateTime? from, DateTime? to, string? search, int page, int pageSize, int? restrictToEmployeeId)
    {
        var query = _db.Expenses
            .Include(e => e.Employee)
            .Include(e => e.Category)
            .Include(e => e.Approvals)
            .AsQueryable();

        if (restrictToEmployeeId.HasValue)
            query = query.Where(e => e.EmployeeId == restrictToEmployeeId);
        else if (employeeId.HasValue)
            query = query.Where(e => e.EmployeeId == employeeId);

        if (categoryId.HasValue) query = query.Where(e => e.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(e => e.Status == status);
        if (from.HasValue) query = query.Where(e => e.ExpenseDate >= from.Value.Date);
        if (to.HasValue) query = query.Where(e => e.ExpenseDate <= to.Value.Date);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(e =>
                e.Description.ToLower().Contains(s) ||
                e.Employee.FirstName.ToLower().Contains(s) ||
                e.Employee.LastName.ToLower().Contains(s) ||
                e.Employee.EmployeeCode.ToLower().Contains(s));
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return new PagedResult<ExpenseDto>
        {
            Items = items.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<ExpenseDto?> GetByIdAsync(int id, int? restrictToEmployeeId)
    {
        var e = await _db.Expenses.Include(x => x.Employee).Include(x => x.Category).Include(x => x.Approvals)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (e is null) return null;
        if (restrictToEmployeeId.HasValue && e.EmployeeId != restrictToEmployeeId) return null;
        return Map(e);
    }

    public async Task<(bool Success, string Message, ExpenseDto? Data)> CreateAsync(int employeeId, CreateExpenseDto dto, IFormFile? receipt, int actorUserId)
    {
        if (!await _db.ExpenseCategories.AnyAsync(c => c.Id == dto.CategoryId && c.IsActive))
            return (false, "Invalid category", null);

        string? receiptUrl = null;
        if (receipt is not null)
        {
            var upload = await _files.SaveReceiptAsync(receipt);
            if (!upload.Success) return (false, upload.Message, null);
            receiptUrl = upload.Url;
        }

        var expense = new Expense
        {
            EmployeeId = employeeId,
            CategoryId = dto.CategoryId,
            Amount = dto.Amount,
            Description = dto.Description.Trim(),
            ExpenseDate = dto.ExpenseDate.Date,
            ReceiptUrl = receiptUrl,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };
        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(actorUserId, "EXPENSE_CREATED", "Expense", expense.Id.ToString(), $"Expense EXP{expense.Id:D3} created");

        // Notify all admins
        var adminUserIds = await _db.Users.Where(u => u.Role.Name == "ADMIN" && u.IsActive).Select(u => u.Id).ToListAsync();
        foreach (var adminId in adminUserIds)
            await _notifications.NotifyAsync(adminId, "New expense request", $"New expense request EXP{expense.Id:D3} requires approval.");

        return (true, "Expense submitted", await GetByIdAsync(expense.Id, null));
    }

    public async Task<(bool Success, string Message, ExpenseDto? Data)> UpdateAsync(int id, UpdateExpenseDto dto, IFormFile? receipt, int actorUserId, int? restrictToEmployeeId)
    {
        var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id);
        if (expense is null) return (false, "Expense not found", null);
        if (restrictToEmployeeId.HasValue && expense.EmployeeId != restrictToEmployeeId)
            return (false, "Forbidden", null);
        if (expense.Status != "PENDING")
            return (false, "Only pending expenses can be updated", null);
        if (!await _db.ExpenseCategories.AnyAsync(c => c.Id == dto.CategoryId && c.IsActive))
            return (false, "Invalid category", null);

        if (receipt is not null)
        {
            var upload = await _files.SaveReceiptAsync(receipt);
            if (!upload.Success) return (false, upload.Message, null);
            _files.DeleteIfExists(expense.ReceiptUrl);
            expense.ReceiptUrl = upload.Url;
        }

        expense.CategoryId = dto.CategoryId;
        expense.Amount = dto.Amount;
        expense.Description = dto.Description.Trim();
        expense.ExpenseDate = dto.ExpenseDate.Date;
        expense.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(actorUserId, "EXPENSE_UPDATED", "Expense", id.ToString(), $"Expense EXP{id:D3} updated");
        return (true, "Expense updated", await GetByIdAsync(id, null));
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id, int actorUserId, int? restrictToEmployeeId)
    {
        var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id);
        if (expense is null) return (false, "Expense not found");
        if (restrictToEmployeeId.HasValue && expense.EmployeeId != restrictToEmployeeId)
            return (false, "Forbidden");
        if (expense.Status != "PENDING")
            return (false, "Only pending expenses can be deleted");

        _files.DeleteIfExists(expense.ReceiptUrl);
        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(actorUserId, "EXPENSE_DELETED", "Expense", id.ToString(), $"Expense EXP{id:D3} deleted");
        return (true, "Expense deleted");
    }

    public async Task<(bool Success, string Message, ExpenseDto? Data)> ApproveAsync(int id, int adminUserId, string? comments)
    {
        var expense = await _db.Expenses.Include(e => e.Employee).ThenInclude(emp => emp.User)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (expense is null) return (false, "Expense not found", null);
        if (expense.Status != "PENDING") return (false, "Expense is not pending", null);

        expense.Status = "APPROVED";
        expense.UpdatedAt = DateTime.UtcNow;

        _db.ExpenseApprovals.Add(new ExpenseApproval
        {
            ExpenseId = expense.Id,
            AdminId = adminUserId,
            Action = "APPROVED",
            Comments = comments,
            ActionDate = DateTime.UtcNow
        });

        var reference = $"TXN{DateTime.UtcNow:yyyyMMddHHmmss}{expense.Id:D4}";
        _db.Transactions.Add(new Transaction
        {
            EmployeeId = expense.EmployeeId,
            ExpenseId = expense.Id,
            TransactionType = "DEBIT",
            Amount = expense.Amount,
            Description = $"Expense reimbursement for EXP{expense.Id:D3}",
            TransactionDate = DateTime.UtcNow,
            ReferenceNumber = reference,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        if (expense.Employee.User is not null)
            await _notifications.NotifyAsync(expense.Employee.User.Id, "Expense approved",
                $"Your expense EXP{expense.Id:D3} has been approved.");

        await _audit.LogAsync(adminUserId, "EXPENSE_APPROVED", "Expense", id.ToString(), $"Expense EXP{id:D3} approved");
        return (true, "Expense approved", await GetByIdAsync(id, null));
    }

    public async Task<(bool Success, string Message, ExpenseDto? Data)> RejectAsync(int id, int adminUserId, string comments)
    {
        var expense = await _db.Expenses.Include(e => e.Employee).ThenInclude(emp => emp.User)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (expense is null) return (false, "Expense not found", null);
        if (expense.Status != "PENDING") return (false, "Expense is not pending", null);
        if (string.IsNullOrWhiteSpace(comments)) return (false, "Rejection comment is required", null);

        expense.Status = "REJECTED";
        expense.UpdatedAt = DateTime.UtcNow;

        _db.ExpenseApprovals.Add(new ExpenseApproval
        {
            ExpenseId = expense.Id,
            AdminId = adminUserId,
            Action = "REJECTED",
            Comments = comments.Trim(),
            ActionDate = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        if (expense.Employee.User is not null)
            await _notifications.NotifyAsync(expense.Employee.User.Id, "Expense rejected",
                $"Your expense EXP{expense.Id:D3} has been rejected.");

        await _audit.LogAsync(adminUserId, "EXPENSE_REJECTED", "Expense", id.ToString(), $"Expense EXP{id:D3} rejected");
        return (true, "Expense rejected", await GetByIdAsync(id, null));
    }

    private static ExpenseDto Map(Expense e) => new()
    {
        Id = e.Id,
        EmployeeId = e.EmployeeId,
        EmployeeName = e.Employee is null ? string.Empty : $"{e.Employee.FirstName} {e.Employee.LastName}",
        EmployeeCode = e.Employee?.EmployeeCode ?? string.Empty,
        CategoryId = e.CategoryId,
        CategoryName = e.Category?.Name ?? string.Empty,
        Amount = e.Amount,
        Description = e.Description,
        ExpenseDate = e.ExpenseDate,
        ReceiptUrl = e.ReceiptUrl,
        Status = e.Status,
        CreatedAt = e.CreatedAt,
        ApprovalComments = e.Approvals?.OrderByDescending(a => a.ActionDate).FirstOrDefault()?.Comments
    };
}
