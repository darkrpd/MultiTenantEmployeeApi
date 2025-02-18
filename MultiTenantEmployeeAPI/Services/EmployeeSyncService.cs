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
        _dataFilePath = Path.GetFullPath(configuration["DataFilePath"]); // Root dizininden al
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Employee Sync Service Başlatıldı.");

        // Timer içinde async metod çağırmak için bir wrapper kullanıyoruz
        _timer = new Timer(async state => await SyncEmployeeDepartments(), null, TimeSpan.Zero, TimeSpan.FromMinutes(30));

        return Task.CompletedTask;
    }


    private async Task SyncEmployeeDepartments()
    {
        using var scope = _scopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _logger.LogInformation("Çalışan ve Departman Senkronizasyonu Başlatıldı...");

        if (!File.Exists(_dataFilePath))
        {
            _logger.LogWarning($"Veri dosyası bulunamadı: {_dataFilePath}. Yeni dosya oluşturuluyor...");

            // Varsayılan boş JSON içeriği
            var defaultJson = "[]"; // Boş bir liste
            await File.WriteAllTextAsync(_dataFilePath, defaultJson);

            _logger.LogInformation($"Yeni JSON dosyası oluşturuldu: {_dataFilePath}");
            return; // İlk çalıştırmada dosya boş olduğu için işlemi bitiriyoruz.
        }
        
        // JSON dosyasından veri oku
        var jsonData = File.ReadAllText(_dataFilePath);

        var employeeDepartments = JsonConvert.DeserializeObject<List<EmployeeDepartmentSyncModel>>(jsonData);

        if (employeeDepartments == null || !employeeDepartments.Any())
        {
            _logger.LogWarning("Güncellenecek çalışan-departman ilişkisi bulunamadı.");
            return;
        }

        foreach (var empDept in employeeDepartments)
        {
            var employee = _context.Employees.FirstOrDefault(e => e.Id == empDept.EmployeeId);
            var department = _context.Departments.FirstOrDefault(d => d.Id == empDept.DepartmentId);

            if (employee == null || department == null)
            {
                _logger.LogWarning($"Çalışan veya Departman bulunamadı: EmployeeId={empDept.EmployeeId}, DepartmentId={empDept.DepartmentId}");
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

                _logger.LogInformation($"Yeni ilişki eklendi: EmployeeId={empDept.EmployeeId}, DepartmentId={empDept.DepartmentId}");
            }
        }

        _context.SaveChanges();
        _logger.LogInformation("Çalışan ve Departman Senkronizasyonu Tamamlandı.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Employee Sync Service Durduruldu.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
