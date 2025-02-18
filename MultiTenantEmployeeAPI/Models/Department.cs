namespace MultiTenantEmployeeAPI.Models;

public class Department
{
    public int Id { get; set; }
    public string DepartmentName { get; set; }
    public int TenantId { get; set; }

    public IList<EmployeeDepartment> EmployeeDepartments { get; set; }
}
