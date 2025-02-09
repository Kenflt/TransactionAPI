using TransactionAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddControllers();
builder.Services.AddScoped<ITransactionService, TransactionService>();

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
app.Run();