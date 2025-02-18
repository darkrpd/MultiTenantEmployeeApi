using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantEmployeeAPI.Data;
using MultiTenantEmployeeAPI.Dtos;
using MultiTenantEmployeeAPI.Models;

[Route("api/employees")]
[ApiController]
public class EmployeesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EmployeesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetEmployees([FromQuery] int tenantId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (tenantId <= 0) return BadRequest("Tenant id must be filled.");

        var query = _context.Employees
            .Where(e => e.TenantId == tenantId)
            .Include(e => e.EmployeeDepartments)
                .ThenInclude(ed => ed.Department)
            .AsNoTracking();

        var totalEmployees = await query.CountAsync();
        var employees = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new
        {
            TotalCount = totalEmployees,
            Page = page,
            PageSize = pageSize,
            Employees = employees.Select(e => new
            {
                e.Id,
                e.Name,
                e.Email,
                Departments = e.EmployeeDepartments.Select(ed => new { ed.Department.Id, ed.Department.DepartmentName })
            })
        };

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddEmployee([FromBody] AddEmployeeRequest addEmployee)
    {
        if (addEmployee.TenantId == 0 ||
            string.IsNullOrEmpty(addEmployee.Email) ||
            string.IsNullOrEmpty(addEmployee.Name))
            return BadRequest("Corrupted employee data. Please fill out all fields.");

        var employee = new Employee
        {
            TenantId = addEmployee.TenantId,
            Email = addEmployee.Email,
            Name = addEmployee.Name
        };
        
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return Ok(employee.Id);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeRequest updatedEmployee)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null) return NotFound("Employee not found.");

        employee.Name = updatedEmployee.Name;
        employee.Email = updatedEmployee.Email;
        employee.TenantId = updatedEmployee.TenantId;

        await _context.SaveChangesAsync();
        return Ok(employee);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null) return NotFound("Employee not found.");

        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();

        return Ok("Employee deleted.");
    }
}
