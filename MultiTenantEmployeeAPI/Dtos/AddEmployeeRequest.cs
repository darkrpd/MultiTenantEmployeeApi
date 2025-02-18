namespace MultiTenantEmployeeAPI.Dtos;

public class AddEmployeeRequest
{
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public int TenantId { get; set; }
}