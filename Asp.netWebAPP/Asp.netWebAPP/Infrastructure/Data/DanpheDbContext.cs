using Asp.netWebAPP.Core.Domain.Model;
using Microsoft.EntityFrameworkCore;


namespace Asp.netWebAPP.Infrastructure.Data
{
    public class DanpheDbContext : DbContext
    {
        public DanpheDbContext(DbContextOptions<DanpheDbContext> options) : base(options) { }
        
        public DbSet<PatientModel> Patient { get; set; }
        public DbSet<EmployeeModel> Employees { get; set; }
        public DbSet<PHRMPrescriptionModel> PHRMPrescription { get; set; }
        public DbSet<PHRMPrescriptionItemModel> PHRMPrescriptionItems { get; set; }
        public DbSet<PHRMItemMasterModel> PHRMItemMaster { get; set; }
        public DbSet<CountryModel> Country { get; set; }
        public DbSet<CountrySubDivisionModel> CountrySubDivision { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Map PatientModel to the real table name in DB
            modelBuilder.Entity<PatientModel>().ToTable("PAT_Patient");
            modelBuilder.Entity<EmployeeModel>().ToTable("EMP_Employee");
            modelBuilder.Entity<PHRMPrescriptionModel>().ToTable("PHRM_Prescription");
            modelBuilder.Entity<PHRMPrescriptionItemModel>().ToTable("PHRM_PrescriptionItems");
            modelBuilder.Entity<PHRMItemMasterModel>().ToTable("PHRM_MST_Item");
            modelBuilder.Entity<CountryModel>().ToTable("MST_Country");
            modelBuilder.Entity<CountrySubDivisionModel>().ToTable("MST_CountrySubDivision");

            base.OnModelCreating(modelBuilder);
        }


    }  
    
}
