// Services/CustomerService.cs
using WebAppMulti.Database;
using WebAppMulti.Database.Models;
using Microsoft.EntityFrameworkCore;
using MyDbContext = WebAppMulti.Database.MyDbContext;


public class CustomerService
{
    private readonly MyDbContext _context;
    //private readonly WebAppMulti.Database.MyDbContext _context;

    public CustomerService(MyDbContext context)
    {
        _context = context;
    }

    public async Task<List<DimCustomer>> GetAllAsync() =>
        await _context.DimCustomers.ToListAsync();

    public async Task<List<dynamic>> GetAllLessFieldsAsync() =>
        await _context.DimCustomers
            .Select(c => new {
                c.CustomerKey,
                //c.FirstName,
                //c.LastName
            })
            .Cast<dynamic>()
            .ToListAsync();


    public async Task<DimCustomer?> GetByIdAsync(int id) =>
        await _context.DimCustomers.FindAsync(id);

    public async Task<DimCustomer> CreateAsync(DimCustomer customer)
    {
        _context.DimCustomers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task<bool> UpdateAsync(int id, DimCustomer updated)
    {
        var existing = await _context.DimCustomers.FindAsync(id);
        if (existing == null) return false;

        _context.Entry(existing).CurrentValues.SetValues(updated);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var customer = await _context.DimCustomers.FindAsync(id);
        if (customer == null) return false;

        _context.DimCustomers.Remove(customer);
        await _context.SaveChangesAsync();
        return true;
    }
}
