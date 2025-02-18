using Newtonsoft.Json;
using MultiTenantEmployeeAPI.Data;
using MultiTenantEmployeeAPI.Models;
using MultiTenantEmployeeAPI.Services.Models;

namespace MultiTenantEmployeeAPI.Services;

public class EmployeeSyncService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmployeeSyncService> _logger;
    private Timer _timer;

    private readonly string _dataFilePath;

    public EmployeeSyncService(IServiceScopeFactory scopeFactory,
        ILogger<EmployeeSyncService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _dataFilePath = Path.GetFullPath(configuration["DataFilePath"]); 
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Employee Sync Service Has been started.");

        _timer = new Timer(async state => await SyncEmployeeDepartments(), null, TimeSpan.Zero, TimeSpan.FromMinutes(30));

        return Task.CompletedTask;
    }


    private async Task SyncEmployeeDepartments()
    {
        using var scope = _scopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _logger.LogInformation("Employee and Departments Sync Started.");

        if (!File.Exists(_dataFilePath))
        {
            _logger.LogWarning($"Employee file not found: {_dataFilePath}. A new file will be created at {_dataFilePath}...");

            var defaultJson = "[]"; 
            await File.WriteAllTextAsync(_dataFilePath, defaultJson);

            _logger.LogInformation($"New employees.json file created: {_dataFilePath}");
            return;
        }
        
        // JSON dosyasından veri oku
        var jsonData = File.ReadAllText(_dataFilePath);

        var employeeDepartments = JsonConvert.DeserializeObject<List<EmployeeDepartmentSyncModel>>(jsonData);

        if (employeeDepartments == null || !employeeDepartments.Any())
        {
            _logger.LogWarning("New employee-department bonding is not found");
            return;
        }

        foreach (var empDept in employeeDepartments)
        {
            var employee = _context.Employees.FirstOrDefault(e => e.Id == empDept.EmployeeId);
            var department = _context.Departments.FirstOrDefault(d => d.Id == empDept.DepartmentId);

            if (employee == null || department == null)
            {
                _logger.LogWarning($"Employee or Department is not found: EmployeeId={empDept.EmployeeId}, DepartmentId={empDept.DepartmentId}");
                continue;
            }

            var existingRelation = _context.EmployeeDepartments
                .FirstOrDefault(ed => ed.EmployeeId == empDept.EmployeeId && ed.DepartmentId == empDept.DepartmentId);

            if (existingRelation == null)
            {
                _context.EmployeeDepartments.Add(new EmployeeDepartment
                {
                    EmployeeId = empDept.EmployeeId,
                    DepartmentId = empDept.DepartmentId
                });

                _logger.LogInformation($"A new relation added: EmployeeId={empDept.EmployeeId}, DepartmentId={empDept.DepartmentId}");
            }
        }

        _context.SaveChanges();
        _logger.LogInformation("Employee and Departments Sync completed.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Employee Sync Service has been stopped.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
