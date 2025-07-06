using PrintServer.Core.Interfaces;
using PrintServer.Core.Printers;
using PrintServer.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register printer drivers
builder.Services.AddScoped<MunbynPrinterDriver>();
builder.Services.AddScoped<EpsonPrinterDriver>();

// Register printer drivers as IPrinterDriver
builder.Services.AddScoped<IPrinterDriver, MunbynPrinterDriver>();
builder.Services.AddScoped<IPrinterDriver, EpsonPrinterDriver>();

// Register print service
builder.Services.AddScoped<IPrintService, PrintService>();

// Configure logging
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Initialize printer drivers
using var scope = app.Services.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

try
{
    var munbynDriver = scope.ServiceProvider.GetRequiredService<MunbynPrinterDriver>();
    var epsonDriver = scope.ServiceProvider.GetRequiredService<EpsonPrinterDriver>();

    await munbynDriver.InitializeAsync();
    await epsonDriver.InitializeAsync();

    logger.LogInformation("Print server initialized successfully");
}
catch (Exception ex)
{
    logger.LogError(ex, "Error initializing print server");
}

app.Run(); 