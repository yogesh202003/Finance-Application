using FinanceManagementApp.API.Data;
using FinanceManagementApp.API.DTOs;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FinanceManagementApp.API.Services;

public interface IDashboardService
{
    Task<DashboardAdminDto> GetAdminAsync();
    Task<DashboardEmployeeDto> GetEmployeeAsync(int employeeId);
}

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db) => _db = db;

    public async Task<DashboardAdminDto> GetAdminAsync()
    {
        var expenses = await _db.Expenses.Include(e => e.Category).Include(e => e.Employee).ToListAsync();
        var now = DateTime.UtcNow;
        var months = Enumerable.Range(0, 6)
            .Select(i => now.AddMonths(-i))
            .Reverse()
            .Select(d => new { d.Year, d.Month, Label = d.ToString("MMM yyyy") })
            .ToList();

        var monthly = months.Select(m => new MonthlyAmountDto
        {
            Month = m.Label,
            Amount = expenses.Where(e => e.ExpenseDate.Year == m.Year && e.ExpenseDate.Month == m.Month && e.Status == "APPROVED").Sum(e => e.Amount),
            Count = expenses.Count(e => e.ExpenseDate.Year == m.Year && e.ExpenseDate.Month == m.Month)
        }).ToList();

        var categories = await _db.ExpenseCategories.ToListAsync();
        var categoryBreakdown = categories.Select(c => new CategoryAmountDto
        {
            Category = c.Name,
            Amount = expenses.Where(e => e.CategoryId == c.Id && e.Status == "APPROVED").Sum(e => e.Amount),
            Count = expenses.Count(e => e.CategoryId == c.Id)
        }).Where(x => x.Count > 0).OrderByDescending(x => x.Amount).ToList();

        var recentTx = await _db.Transactions.Include(t => t.Employee)
            .OrderByDescending(t => t.TransactionDate).Take(5)
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

        var pending = await _db.Expenses.Include(e => e.Employee).Include(e => e.Category)
            .Where(e => e.Status == "PENDING")
            .OrderByDescending(e => e.CreatedAt).Take(5)
            .Select(e => new ExpenseDto
            {
                Id = e.Id,
                EmployeeId = e.EmployeeId,
                EmployeeName = e.Employee.FirstName + " " + e.Employee.LastName,
                EmployeeCode = e.Employee.EmployeeCode,
                CategoryId = e.CategoryId,
                CategoryName = e.Category.Name,
                Amount = e.Amount,
                Description = e.Description,
                ExpenseDate = e.ExpenseDate,
                ReceiptUrl = e.ReceiptUrl,
                Status = e.Status,
                CreatedAt = e.CreatedAt
            }).ToListAsync();

        return new DashboardAdminDto
        {
            TotalEmployees = await _db.Employees.CountAsync(),
            ActiveEmployees = await _db.Employees.CountAsync(e => e.Status == "ACTIVE"),
            TotalExpenses = expenses.Count,
            PendingExpenses = expenses.Count(e => e.Status == "PENDING"),
            ApprovedExpenses = expenses.Count(e => e.Status == "APPROVED"),
            RejectedExpenses = expenses.Count(e => e.Status == "REJECTED"),
            TotalExpenseAmount = expenses.Where(e => e.Status == "APPROVED").Sum(e => e.Amount),
            MonthlyExpenses = monthly,
            CategoryBreakdown = categoryBreakdown,
            RecentTransactions = recentTx,
            PendingApprovals = pending
        };
    }

    public async Task<DashboardEmployeeDto> GetEmployeeAsync(int employeeId)
    {
        var expenses = await _db.Expenses.Include(e => e.Category).Include(e => e.Employee)
            .Where(e => e.EmployeeId == employeeId).ToListAsync();

        return new DashboardEmployeeDto
        {
            TotalMyExpenses = expenses.Count,
            PendingExpenses = expenses.Count(e => e.Status == "PENDING"),
            ApprovedExpenses = expenses.Count(e => e.Status == "APPROVED"),
            RejectedExpenses = expenses.Count(e => e.Status == "REJECTED"),
            ApprovedAmount = expenses.Where(e => e.Status == "APPROVED").Sum(e => e.Amount),
            RecentExpenses = expenses.OrderByDescending(e => e.CreatedAt).Take(5).Select(e => new ExpenseDto
            {
                Id = e.Id,
                EmployeeId = e.EmployeeId,
                EmployeeName = $"{e.Employee.FirstName} {e.Employee.LastName}",
                EmployeeCode = e.Employee.EmployeeCode,
                CategoryId = e.CategoryId,
                CategoryName = e.Category.Name,
                Amount = e.Amount,
                Description = e.Description,
                ExpenseDate = e.ExpenseDate,
                ReceiptUrl = e.ReceiptUrl,
                Status = e.Status,
                CreatedAt = e.CreatedAt
            }).ToList()
        };
    }
}

public interface IReportService
{
    Task<ReportDto> MonthlyAsync(DateTime? from, DateTime? to);
    Task<ReportDto> EmployeeAsync(DateTime? from, DateTime? to, int? employeeId);
    Task<ReportDto> DepartmentAsync(DateTime? from, DateTime? to, int? departmentId);
    Task<ReportDto> CategoryAsync(DateTime? from, DateTime? to, int? categoryId, string? status);
    Task<byte[]> ExportCsvAsync(ReportDto report);
    Task<byte[]> ExportPdfAsync(ReportDto report);
}

public class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db) => _db = db;

    public async Task<ReportDto> MonthlyAsync(DateTime? from, DateTime? to)
    {
        var query = Filter(_db.Expenses.AsQueryable(), from, to, null, null, null, "APPROVED");
        var data = await query.ToListAsync();
        var groups = data.GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new ReportItemDto
            {
                Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                Amount = g.Sum(x => x.Amount),
                Count = g.Count()
            }).ToList();

        return Build("Monthly Expense Report", from, to, groups);
    }

    public async Task<ReportDto> EmployeeAsync(DateTime? from, DateTime? to, int? employeeId)
    {
        var query = Filter(_db.Expenses.Include(e => e.Employee).AsQueryable(), from, to, employeeId, null, null, null);
        var data = await query.ToListAsync();
        var groups = data.GroupBy(e => e.Employee)
            .Select(g => new ReportItemDto
            {
                Label = $"{g.Key.EmployeeCode} - {g.Key.FirstName} {g.Key.LastName}",
                Amount = g.Sum(x => x.Amount),
                Count = g.Count()
            }).OrderByDescending(x => x.Amount).ToList();
        return Build("Employee Expense Report", from, to, groups);
    }

    public async Task<ReportDto> DepartmentAsync(DateTime? from, DateTime? to, int? departmentId)
    {
        var query = Filter(_db.Expenses.Include(e => e.Employee).ThenInclude(emp => emp.Department).AsQueryable(),
            from, to, null, departmentId, null, null);
        var data = await query.ToListAsync();
        var groups = data.GroupBy(e => e.Employee.Department.Name)
            .Select(g => new ReportItemDto
            {
                Label = g.Key,
                Amount = g.Sum(x => x.Amount),
                Count = g.Count()
            }).OrderByDescending(x => x.Amount).ToList();
        return Build("Department Expense Report", from, to, groups);
    }

    public async Task<ReportDto> CategoryAsync(DateTime? from, DateTime? to, int? categoryId, string? status)
    {
        var query = Filter(_db.Expenses.Include(e => e.Category).AsQueryable(), from, to, null, null, categoryId, status);
        var data = await query.ToListAsync();
        var groups = data.GroupBy(e => e.Category.Name)
            .Select(g => new ReportItemDto
            {
                Label = g.Key,
                Amount = g.Sum(x => x.Amount),
                Count = g.Count(),
                Status = status
            }).OrderByDescending(x => x.Amount).ToList();
        return Build("Category Expense Report", from, to, groups);
    }

    public Task<byte[]> ExportCsvAsync(ReportDto report)
    {
        using var writer = new StringWriter();
        using var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);
        csv.WriteField("Label");
        csv.WriteField("Amount");
        csv.WriteField("Count");
        csv.WriteField("Status");
        csv.NextRecord();
        foreach (var item in report.Items)
        {
            csv.WriteField(item.Label);
            csv.WriteField(item.Amount);
            csv.WriteField(item.Count);
            csv.WriteField(item.Status);
            csv.NextRecord();
        }
        return Task.FromResult(System.Text.Encoding.UTF8.GetBytes(writer.ToString()));
    }

    public Task<byte[]> ExportPdfAsync(ReportDto report)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Header().Text(report.Title).FontSize(18).SemiBold();
                page.Content().Column(col =>
                {
                    col.Item().Text($"Total Amount: {report.TotalAmount:C} | Total Count: {report.TotalCount}");
                    col.Item().PaddingVertical(10);
                    foreach (var item in report.Items)
                        col.Item().Text($"{item.Label}: {item.Amount:C} ({item.Count})");
                });
            });
        });
        return Task.FromResult(document.GeneratePdf());
    }

    private static IQueryable<Models.Expense> Filter(
        IQueryable<Models.Expense> query, DateTime? from, DateTime? to,
        int? employeeId, int? departmentId, int? categoryId, string? status)
    {
        if (from.HasValue) query = query.Where(e => e.ExpenseDate >= from.Value.Date);
        if (to.HasValue) query = query.Where(e => e.ExpenseDate <= to.Value.Date);
        if (employeeId.HasValue) query = query.Where(e => e.EmployeeId == employeeId);
        if (departmentId.HasValue) query = query.Where(e => e.Employee.DepartmentId == departmentId);
        if (categoryId.HasValue) query = query.Where(e => e.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(e => e.Status == status);
        return query;
    }

    private static ReportDto Build(string title, DateTime? from, DateTime? to, List<ReportItemDto> items) => new()
    {
        Title = title,
        FromDate = from,
        ToDate = to,
        Items = items,
        TotalAmount = items.Sum(i => i.Amount),
        TotalCount = items.Sum(i => i.Count)
    };
}
