using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using WebAppMulti.Database.Configurations;
using WebAppMulti.Database.Models;

namespace WebAppMulti.Database;


public partial class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees { get; set; }
    //public DbSet<DimGeography> Geography { get; set; }

    //building simple CRUD and working manually with services and LINQ, lazy loading is likely not necessary
    public DbSet<DimGeography> DimGeographys{ get; set; }
    public DbSet<DimCustomer> DimCustomers { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        //base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MyDbContext).Assembly);

        //modelBuilder.ApplyConfiguration(new DimCustomerConfig());
        //modelBuilder.ApplyConfiguration(new EmployeeConfig());

        //modelBuilder.ApplyConfiguration(new CustomerConfig());
        //modelBuilder.ApplyConfiguration(new OrderConfig());
        //modelBuilder.ApplyConfiguration(new InventoryItemConfig());
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
