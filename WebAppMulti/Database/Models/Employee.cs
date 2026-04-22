using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppMulti.Database.Models
{
    [Table("DimEmployee")]
    public class Employee
    {
        [Key]  // This tells EF Core it's the primary key
        public int EmployeeKey { get; set; }
        public string? DepartmentName { get; set; }
        public decimal BaseRate { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime HireDate { get; set; }
        public DateTime BirthDate { get; set; }
        public string? Title { get; set; }
        public string? EmailAddress { get; set; }
        public string? Phone { get; set; }
        public bool SalariedFlag { get; set; }
        public byte PayFrequency { get; set; }
        public short VacationHours { get; set; }
        public short SickLeaveHours { get; set; }
        public string? Status { get; set; }
    }
}
