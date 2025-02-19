using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantEmployeeAPI.Data;
using MultiTenantEmployeeAPI.Models;
using MultiTenantEmployeeAPI.Dtos;

namespace MultiTenantEmployeeAPI.Tests;

public class EmployeesControllerTests
{
    private readonly ApplicationDbContext _context;
    private readonly EmployeesController _controller;

    public EmployeesControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
        _context = new ApplicationDbContext(options);
        _controller = new EmployeesController(_context);
    }

    [Fact]
    public async Task GetEmployees_ReturnsOkResult()
    {
        // Arrange
        _context.Employees.Add(new Employee { Id = 1, Name = "Ali Aşkın", Email = "ali@test.com", TenantId = 1 });
        _context.SaveChanges();

        // Act
        var result = await _controller.GetEmployees(tenantId: 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var employees = Assert.IsAssignableFrom<object>(okResult.Value);
        Assert.NotNull(employees);
    }

    [Fact]
    public async Task AddEmployee_AdminUser_CreatesEmployee()
    {
        // Arrange
        var employee = new AddEmployeeRequest { Name = "Test User", Email = "test@test.com", TenantId = 1 };

        // Act
        var result = await _controller.AddEmployee(employee);

        // Assert
        var createdAtActionResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<int>(createdAtActionResult.Value);
    }

    [Fact]
    public async Task DeleteEmployee_AdminUser_DeletesEmployee()
    {
        // Arrange
        var employee = new Employee { Id = 3, Name = "Delete Me", Email = "delete@test.com", TenantId = 1 };
        _context.Employees.Add(employee);
        _context.SaveChanges();

        // Act
        var result = await _controller.DeleteEmployee(3);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Employee deleted.", okResult.Value);
    }
}
