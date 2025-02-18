using MultiTenantEmployeeAPI.Data;

namespace MultiTenantEmployeeAPI.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/employees")]
[ApiController]
[Authorize] // JWT Authentication zorunlu
public class EmployeesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EmployeesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetEmployees([FromQuery] int tenantId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (tenantId <= 0) return BadRequest("TenantId zorunludur.");

        var query = _context.Employees
            .Where(e => e.TenantId == tenantId)
            .Include(e => e.EmployeeDepartments)
            .ThenInclude(ed => ed.Department) // Tüm departmanları tek sorguda çekiyoruz
            .AsNoTracking(); // Performans için No Tracking kullanıyoruz

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
}
