
using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Objects;

namespace NetworkMonitor.Data
{
    public class MonitorContext : DbContext
    {

        public MonitorContext(DbContextOptions<MonitorContext> options) : base(options)
        {
        }

        public DbSet<MonitorPingInfo> MonitorPingInfos { get; set; }
        public DbSet<PingInfo> PingInfos { get; set; }
        public DbSet<MonitorIP> MonitorIPs { get; set; }

        public DbSet<UserInfo> UserInfos { get; set; }
        public DbSet<LoadServer> LoadServers { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MonitorPingInfo>().ToTable("MonitorPingInfo");
            modelBuilder.Entity<MonitorIP>().ToTable("MonitorIP");
        }
    }
    
}
