using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FinanceManagementApp.API.Data;
using FinanceManagementApp.API.DTOs;
using FinanceManagementApp.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FinanceManagementApp.API.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    Task<(bool Success, string Message)> ChangePasswordAsync(int userId, ChangePasswordDto request);
    Task<(bool Success, string Message, string? DevToken)> ForgotPasswordAsync(ForgotPasswordDto request);
    Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordDto request);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IAuditService _audit;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, IConfiguration config, IAuditService audit, ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _audit = audit;
        _logger = logger;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var identifier = request.UsernameOrEmail.Trim();
        var user = await _db.Users
            .Include(u => u.Role)
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u =>
                u.Email == identifier ||
                u.Username == identifier ||
                (u.Employee != null && u.Employee.EmployeeCode == identifier));

        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            await _audit.LogAsync(user?.Id, "LOGIN_FAILED", "User", user?.Id.ToString(), $"Failed login for {identifier}");
            _logger.LogWarning("Login failed for identifier {Identifier}", identifier);
            return new LoginResponseDto { Success = false, Message = "Invalid credentials" };
        }

        var expirationMinutes = int.Parse(_config["Jwt:ExpirationMinutes"] ?? "480");
        var expiration = DateTime.UtcNow.AddMinutes(expirationMinutes);
        var token = GenerateToken(user, expiration);

        await _audit.LogAsync(user.Id, "LOGIN_SUCCESS", "User", user.Id.ToString(), "User logged in successfully");

        var name = user.Employee is null
            ? user.Username
            : $"{user.Employee.FirstName} {user.Employee.LastName}";

        return new LoginResponseDto
        {
            Success = true,
            Message = "Login successful",
            Token = token,
            Expiration = expiration,
            UserId = user.Id,
            EmployeeId = user.EmployeeId,
            Name = name,
            Email = user.Email,
            Role = user.Role.Name
        };
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(int userId, ChangePasswordDto request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return (false, "User not found");
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return (false, "Current password is incorrect");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "PASSWORD_CHANGED", "User", userId.ToString(), "Password changed");
        return (true, "Password changed successfully");
    }

    public async Task<(bool Success, string Message, string? DevToken)> ForgotPasswordAsync(ForgotPasswordDto request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        // Always return success message to avoid account enumeration
        if (user is null)
            return (true, "If the email exists, a reset token has been generated.", null);

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(user.Id, "PASSWORD_RESET_REQUESTED", "User", user.Id.ToString(), "Password reset requested");

        // In development, return token so it can be tested via Swagger without email
        var isDev = string.Equals(_config["ASPNETCORE_ENVIRONMENT"], "Development", StringComparison.OrdinalIgnoreCase)
                    || Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        return (true, "If the email exists, a reset token has been generated.", isDev ? token : null);
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordDto request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null || user.PasswordResetToken != request.Token ||
            user.PasswordResetTokenExpiry is null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            return (false, "Invalid or expired reset token");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(user.Id, "PASSWORD_CHANGED", "User", user.Id.ToString(), "Password reset completed");
        return (true, "Password reset successfully");
    }

    private string GenerateToken(User user, DateTime expiration)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.Name),
            new("employeeId", user.EmployeeId?.ToString() ?? string.Empty)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiration,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public interface IAuditService
{
    Task LogAsync(int? userId, string action, string entityName, string? entityId, string? description);
}

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db) => _db = db;

    public async Task LogAsync(int? userId, string action, string entityName, string? entityId, string? description)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Description = description,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}

public interface INotificationService
{
    Task NotifyAsync(int userId, string title, string message);
}

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db) => _db = db;

    public async Task NotifyAsync(int userId, string title, string message)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}

public interface IFileStorageService
{
    Task<(bool Success, string? Url, string Message)> SaveReceiptAsync(IFormFile file);
    bool DeleteIfExists(string? relativeUrl);
}

public class LocalFileStorageService : IFileStorageService
{
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".pdf" };
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "application/pdf"
    };

    public LocalFileStorageService(IConfiguration config, IWebHostEnvironment env)
    {
        _config = config;
        _env = env;
    }

    public async Task<(bool Success, string? Url, string Message)> SaveReceiptAsync(IFormFile file)
    {
        if (file.Length == 0) return (false, null, "File is empty");

        var maxMb = int.Parse(_config["Upload:MaxFileSizeMB"] ?? "5");
        if (file.Length > maxMb * 1024L * 1024L)
            return (false, null, $"File exceeds {maxMb}MB limit");

        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext))
            return (false, null, "Only JPG, JPEG, PNG, and PDF files are allowed");

        if (!AllowedMimeTypes.Contains(file.ContentType))
            return (false, null, "Invalid file content type");

        // Basic magic-byte check
        await using var stream = file.OpenReadStream();
        var header = new byte[8];
        var read = await stream.ReadAsync(header.AsMemory(0, 8));
        stream.Position = 0;
        if (!IsValidFileHeader(header, read, ext))
            return (false, null, "File content does not match extension");

        var uploadRoot = _config["Upload:Directory"] ?? Path.Combine(_env.ContentRootPath, "..", "uploads");
        Directory.CreateDirectory(uploadRoot);
        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(uploadRoot, fileName);
        await using (var fs = File.Create(fullPath))
        {
            await stream.CopyToAsync(fs);
        }

        return (true, $"/uploads/{fileName}", "Uploaded");
    }

    public bool DeleteIfExists(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl)) return false;
        var uploadRoot = _config["Upload:Directory"] ?? Path.Combine(_env.ContentRootPath, "..", "uploads");
        var name = Path.GetFileName(relativeUrl);
        var full = Path.Combine(uploadRoot, name);
        if (!File.Exists(full)) return false;
        File.Delete(full);
        return true;
    }

    private static bool IsValidFileHeader(byte[] header, int length, string ext)
    {
        if (length < 3) return false;
        if (ext.Equals(".png", StringComparison.OrdinalIgnoreCase))
            return header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        if (ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            return header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        if (ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            return header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46; // %PDF
        return false;
    }
}
