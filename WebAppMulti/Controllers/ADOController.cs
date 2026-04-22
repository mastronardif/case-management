using Microsoft.AspNetCore.Mvc;
using System.Data;
using WebAppMulti.Database.Repository;

namespace WebAppMulti.Controllers
{

    public class StoredProcRequest
    {
        public string Name { get; set; } = string.Empty;   // SP name
        public Dictionary<string, object>? Parameters { get; set; }  // key-value params
    }

    public class MultiStoredProcRequest
    {
        public List<StoredProcRequest> Procedures { get; set; } = new();
    }


    [ApiController]
    [Route("api/[controller]")]
    public class ADOController : Controller
    {
        private readonly GenericRepository _repo;
        private readonly DapperRepository _repoDapper;

        public ADOController(GenericRepository repo, DapperRepository repoDapper)
        {
            _repo = repo;
            _repoDapper = repoDapper;
        }



        /// <summary>
        /// Insert data using a stored procedure.
        /// Example body:
        /// {
        ///   "name": "InsertCustomer",
        ///   "parameters": {
        ///     "@FirstName": "John",
        ///     "@LastName": "Doe",
        ///     "@Email": "john.doe@email.com"
        ///   }
        /// }
        /// </summary>
        [HttpPost("insert")]
        public async Task<IActionResult> UpsertAsync([FromBody] StoredProcRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Stored procedure name is required.");

            try
            {
                // Normalize parameters to prevent null issues
                var parameters = request.Parameters ?? new Dictionary<string, object>();

                // Execute the stored procedure
                var resultTable = await Task.Run(() => _repo.ExecuteStoredProcedure(request.Name, parameters));

                // Convert DataTable to List<Dictionary<string, object>>
                var rows = resultTable.AsEnumerable()
                    .Select(r => resultTable.Columns.Cast<DataColumn>()
                        .ToDictionary(c => c.ColumnName, c => r[c]))
                    .ToList();

                return Ok(new
                {
                    Message = "Stored procedure executed successfully.",
                    StoredProcedure = request.Name,
                    Rows = resultTable.Rows.Count,
                    Data = rows
                });

                // Return a standard REST 201 Created response with metadata
                //return CreatedAtAction(nameof(UpsertAsync), new
                //{
                //    storedProcedure = request.Name,
                //    rowCount = rows.Count,
                //    status = rows.FirstOrDefault()?["ActionTaken"] ?? "Unknown",
                //    data = rows
                //});
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error executing stored procedure.",
                    storedProcedure = request.Name,
                    error = ex.Message
                });
            }
        }



        [HttpGet("by-id/{id}")]
        public IActionResult GetCustomerById(int id, [FromQuery] bool useDapper = false)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@Id", id }
            };

            if (useDapper)
            {
                // Use Dapper-based repo
                var result = _repoDapper.ExecuteStoredProcedure("[GetCustomerById]", parameters);
                return Ok(result);
            }
            else
            {
                // Use ADO-based repo
                var result = _repo.ExecuteStoredProcedure("[GetCustomerById]", parameters);

                var rows = result.AsEnumerable()
                     .Select(r => result.Columns.Cast<DataColumn>()
                         .ToDictionary(c => c.ColumnName, c => r[c]))
                     .ToList();

                return Ok(rows);
            }
        }

        [HttpPost("run")]
        public async Task<IActionResult> RunStoredProc([FromBody] StoredProcRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Name))
                return BadRequest("Stored procedure name is required.");

            var result = await _repoDapper.RunAsync(req.Name, req.Parameters);

            return Ok(result);
        }

        [HttpPost("run-multiple")]
        public async Task<IActionResult> RunMultiple([FromBody] List<StoredProcRequest> request, [FromQuery] bool parallel = false)
        {
            var list = request.Select(r => (r.Name, r.Parameters)).ToList();
            var result = await _repoDapper.RunProceduresAsync(list, parallel);

            return Ok(result);
        }

        [HttpPost("run-multiRS")]
        public async Task<IActionResult> RunStoredProcMulti([FromBody] StoredProcRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Name))
                return BadRequest("Stored procedure name is required.");

            var result = await _repoDapper.RunStoredProcMultiAsync(req.Name, req.Parameters);
            return Ok(result);
        }




        //[HttpPost("execute-multiple")]
        //public IActionResult ExecuteMultiple([FromBody] List<StoredProcRequest> request)
        //{
        //    var procedures = request
        //        .Select(r => (r.Name, r.Parameters))
        //        .ToList();

        //    var results = _repo.ExecuteMultipleStoredProcedures(procedures);

        //    return Ok(results);
        //}


        //[HttpPost("execute-multiple")]
        //public async Task<IActionResult> ExecuteMultipleAsync([FromBody] List<StoredProcRequest> request)
        //{
        //    if (request == null || request.Count == 0)
        //        return BadRequest("No stored procedures provided.");

        //    var procedures = request
        //        .Select(r => (r.Name, r.Parameters))
        //        .ToList();

        //    var results = await _repo.ExecuteMultipleStoredProceduresAsync(procedures);

        //    return Ok(results);
        //}


        [HttpPost("execute-multiple")]
        public async Task<IActionResult> ExecuteMultipleAsync(
        [FromBody] List<StoredProcRequest> request,
        [FromQuery] bool parallel = false)
        {

            if (request == null || request.Count == 0)
                return BadRequest("No stored procedures provided.");

            var procedures = request
                .Select(r => (r.Name, r.Parameters))
                .ToList();

            object results;

            if (parallel)
            {
                results = await _repo.ExecuteMultipleStoredProceduresParallelAsync(procedures);
            }
            else
            {
                results = await _repo.ExecuteMultipleStoredProceduresAsync(procedures);
            }

            return Ok(results);
        }


    }
}
