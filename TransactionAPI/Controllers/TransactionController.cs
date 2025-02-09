using Microsoft.AspNetCore.Mvc;
using TransactionAPI.Models;
using TransactionAPI.Services;

namespace TransactionAPI.Controllers
{
    [ApiController]
    [Route("api/submittrxmessage")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpPost]
        public ActionResult<TransactionResponse> SubmitTransaction([FromBody] TransactionRequest request)
        {
            if (string.IsNullOrEmpty(request.PartnerRefNo))
            {
                return BadRequest(new TransactionResponse { Result = 0, ResultMessage = "PartnerRefNo is Required." });
            }
            if (request.TotalAmount <= 0)
            {
                return BadRequest(new TransactionResponse { Result = 0, ResultMessage = "Invalid Total Amount." });
            }

            var response = _transactionService.ProcessTransaction(request);

            if (response.Result == 0)
            {
                return BadRequest(response); // Ensure errors return BadRequest
            }

            return Ok(response);
        }

    }
}