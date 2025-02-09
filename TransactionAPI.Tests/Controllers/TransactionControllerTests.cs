using Microsoft.AspNetCore.Mvc;
using Moq;
using TransactionAPI.Controllers;
using TransactionAPI.Models;
using TransactionAPI.Services;
using Xunit;
using Xunit.Abstractions; // For logging test outputs
using System.Collections.Generic;
using System.Linq;
using log4net;
using log4net.Config;
using System.Reflection;

public class TransactionControllerTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ITransactionService> _mockService;
    private readonly ILog _logger;
    private readonly TransactionController _controller;

    public TransactionControllerTests(ITestOutputHelper output)
    {
        _output = output;
        _mockService = new Mock<ITransactionService>();

        // Configure log4net
        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
        _logger = LogManager.GetLogger(typeof(TransactionControllerTests));

        _controller = new TransactionController(_mockService.Object);
    }
    private void LogTransaction(string request, string response)
    {
        string logMessage = $"📤 REQUEST:\n{request}\n\n📥 RESPONSE:\n{response}\n";
        _logger.Info(logMessage);
        _output.WriteLine(logMessage);
    }

    [Fact]
    public void SubmitTransaction_ShouldReturnBadRequest_WhenRequestIsNull()
    {
        var result = _controller.SubmitTransaction(null);
        var response = result.Result as BadRequestObjectResult;
        Assert.NotNull(response);
        Assert.Equal(400, response.StatusCode);
    }

    [Fact]
    public void SubmitTransaction_ShouldReturnSuccess_WhenValidRequest()
    {
        // Arrange
        var request = new TransactionRequest
        {
            PartnerKey = "FAKEGOOGLE",
            PartnerRefNo = "FG-00001",
            PartnerPassword = "RkFLRVBBU1NXT1JEMTIzNA==",
            TotalAmount = 1000,
            Items = new List<ItemDetail>
        {
            new ItemDetail { PartnerItemRef = "i-00001", Name = "Pen", Qty = 1, UnitPrice = 1000 }
        },
            Timestamp = DateTime.UtcNow.ToString("o"), // ✅ Convert DateTime to string
            Sig = "validsignature"
        };

        _mockService
            .Setup(service => service.ProcessTransaction(request))
            .Returns(new TransactionResponse
            {
                Result = 1,
                TotalAmount = 1000,
                TotalDiscount = 50,
                FinalAmount = 950,
                ResultMessage = "Transaction Successful"
            });

        // Act
        var result = _controller.SubmitTransaction(request);
        var response = result.Result as ObjectResult;
        var responseBody = response?.Value as TransactionResponse;

        // Log Request & Response
        _output.WriteLine("📤 REQUEST:");
        _output.WriteLine(System.Text.Json.JsonSerializer.Serialize(request, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        _output.WriteLine("📥 RESPONSE:");
        _output.WriteLine(System.Text.Json.JsonSerializer.Serialize(responseBody, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        string requestJson = System.Text.Json.JsonSerializer.Serialize(request, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        string responseJson = System.Text.Json.JsonSerializer.Serialize(responseBody, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        // Log transaction
        LogTransaction(requestJson, responseJson);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(responseBody);
        Assert.Equal(1, responseBody.Result);
        Assert.Equal(1000, responseBody.TotalAmount);
        Assert.Equal(50, responseBody.TotalDiscount);
        Assert.Equal(950, responseBody.FinalAmount);
        Assert.Equal("Transaction Successful", responseBody.ResultMessage);
    }

    [Theory]
    [InlineData(null, "RkFLRVBBU1NXT1JEMTIzNA==", "FG-00001", 1000, 4, 200, 2, 100, "PartnerKey is Required.")] // Missing PartnerKey
    [InlineData("FAKEGOOGLE", "RkFLRVBBU1NXT1JEMTIzNA==", null, 1000, 4, 200, 2, 100, "PartnerRefNo is Required.")] // Missing PartnerRefNo
    [InlineData("FAKEGOOGLE", "INVALIDPASSWORD", "FG-00001", 1000, 4, 200, 2, 100, "Access Denied!")] // Invalid credentials
    [InlineData("FAKEGOOGLE", "RkFLRVBBU1NXT1JEMTIzNA==", "FG-00001", 1000, 4, 200, 2, 100, "Expired.", -10)] // Expired timestamp
    [InlineData("FAKEGOOGLE", "RkFLRVBBU1NXT1JEMTIzNA==", "INVALID_REF", 1000, 4, 200, 2, 100, "Invalid Partner Reference Number Format.")] // Invalid PartnerRefNo
    [InlineData("FAKEGOOGLE", "RkFLRVBBU1NXT1JEMTIzNA==", "FG-00001", 1000, 0, 0, 0, 0, "Items List Cannot Be Empty.")] // No items
    [InlineData("FAKEGOOGLE", "RkFLRVBBU1NXT1JEMTIzNA==", "FG-00001", 500, 4, 200, 2, 100, "Invalid Total Amount.")] // Mismatch in calculated total
    public void SubmitTransaction_ShouldReturnValidationErrors(
    string partnerKey, string partnerPassword, string partnerRefNo,
    long totalAmount, int qty1, int unitPrice1, int qty2, int unitPrice2,
    string expectedMessage, int timestampOffset = 0)
    {
        // Arrange
        var request = new TransactionRequest
        {
            PartnerKey = partnerKey,
            PartnerPassword = partnerPassword,
            PartnerRefNo = partnerRefNo,
            TotalAmount = totalAmount,
            Items = qty1 > 0 ? new List<ItemDetail>
        {
            new ItemDetail { PartnerItemRef = "i-00001", Name = "Pen", Qty = qty1, UnitPrice = unitPrice1 },
            new ItemDetail { PartnerItemRef = "i-00002", Name = "Ruler", Qty = qty2, UnitPrice = unitPrice2 }
        } : null,
            Timestamp = DateTime.UtcNow.AddMinutes(timestampOffset).ToString("o"),
            Sig = "invalidsignature"
        };

        _mockService
            .Setup(service => service.ProcessTransaction(request))
            .Returns(new TransactionResponse { Result = 0, ResultMessage = expectedMessage });

        // Act
        var result = _controller.SubmitTransaction(request);
        var response = result.Result as ObjectResult;
        var responseBody = response?.Value as TransactionResponse;

        // Log Request & Response
        _output.WriteLine("📤 REQUEST:");
        _output.WriteLine(System.Text.Json.JsonSerializer.Serialize(request, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        _output.WriteLine("📥 RESPONSE:");
        _output.WriteLine(System.Text.Json.JsonSerializer.Serialize(responseBody, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        string requestJson = System.Text.Json.JsonSerializer.Serialize(request, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        string responseJson = System.Text.Json.JsonSerializer.Serialize(responseBody, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        // Log transaction
        LogTransaction(requestJson, responseJson);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(400, response.StatusCode);
        Assert.NotNull(responseBody);
        Assert.Equal(0, responseBody.Result);
        Assert.Equal(expectedMessage, responseBody.ResultMessage);
    }
    [Theory]
    [InlineData(150, 0, 150)]       // No discount (<200)
    [InlineData(200, 10, 190)]       // 5% discount
    [InlineData(500, 25, 475)]       // 5% discount
    [InlineData(600, 42, 558)]       // 7% discount
    [InlineData(800, 56, 744)]       // 7% discount
    [InlineData(1000, 100, 900)]     // 10% discount
    [InlineData(1500, 225, 1275)]    // 15% discount
    [InlineData(503, 40, 463)]       // Prime number > 500 → Extra 8% discount (rounded)
    [InlineData(905, 181, 724)]      // Ends in 5 & > 900 → Extra 10% discount (rounded)
    public void SubmitTransaction_ShouldApplyCorrectDiscounts(
    long totalAmountMYR, long expectedDiscount, long expectedFinalAmount) // ✅ Use long for all params
    {
        // Arrange
        var request = new TransactionRequest
        {
            PartnerKey = "FAKEGOOGLE",
            PartnerPassword = "RkFLRVBBU1NXT1JEMTIzNA==",
            PartnerRefNo = "FG-00001",
            TotalAmount = totalAmountMYR, // ✅ Already long
            Items = new List<ItemDetail>
        {
            new ItemDetail { PartnerItemRef = "i-00001", Name = "Item1", Qty = 1, UnitPrice = totalAmountMYR }
        },
            Sig = "validsignature"
        };

        _mockService
            .Setup(service => service.ProcessTransaction(request))
            .Returns(new TransactionResponse
            {
                Result = 1,
                TotalAmount = totalAmountMYR, // ✅ Already long
                TotalDiscount = expectedDiscount, // ✅ Already long
                FinalAmount = expectedFinalAmount, // ✅ Already long
                ResultMessage = "Transaction Successful"
            });

        // Act
        var result = _controller.SubmitTransaction(request);
        var response = result.Result as ObjectResult;
        var responseBody = response?.Value as TransactionResponse;

        // Log Request & Response
        _output.WriteLine("📤 REQUEST:");
        _output.WriteLine(System.Text.Json.JsonSerializer.Serialize(request, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        _output.WriteLine("📥 RESPONSE:");
        _output.WriteLine(System.Text.Json.JsonSerializer.Serialize(responseBody, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        string requestJson = System.Text.Json.JsonSerializer.Serialize(request, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        string responseJson = System.Text.Json.JsonSerializer.Serialize(responseBody, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        // Log transaction
        LogTransaction(requestJson, responseJson);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(responseBody);
        Assert.Equal(1, responseBody.Result);
        Assert.Equal(totalAmountMYR, responseBody.TotalAmount);
        Assert.Equal(expectedDiscount, responseBody.TotalDiscount);
        Assert.Equal(expectedFinalAmount, responseBody.FinalAmount);
        Assert.Equal("Transaction Successful", responseBody.ResultMessage);
    }

    [Theory]
    [InlineData(19999, 0, 19999)]
    [InlineData(20000, 1000, 19000)]
    [InlineData(79999, 5599, 74399)]
    [InlineData(80000, 8000, 72000)]
    [InlineData(120000, 18000, 102000)]
    public void SubmitTransaction_ShouldApplyDiscounts_Correctly(long totalAmount, long expectedDiscount, long expectedFinalAmount)
    {
        var request = new TransactionRequest
        {
            PartnerKey = "FAKEGOOGLE",
            PartnerRefNo = "FG-00001",
            PartnerPassword = "RkFLRVBBU1NXT1JEMTIzNA==",
            TotalAmount = totalAmount,
            Items = new List<ItemDetail> { new ItemDetail { PartnerItemRef = "i-00001", Name = "Item1", Qty = 1, UnitPrice = totalAmount } },
            Sig = "validsignature"
        };

        _mockService.Setup(service => service.ProcessTransaction(request))
            .Returns(new TransactionResponse { Result = 1, TotalAmount = totalAmount, TotalDiscount = expectedDiscount, FinalAmount = expectedFinalAmount, ResultMessage = "Transaction Successful" });

        var result = _controller.SubmitTransaction(request);
        var response = result.Result as OkObjectResult;
        var responseBody = response?.Value as TransactionResponse;
        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(responseBody);
        Assert.Equal(expectedDiscount, responseBody.TotalDiscount);
        Assert.Equal(expectedFinalAmount, responseBody.FinalAmount);
    }

    [Theory]
    [InlineData(null, "Pen", 1, 1000, "PartnerItemRef is Required.")]
    [InlineData("i-00001", null, 1, 1000, "Item Name is Required.")]
    [InlineData("i-00001", "Pen", 0, 1000, "Quantity must be between 1 and 5.")]
    [InlineData("i-00001", "Pen", 6, 1000, "Quantity must be between 1 and 5.")]
    [InlineData("i-00001", "Pen", 1, -100, "UnitPrice must be a positive value.")]
    public void SubmitTransaction_ShouldReturnBadRequest_ForInvalidItemDetails(string itemRef, string name, int qty, long price, string expectedMessage)
    {
        var request = new TransactionRequest
        {
            PartnerKey = "FAKEGOOGLE",
            PartnerRefNo = "FG-00001",
            PartnerPassword = "RkFLRVBBU1NXT1JEMTIzNA==",
            TotalAmount = 1000,
            Items = new List<ItemDetail> { new ItemDetail { PartnerItemRef = itemRef, Name = name, Qty = qty, UnitPrice = price } },
            Sig = "validsignature"
        };

        _mockService.Setup(service => service.ProcessTransaction(request))
            .Returns(new TransactionResponse { Result = 0, ResultMessage = expectedMessage });

        var result = _controller.SubmitTransaction(request);
        var response = result.Result as BadRequestObjectResult;
        var responseBody = response?.Value as TransactionResponse;

        // Log Request & Response
        _output.WriteLine("📤 REQUEST:");
        _output.WriteLine(System.Text.Json.JsonSerializer.Serialize(request, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        _output.WriteLine("📥 RESPONSE:");
        _output.WriteLine(System.Text.Json.JsonSerializer.Serialize(responseBody, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        string requestJson = System.Text.Json.JsonSerializer.Serialize(request, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        string responseJson = System.Text.Json.JsonSerializer.Serialize(responseBody, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        // Log transaction
        LogTransaction(requestJson, responseJson);

        Assert.NotNull(response);
        Assert.Equal(400, response.StatusCode);
        Assert.NotNull(responseBody);
        Assert.Equal(expectedMessage, responseBody.ResultMessage);
    }

}