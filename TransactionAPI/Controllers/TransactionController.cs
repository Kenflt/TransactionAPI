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
    //C: \Users\kendr\source\repos\TransactionAPI\TransactionAPI\Dockerfile
    //8080:8080
    //docker start 17b76a7daede
    //docker stop 17b76a7daede
    //    {
    //    "PartnerKey": "FAKEGOOGLE",
    //    "PartnerPassword": "RkFLRVBBU1NXT1JEMTIzNA==",
    //    "PartnerRefNo": "FG-00001",
    //    "TotalAmount": 1000,
    //    "Timestamp": "2025-02-09T12:00:00Z",
    //    "Items": [
    //        {
    //            "PartnerItemRef": "i-00001",
    //            "Name": "Item1",
    //            "Qty": 1,
    //            "UnitPrice": 1000
    //        }
    //    ],
    //    "Sig": "validsignature"
    //}

    //using Postman http://localhost:8080/api/transaction/submit-transaction
    //Header: Content-Type application/json

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