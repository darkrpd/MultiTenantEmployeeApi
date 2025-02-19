using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MultiTenantEmployeeAPI.Controllers;
using MultiTenantEmployeeAPI.Data;
using MultiTenantEmployeeAPI.Dtos;
using MultiTenantEmployeeAPI.Models;

namespace MultiTenantEmployeeAPI.Tests;

public class AuthControllerTests
{
    private readonly ApplicationDbContext _context;
    private readonly AuthController _controller;
    private readonly IConfiguration _configuration;

    public AuthControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
        _context = new ApplicationDbContext(options);
        
        var configurationBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "JwtSettings:SecretKey", "supersecretkey1234567890aaaaaaaaaaaaaaaaa" },
                { "JwtSettings:Issuer", "test" },
                { "JwtSettings:Audience", "test" },
                { "JwtSettings:ExpirationMinutes", "60" }
            }!);

        _configuration = configurationBuilder.Build();

        _controller = new AuthController(_context, _configuration);
    }

    [Fact]
    public async Task Register_CreatesNewUser()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Password = "Test123!",
            Role = Role.User
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void Login_ValidUser_ReturnsToken()
    {
        // Arrange
        var user = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = 1
        };
        _context.Users.Add(user);
        _context.SaveChanges();

        var request = new LoginRequest
        {
            Username = "admin",
            Password = "Admin123!"
        };

        // Act
        var result = _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var token = okResult.Value.GetType().GetProperty("Token").GetValue(okResult.Value, null);
        Assert.NotNull(token);
    }
}
