using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace App.API.Data;

/// <summary>
/// Sadece "dotnet ef migrations add" komutu çalışırken kullanılır.
/// Buradaki bağlantı dizesi gerçek bir DB'ye bağlanmaz; yalnızca sağlayıcıyı
/// (PostgreSQL + pgvector) bildirir. Uygulama çalışırken bu sınıf kullanılmaz.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=appdb;Username=postgres;Password=postgres",
                o => o.UseVector())
            .Options;

        return new AppDbContext(options);
    }
}
