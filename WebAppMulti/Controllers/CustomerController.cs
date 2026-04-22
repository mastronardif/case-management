using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppMulti.Database;
using WebAppMulti.Database.Models;

namespace WebAppMulti.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]

    public class CustomerController  : ControllerBase
    {
        private readonly CustomerService _service;

        public CustomerController(CustomerService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<DimCustomer>>> GetAll()
            => await _service.GetAllAsync();

        [HttpGet("summary")]
        public async Task<ActionResult<List<dynamic>>> GetAllLessFields()
            => await _service.GetAllLessFieldsAsync();


        [HttpGet("{id}")]
        public async Task<ActionResult<DimCustomer>> GetById(int id)
        {
            var customer = await _service.GetByIdAsync(id);
            return customer == null ? NotFound() : Ok(customer);
        }

        [HttpPost]
        public async Task<ActionResult<DimCustomer>> Create(DimCustomer customer)
        {
            var created = await _service.CreateAsync(customer);
            return CreatedAtAction(nameof(GetById), new { id = created.CustomerKey }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, DimCustomer customer)
        {
            var ok = await _service.UpdateAsync(id, customer);
            return ok ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }



    }
}
