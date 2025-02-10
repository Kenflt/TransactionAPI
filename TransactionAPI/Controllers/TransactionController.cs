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
//  "partnerkey": "FAKEGOOGLE",
//  "partnerrefno": "FG-00001",
//  "partnerpassword": "RkFLRVBBU1NXT1JEMTIzNA==",
//  "totalamount": 1000,
//  "items": [
//    {
//      "partneritemref": "i-00001",
//      "name": "Pen",
//      "qty": 4,
//      "unitprice": 200
//    },
//    {
//    "partneritemref": "i-00002",
//      "name": "Ruler",
//      "qty": 2,
//      "unitprice": 100
//    }
//  ],
//  "timestamp": "2025-02-10T10:26:22.0000000Z",
//  "sig": " MDE3ZTBkODg4ZDNhYzU0ZDBlZWRmNmU2NmUyOWRhZWU4Y2M1NzQ1OTIzZGRjYTc1ZGNjOTkwYzg2MWJlMDExMw=="
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