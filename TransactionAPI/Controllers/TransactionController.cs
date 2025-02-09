using Microsoft.AspNetCore.Mvc;
using TransactionAPI.Models;
using TransactionAPI.Services;

namespace TransactionAPI.Controllers
{
    [ApiController]
    [Route("api/transaction")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpPost("submit-transaction")]
        public ActionResult<TransactionResponse> SubmitTransaction([FromBody] TransactionRequest request)
        {
            if (IsExpired(request.Timestamp))
                return BadRequest(new TransactionResponse { Result = 0, ResultMessage = "Expired. Provided timestamp exceed server time +-5min" });

            var response = _transactionService.ProcessTransaction(request);
            return response.Result == 1 ? Ok(response) : BadRequest(response);
        }

        private bool IsExpired(string timestamp)
        {
            if (!DateTime.TryParse(timestamp, out DateTime requestTime))
                return true;

            return Math.Abs((DateTime.UtcNow - requestTime).TotalSeconds) > 300;
        }
    }
}