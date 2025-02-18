namespace MultiTenantEmployeeAPI.Models;

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public int TenantId { get; set; }

    public IList<EmployeeDepartment> EmployeeDepartments { get; set; }
}
