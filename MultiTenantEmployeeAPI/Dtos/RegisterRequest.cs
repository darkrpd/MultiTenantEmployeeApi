using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MultiTenantEmployeeAPI.Dtos;

public class RegisterRequest
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    
    [JsonConverter(typeof(StringEnumConverter))]
    public Role Role { get; set; }
}

public enum Role
{
    Admin = 1,
    User = 2
}

