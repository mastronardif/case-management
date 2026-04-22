using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppMulti.Database;
using WebAppMulti.Database.Models;
using WebAppMulti.Helpers;
using WebAppMulti.Services;

namespace WebAppMulti.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class EFdbController : ControllerBase
    {
        private readonly EFdbServie _service;

        public EFdbController(EFdbServie service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<DimCustomer>>> GetAll()
            => await _service.GetAllAsync();

        [HttpGet("queryable")]
        public IActionResult GetQueryable([FromQuery] string lastName)
        {
            // Returns query (executed by serializer when enumerated)
            var customers = _service.GetCustomersQueryable(lastName);
            return Ok(customers);
        }

        [HttpGet("enumerable")]
        public IActionResult GetEnumerable([FromQuery] string lastName)
        {
            // Already executed in service
            var customers = _service.GetCustomersEnumerable(lastName);
            return Ok(customers);
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecent([FromQuery] DateTime afterDate)
        {
            var customers = await _service.GetRecentCustomersAsync(afterDate);
            return Ok(customers);
        }

        [HttpGet("AllEmployees")]
        public async Task<ActionResult> GetAllEmployees()
        {
            var vvv = await _service.GetAllLessFieldsAsync();
            return Ok(vvv);
        }

        //[HttpGet ("GetCustomersWithGeography")]
        //public async Task<IActionResult> GetCustomers()
        //{
        //    var customers = await _service.GetCustomersWithGeographyAsync();
        //    return Ok(customers);
        //}

        [HttpGet("customers-with-geoEnumrable")]
        public async Task<IActionResult> GetCustomersWithGeographyE()
        {

            string name = "Frank";
            bool result = name.IsCapitalized(); // feels like an instance method!
            var fff = name.Capitalize();

            var customers = await _service.GetCustomersWithGeographyEnumrableAsync();
            //return Ok(customers);
            return Ok(new { data = customers });

        }

        [HttpGet("customers-with-geo")]
        public async Task<IActionResult> GetCustomersWithGeography()
        {
            var customers = await _service.GetCustomersWithGeographyAsync();
            return Ok(customers);
        }
    }
}
