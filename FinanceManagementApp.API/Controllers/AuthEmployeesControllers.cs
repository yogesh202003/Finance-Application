using FinanceManagementApp.API.DTOs;
using FinanceManagementApp.API.Middleware;
using FinanceManagementApp.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceManagementApp.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.Fail("Validation failed", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

        var result = await _auth.LoginAsync(request);
        if (!result.Success) return Unauthorized(result);
        return Ok(result);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.Fail("Validation failed"));

        var (success, message) = await _auth.ChangePasswordAsync(User.GetUserId(), request);
        if (!success) return BadRequest(ApiResponse<object>.Fail(message));
        return Ok(ApiResponse<object>.Ok(new { }, message));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword([FromBody] ForgotPasswordDto request)
    {
        var (success, message, token) = await _auth.ForgotPasswordAsync(request);
        return Ok(ApiResponse<object>.Ok(new { resetToken = token }, message));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordDto request)
    {
        var (success, message) = await _auth.ResetPasswordAsync(request);
        if (!success) return BadRequest(ApiResponse<object>.Fail(message));
        return Ok(ApiResponse<object>.Ok(new { }, message));
    }
}

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    [HttpGet("admin")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ApiResponse<DashboardAdminDto>>> Admin() =>
        Ok(ApiResponse<DashboardAdminDto>.Ok(await _dashboard.GetAdminAsync()));

    [HttpGet("employee")]
    [Authorize(Roles = "EMPLOYEE")]
    public async Task<ActionResult<ApiResponse<DashboardEmployeeDto>>> Employee()
    {
        var employeeId = User.GetEmployeeId();
        if (employeeId is null) return Forbid();
        return Ok(ApiResponse<DashboardEmployeeDto>.Ok(await _dashboard.GetEmployeeAsync(employeeId.Value)));
    }
}

[ApiController]
[Route("api/employees")]
[Authorize(Roles = "ADMIN")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employees;

    public EmployeesController(IEmployeeService employees) => _employees = employees;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<EmployeeDto>>>> Get(
        [FromQuery] string? search, [FromQuery] string? status, [FromQuery] int? departmentId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await _employees.GetAsync(search, status, departmentId, page, pageSize);
        return Ok(ApiResponse<PagedResult<EmployeeDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> GetById(int id)
    {
        var emp = await _employees.GetByIdAsync(id);
        if (emp is null) return NotFound(ApiResponse<EmployeeDto>.Fail("Employee not found"));
        return Ok(ApiResponse<EmployeeDto>.Ok(emp));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> Create([FromBody] CreateEmployeeDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse<EmployeeDto>.Fail("Validation failed"));
        var (success, message, data) = await _employees.CreateAsync(dto, User.GetUserId());
        if (!success) return BadRequest(ApiResponse<EmployeeDto>.Fail(message));
        return Ok(ApiResponse<EmployeeDto>.Ok(data!, message));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> Update(int id, [FromBody] UpdateEmployeeDto dto)
    {
        var (success, message, data) = await _employees.UpdateAsync(id, dto, User.GetUserId());
        if (!success) return BadRequest(ApiResponse<EmployeeDto>.Fail(message));
        return Ok(ApiResponse<EmployeeDto>.Ok(data!, message));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        var (success, message) = await _employees.DeleteAsync(id, User.GetUserId());
        if (!success) return BadRequest(ApiResponse<object>.Fail(message));
        return Ok(ApiResponse<object>.Ok(new { }, message));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<ApiResponse<object>>> Status(int id, [FromBody] UpdateEmployeeStatusDto dto)
    {
        var (success, message) = await _employees.UpdateStatusAsync(id, dto.Status, User.GetUserId());
        if (!success) return BadRequest(ApiResponse<object>.Fail(message));
        return Ok(ApiResponse<object>.Ok(new { }, message));
    }

    [HttpPost("{id:int}/reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(int id, [FromBody] ResetEmployeePasswordDto dto)
    {
        var (success, message) = await _employees.ResetPasswordAsync(id, dto.NewPassword, User.GetUserId());
        if (!success) return BadRequest(ApiResponse<object>.Fail(message));
        return Ok(ApiResponse<object>.Ok(new { }, message));
    }
}

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly Data.AppDbContext _db;
    private readonly IAuditService _audit;

    public DepartmentsController(Data.AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<DepartmentDto>>>> Get()
    {
        var items = await _db.Departments.OrderBy(d => d.Name)
            .Select(d => new DepartmentDto
            {
                Id = d.Id, Name = d.Name, Description = d.Description, IsActive = d.IsActive, CreatedAt = d.CreatedAt
            }).ToListAsync();
        return Ok(ApiResponse<List<DepartmentDto>>.Ok(items));
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Create([FromBody] CreateDepartmentDto dto)
    {
        if (await _db.Departments.AnyAsync(d => d.Name == dto.Name))
            return BadRequest(ApiResponse<DepartmentDto>.Fail("Department already exists"));

        var dept = new Models.Department { Name = dto.Name.Trim(), Description = dto.Description, IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.Departments.Add(dept);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(User.GetUserId(), "DEPARTMENT_CREATED", "Department", dept.Id.ToString(), dept.Name);
        return Ok(ApiResponse<DepartmentDto>.Ok(new DepartmentDto
        {
            Id = dept.Id, Name = dept.Name, Description = dept.Description, IsActive = dept.IsActive, CreatedAt = dept.CreatedAt
        }, "Department created"));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Update(int id, [FromBody] UpdateDepartmentDto dto)
    {
        var dept = await _db.Departments.FindAsync(id);
        if (dept is null) return NotFound(ApiResponse<DepartmentDto>.Fail("Not found"));
        dept.Name = dto.Name.Trim();
        dept.Description = dto.Description;
        dept.IsActive = dto.IsActive;
        dept.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<DepartmentDto>.Ok(new DepartmentDto
        {
            Id = dept.Id, Name = dept.Name, Description = dept.Description, IsActive = dept.IsActive, CreatedAt = dept.CreatedAt
        }));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        var dept = await _db.Departments.FindAsync(id);
        if (dept is null) return NotFound(ApiResponse<object>.Fail("Not found"));
        if (await _db.Employees.AnyAsync(e => e.DepartmentId == id))
            return BadRequest(ApiResponse<object>.Fail("Cannot delete department with employees. Deactivate instead."));
        _db.Departments.Remove(dept);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { }, "Deleted"));
    }
}
