using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MultiTenantEmployeeAPI.Dtos;

public class RegisterRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
    
    [JsonConverter(typeof(StringEnumConverter))]
    public Role Role { get; set; }
}

public enum Role { 
    Admin,
    User
};
