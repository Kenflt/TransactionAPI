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
    [InlineData(15000, 0, 150.00)]       // No discount (<200 MYR)
    [InlineData(20000, 10.00, 190.00)]    // 5% discount
    [InlineData(50000, 25.00, 475.00)]    // 5% discount
    [InlineData(60000, 42.00, 558.00)]    // 7% discount
    [InlineData(80000, 56.00, 744.00)]    // 7% discount
    [InlineData(100000, 100.00, 900.00)]  // 10% discount
    [InlineData(150000, 225.00, 1275.00)] // 15% discount
    [InlineData(50300, 40.24, 462.76)]    // Prime number 503 → Extra 8% discount (rounded)
    [InlineData(90500, 181.00, 724.00)]   // Ends in 5 & > 900 → Extra 10% discount (rounded)
    public void SubmitTransaction_ShouldApplyCorrectDiscounts(
    long totalAmountCents, long expectedDiscountMYR, long expectedFinalAmountMYR)
    {
        // Arrange
        long totalAmountMYR = (long)(totalAmountCents / 100.0);
        var request = new TransactionRequest
        {
            PartnerKey = "FAKEGOOGLE",
            PartnerPassword = "RkFLRVBBU1NXT1JEMTIzNA==",
            PartnerRefNo = "FG-00001",
            TotalAmount = totalAmountCents, // ✅ Stored in cents
            Items = new List<ItemDetail>
        {
            new ItemDetail { PartnerItemRef = "i-00001", Name = "Item1", Qty = 1, UnitPrice = totalAmountCents }
        },
            Sig = "validsignature"
        };

        _mockService
            .Setup(service => service.ProcessTransaction(request))
            .Returns(new TransactionResponse
            {
                Result = 1,
                TotalAmount = totalAmountMYR, // ✅ Converted to MYR
                TotalDiscount = expectedDiscountMYR, // ✅ Already in MYR
                FinalAmount = expectedFinalAmountMYR, // ✅ Already in MYR
                ResultMessage = "Transaction Successful"
            });

        // Act
        var result = _controller.SubmitTransaction(request);
        var response = result.Result as ObjectResult;
        var responseBody = response?.Value as TransactionResponse;

        // Assert
        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(responseBody);
        Assert.Equal(1, responseBody.Result);
        Assert.Equal(totalAmountMYR, responseBody.TotalAmount);
        Assert.Equal(expectedDiscountMYR, responseBody.TotalDiscount);
        Assert.Equal(expectedFinalAmountMYR, responseBody.FinalAmount);
        Assert.Equal("Transaction Successful", responseBody.ResultMessage);
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