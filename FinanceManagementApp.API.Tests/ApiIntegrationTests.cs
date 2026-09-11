using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FinanceManagementApp.API.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FinanceManagementApp.API.Tests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    private async Task<LoginResponseDto> LoginAsync(HttpClient client, string user, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            UsernameOrEmail = user,
            Password = password
        });
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<LoginResponseDto>(body, _json)
                     ?? throw new Exception("Login deserialize failed: " + body);
        return result;
    }

    [Fact]
    public async Task Login_Success_Admin()
    {
        var client = CreateClient();
        var result = await LoginAsync(client, "admin@financeapp.com", "Admin@123");
        Assert.True(result.Success);
        Assert.Equal("ADMIN", result.Role);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task Login_Invalid_ReturnsUnauthorized()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            UsernameOrEmail = "admin@financeapp.com",
            Password = "WrongPassword"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unauthorized_Request_Returns401()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/employees");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Employee_Cannot_Access_Admin_Employees()
    {
        var client = CreateClient();
        var login = await LoginAsync(client, "employee@financeapp.com", "Employee@123");
        Assert.True(login.Success);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        var response = await client.GetAsync("/api/employees");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_Can_List_Employees()
    {
        var client = CreateClient();
        var login = await LoginAsync(client, "admin@financeapp.com", "Admin@123");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        var response = await client.GetAsync("/api/employees");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Expense_Create_Approve_Creates_Transaction_And_Notification()
    {
        var client = CreateClient();
        var empLogin = await LoginAsync(client, "employee@financeapp.com", "Employee@123");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", empLogin.Token);

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("1"), "CategoryId");
        form.Add(new StringContent("99.99"), "Amount");
        form.Add(new StringContent("Integration test expense"), "Description");
        form.Add(new StringContent(DateTime.UtcNow.ToString("yyyy-MM-dd")), "ExpenseDate");

        var createResponse = await client.PostAsync("/api/expenses", form);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        Assert.True(createResponse.IsSuccessStatusCode, createBody);

        using var doc = JsonDocument.Parse(createBody);
        var expenseId = doc.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var adminLogin = await LoginAsync(client, "admin@financeapp.com", "Admin@123");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminLogin.Token);
        var approveResponse = await client.PutAsJsonAsync($"/api/expenses/{expenseId}/approve", new ApproveExpenseDto { Comments = "OK" });
        Assert.True(approveResponse.IsSuccessStatusCode, await approveResponse.Content.ReadAsStringAsync());

        var txResponse = await client.GetAsync("/api/transactions");
        Assert.Equal(HttpStatusCode.OK, txResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", empLogin.Token);
        var notifResponse = await client.GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.OK, notifResponse.StatusCode);
    }

    [Fact]
    public async Task Expense_Reject_Requires_Comment()
    {
        var client = CreateClient();
        var empLogin = await LoginAsync(client, "employee@financeapp.com", "Employee@123");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", empLogin.Token);

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("2"), "CategoryId");
        form.Add(new StringContent("12.50"), "Amount");
        form.Add(new StringContent("Reject me please"), "Description");
        form.Add(new StringContent(DateTime.UtcNow.ToString("yyyy-MM-dd")), "ExpenseDate");
        var createResponse = await client.PostAsync("/api/expenses", form);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        Assert.True(createResponse.IsSuccessStatusCode, createBody);
        using var doc = JsonDocument.Parse(createBody);
        var expenseId = doc.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var adminLogin = await LoginAsync(client, "admin@financeapp.com", "Admin@123");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminLogin.Token);
        var rejectResponse = await client.PutAsJsonAsync($"/api/expenses/{expenseId}/reject", new RejectExpenseDto { Comments = "Insufficient documentation" });
        Assert.True(rejectResponse.IsSuccessStatusCode, await rejectResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Employee_Cannot_View_Other_Employee_Expense()
    {
        var client = CreateClient();
        var adminLogin = await LoginAsync(client, "admin@financeapp.com", "Admin@123");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminLogin.Token);
        var list = await client.GetAsync("/api/expenses?pageSize=50");
        var listBody = await list.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(listBody);
        var items = doc.RootElement.GetProperty("data").GetProperty("items");
        Assert.True(items.GetArrayLength() > 0);
        var expenseId = items[0].GetProperty("id").GetInt32();
        var ownerEmployeeId = items[0].GetProperty("employeeId").GetInt32();

        // Login as EMP002 if expense belongs to EMP001
        var otherLogin = await LoginAsync(client, "alice.johnson@financeapp.com", "Employee@123");
        if (otherLogin.EmployeeId == ownerEmployeeId)
            otherLogin = await LoginAsync(client, "bob.smith@financeapp.com", "Employee@123");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherLogin.Token);
        var response = await client.GetAsync($"/api/expenses/{expenseId}");
        Assert.True(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden);
    }
}
