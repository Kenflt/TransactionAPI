using TransactionAPI.Services;
using log4net;
using log4net.Config;
using System.Reflection;

var logRepository = LogManager.GetRepository(System.Reflection.Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddControllers();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.WebHost.UseUrls("http://0.0.0.0:8080");
var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
app.Run();