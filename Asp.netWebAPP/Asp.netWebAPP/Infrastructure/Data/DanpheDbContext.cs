using Asp.netWebAPP.Core.Domain.Model;
using Microsoft.EntityFrameworkCore;


namespace Asp.netWebAPP.Infrastructure.Data
{
    public class DanpheDbContext : DbContext
    {
        public DanpheDbContext(DbContextOptions<DanpheDbContext> options) : base(options) { }
        
             public DbSet<PatientModel> Patient { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PatientModel>()
            .ToTable("PAT_Patient", t => t.HasTrigger("TRG_PAT_History_PatientName"));


            base.OnModelCreating(modelBuilder);
        }


    }  
    
}
