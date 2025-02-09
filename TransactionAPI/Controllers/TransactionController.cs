using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TransactionAPI.Models;
using TransactionAPI.Services;
using System.Text.Json;
using log4net;

namespace TransactionAPI.Controllers
{
    [ApiController]
    [Route("api/transaction")]
    public class TransactionController : ControllerBase
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(TransactionController));
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpPost("submit-transaction")]
        public ActionResult<TransactionResponse> SubmitTransaction([FromBody] TransactionRequest request)
        {
            if (request == null)
            {
                _logger.Error("Received null request.");
                return BadRequest(new TransactionResponse { Result = 0, ResultMessage = "Invalid request." });
            }

            // Log Request Body
            var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });
            _logger.Info($"📤 REQUEST:\n{requestJson}");

            var response = _transactionService.ProcessTransaction(request);

            // Log Response Body
            var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            _logger.Info($"📥 RESPONSE:\n{responseJson}");

            if (request == null)
            {
                return BadRequest(new TransactionResponse { Result = 0, ResultMessage = "Invalid request." });
            }

            return response.Result == 1 ? Ok(response) : BadRequest(response);
        }
    }
}