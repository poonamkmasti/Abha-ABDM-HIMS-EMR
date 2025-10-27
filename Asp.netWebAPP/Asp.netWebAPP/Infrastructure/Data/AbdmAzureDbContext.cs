using Asp.netWebAPP.Core.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace Asp.netWebAPP.Infrastructure.Data
{
    public class AbdmAzureDbContext: DbContext
    {
        public AbdmAzureDbContext(DbContextOptions<AbdmAzureDbContext> options) : base(options)
        {
            
        }
        //new
        public DbSet<AbdmCallbackLog> AbdmCallbackLogs { get; set; }
        public DbSet<AbdmPatientLinkToken> AbdmPatientLinkTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           
            modelBuilder.Entity<AbdmCallbackLog>()
            .ToTable("AbdmCallbackLog");

            modelBuilder.Entity<AbdmPatientLinkToken>()
                .ToTable("AbdmPatientLinkToken");



        }
    }
}
