using FinanceManagementApp.API.Data;
using FinanceManagementApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceManagementApp.API.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        if (!await db.Roles.AnyAsync())
        {
            db.Roles.AddRange(
                new Role { Name = "ADMIN", CreatedAt = DateTime.UtcNow },
                new Role { Name = "EMPLOYEE", CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        if (!await db.Departments.AnyAsync())
        {
            db.Departments.AddRange(
                new Department { Name = "Finance", Description = "Finance department", IsActive = true },
                new Department { Name = "Human Resources", Description = "HR department", IsActive = true },
                new Department { Name = "Information Technology", Description = "IT department", IsActive = true },
                new Department { Name = "Operations", Description = "Operations department", IsActive = true },
                new Department { Name = "Sales", Description = "Sales department", IsActive = true });
            await db.SaveChangesAsync();
        }

        if (!await db.ExpenseCategories.AnyAsync())
        {
            db.ExpenseCategories.AddRange(
                new ExpenseCategory { Name = "Travel", Description = "Travel expenses" },
                new ExpenseCategory { Name = "Food", Description = "Meals and food" },
                new ExpenseCategory { Name = "Accommodation", Description = "Lodging" },
                new ExpenseCategory { Name = "Transportation", Description = "Local transport" },
                new ExpenseCategory { Name = "Office Supplies", Description = "Office materials" },
                new ExpenseCategory { Name = "Medical", Description = "Medical expenses" },
                new ExpenseCategory { Name = "Other", Description = "Other expenses" });
            await db.SaveChangesAsync();
        }

        var adminRole = await db.Roles.FirstAsync(r => r.Name == "ADMIN");
        var employeeRole = await db.Roles.FirstAsync(r => r.Name == "EMPLOYEE");
        var financeDept = await db.Departments.FirstAsync(d => d.Name == "Finance");
        var itDept = await db.Departments.FirstAsync(d => d.Name == "Information Technology");
        var hrDept = await db.Departments.FirstAsync(d => d.Name == "Human Resources");

        if (!await db.Users.AnyAsync(u => u.Email == "admin@financeapp.com"))
        {
            db.Users.Add(new User
            {
                Username = "admin",
                Email = "admin@financeapp.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                RoleId = adminRole.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        if (!await db.Employees.AnyAsync(e => e.EmployeeCode == "EMP001"))
        {
            var emp = new Employee
            {
                EmployeeCode = "EMP001",
                FirstName = "John",
                LastName = "Employee",
                Email = "employee@financeapp.com",
                Phone = "555-0101",
                DepartmentId = financeDept.Id,
                Designation = "Finance Analyst",
                JoiningDate = DateTime.UtcNow.Date.AddYears(-1),
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };
            db.Employees.Add(emp);
            await db.SaveChangesAsync();

            db.Users.Add(new User
            {
                Username = "employee",
                Email = "employee@financeapp.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee@123"),
                RoleId = employeeRole.Id,
                EmployeeId = emp.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Additional sample employees
        await EnsureEmployeeAsync(db, employeeRole.Id, itDept.Id, "EMP002", "Alice", "Johnson", "alice.johnson@financeapp.com", "Software Engineer", "Employee@123");
        await EnsureEmployeeAsync(db, employeeRole.Id, hrDept.Id, "EMP003", "Bob", "Smith", "bob.smith@financeapp.com", "HR Specialist", "Employee@123");
        await EnsureEmployeeAsync(db, employeeRole.Id, financeDept.Id, "EMP004", "Carol", "Davis", "carol.davis@financeapp.com", "Accountant", "Employee@123");

        if (!await db.Expenses.AnyAsync())
        {
            var emp1 = await db.Employees.FirstAsync(e => e.EmployeeCode == "EMP001");
            var travel = await db.ExpenseCategories.FirstAsync(c => c.Name == "Travel");
            var food = await db.ExpenseCategories.FirstAsync(c => c.Name == "Food");

            db.Expenses.AddRange(
                new Expense
                {
                    EmployeeId = emp1.Id,
                    CategoryId = travel.Id,
                    Amount = 250.00m,
                    Description = "Client site visit travel",
                    ExpenseDate = DateTime.UtcNow.Date.AddDays(-5),
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow
                },
                new Expense
                {
                    EmployeeId = emp1.Id,
                    CategoryId = food.Id,
                    Amount = 45.50m,
                    Description = "Team lunch with client",
                    ExpenseDate = DateTime.UtcNow.Date.AddDays(-3),
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow
                });
            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureEmployeeAsync(
        AppDbContext db, int roleId, int deptId,
        string code, string first, string last, string email, string designation, string password)
    {
        if (await db.Employees.AnyAsync(e => e.EmployeeCode == code)) return;

        var emp = new Employee
        {
            EmployeeCode = code,
            FirstName = first,
            LastName = last,
            Email = email,
            Phone = "555-0100",
            DepartmentId = deptId,
            Designation = designation,
            JoiningDate = DateTime.UtcNow.Date.AddMonths(-6),
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        db.Users.Add(new User
        {
            Username = code.ToLowerInvariant(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            RoleId = roleId,
            EmployeeId = emp.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
