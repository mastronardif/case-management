using Microsoft.EntityFrameworkCore;
using WebAppMulti.Database;
using WebAppMulti.Database.Dtos;
using WebAppMulti.Database.Models;

namespace WebAppMulti.Services
{


    public class EFdbServie
    {
        private readonly MyDbContext _context;
        public EFdbServie(MyDbContext context)
        {
            _context = context;
        }

        public async Task<List<DimCustomer>> GetAllAsync() =>
            await _context.DimCustomers.ToListAsync();

        // IQueryable - query is built but not executed until enumerated
        public IQueryable<DimCustomer> GetCustomersQueryable(string lastNameFilter)
        {
            return _context.DimCustomers
                           .Where(c => c.LastName.Contains(lastNameFilter));
        }

        // IEnumerable - materializes results into memory immediately
        public IEnumerable<DimCustomer> GetCustomersEnumerable(string lastNameFilter)
        {
            return _context.DimCustomers
                           .Where(c => c.LastName.Contains(lastNameFilter))
                           .ToList();
        }

        // Example: return top N customers born after a date
        public async Task<List<DimCustomer>> GetRecentCustomersAsync(DateTime afterDate)
        {
            return await _context.DimCustomers
                .Where(c => c.BirthDate.HasValue && c.BirthDate.Value.CompareTo(afterDate) > 0)
                .OrderBy(c => c.BirthDate)
                .Take(10)
                .ToListAsync();

        }

        //public async Task<List<DimCustomer>?> GetCustomersWithGeographyAsync()
        //{
        //    //return null;
        //    return await _context.DimCustomers
        //                         .Include(c => c.GeographyKeyNavigation)
        //                         .ToListAsync();
        //}

        public async Task<List<CustomerWithGeoDto>> GetCustomersWithGeographyAsync()
        {
            return await _context.DimCustomers
                .GroupBy(c => new { c.FirstName, c.LastName }) // group by names
                .Select(g => g
                    .OrderBy(c => c.CustomerKey) // pick first by CustomerKey
                    .Select(c => new CustomerWithGeoDto
                    {
                        CustomerKey = c.CustomerKey,
                        FirstName = c.FirstName,
                        LastName = c.LastName,
                        EmailAddress = c.EmailAddress,
                        Phone = c.Phone,
                        City = c.GeographyKeyNavigation.City,
                        StateProvinceCode = c.GeographyKeyNavigation.StateProvinceCode,
                        CountryRegionCode = c.GeographyKeyNavigation.CountryRegionCode
                    })
                    .FirstOrDefault()
                )
                .ToListAsync();
        }


        public async Task<List<CustomerWithGeoDto>> GetCustomersWithGeographyEnumrableAsync()
        {
            return await Task.Run(() =>
                _context.DimCustomers
                    .Include(c => c.GeographyKeyNavigation)
                    .Select(c => new CustomerWithGeoDto
                    {
                        CustomerKey = c.CustomerKey,
                        FirstName = c.FirstName,
                        LastName = c.LastName,
                        EmailAddress = c.EmailAddress,
                        Phone = c.Phone,
                        City = c.GeographyKeyNavigation.City,
                        StateProvinceCode = c.GeographyKeyNavigation.StateProvinceCode,
                        CountryRegionCode = c.GeographyKeyNavigation.CountryRegionCode
                    })
                    .AsEnumerable() // switch to in-memory LINQ
                    .GroupBy(c => new { c.FirstName, c.LastName })
                    .Select(g => g.OrderBy(c => c.CustomerKey).First())
                    .OrderBy(c => c.FirstName)
                    .Take(10)
                    .ToList()
            );
        }



        public async Task<List<CustomerWithGeoDto>> GetCustomersWithGeography11Async()
        {
            // Step 1: Get the CustomerKeys of the "first" per name
            var firstPerNameKeys = await _context.DimCustomers
                .GroupBy(c => new { c.FirstName })
                .Select(g => g.OrderBy(c => c.CustomerKey).Select(c => c.CustomerKey).FirstOrDefault())
                .ToListAsync();

            // Step 2: Load those customers with geography into DTOs
            var customers = await _context.DimCustomers
                .Where(c => firstPerNameKeys.Contains(c.CustomerKey))
                .Include(c => c.GeographyKeyNavigation)
                .Select(c => new CustomerWithGeoDto
                {
                    CustomerKey = c.CustomerKey,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    EmailAddress = c.EmailAddress,
                    Phone = c.Phone,
                    City = c.GeographyKeyNavigation.City,
                    StateProvinceCode = c.GeographyKeyNavigation.StateProvinceCode,
                    CountryRegionCode = c.GeographyKeyNavigation.CountryRegionCode
                })
                .OrderBy(c => c.FirstName)
                .Take(10) // optional
                .ToListAsync();

            return customers;
            //return await _context.DimCustomers
            //    .Include(c => c.GeographyKeyNavigation) // navigation property
            //    .Select(c => new CustomerWithGeoDto
            //    {
            //        CustomerKey = c.CustomerKey,
            //        FirstName = c.FirstName,
            //        LastName = c.LastName,
            //        EmailAddress = c.EmailAddress,
            //        Phone = c.Phone,
            //        City = c.GeographyKeyNavigation.City,
            //        StateProvinceCode = c.GeographyKeyNavigation.StateProvinceCode,
            //        CountryRegionCode = c.GeographyKeyNavigation.CountryRegionCode
            //    }).OrderByDescending(c => c.FirstName)
            //    .Take(10)
            //    .ToListAsync();
        }





        public async Task<List<dynamic>> GetAllLessFieldsAsync() =>
                    await _context.Employees
                        .Select(e => new
                        {
                            e.EmployeeKey,
                            //e.FirstName,
                            //e.LastName
                        })
                        .Cast<dynamic>()
                        .ToListAsync();

        //        public async Task<Employee?> GetByIdAsync(int id) =>
        //            await _context.Employees.FindAsync(id);
        //        public async Task<Employee> CreateAsync(Employee employee)
        //        {
        //            _context.Employees.Add(employee);
        //            await _context.SaveChangesAsync();
        //            return employee;
        //        }
        //        public async Task<bool> UpdateAsync(int id, Employee updated)
        //        {
        //            var existing = await _context.Employees.FindAsync(id);
        //            if (existing == null) return false;
        //            _context.Entry(existing).CurrentValues.SetValues(updated);
        //            await _context.SaveChangesAsync();
        //            return true;
        //        }
        //        public async Task<bool> DeleteAsync(int id)
        //        {
        //            var employee = await _context.Employees.FindAsync(id);
        //            if (employee == null) return false;
        //            _context.Employees.Remove(employee);
        //            await _context.SaveChangesAsync();
        //            return true;
        //        }   

    }
}
/*******
 * 
| Feature     | `IQueryable`                   | `IEnumerable`                              |
| ----------- | ------------------------------ | ------------------------------------------ |
| Execution   | Deferred until enumerated      | Immediate (after `ToList()`)               |
| Filtering   | Done **in the database** (SQL) | Done **in memory** (C#)                    |
| Performance | More efficient for large sets  | Good for small sets or already-loaded data |
| Use Case    | Composing queries              | Working with materialized collections      |

 * 
 * *****/