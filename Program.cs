using DBtoDB.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add API documentation services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "DBtoDB API", 
        Description = "API for executing SQL Server stored procedures",
        Version = "v1" 
    });
});

// Add memory cache for storing procedure results
builder.Services.AddMemoryCache();

// Register the SQL Server database service
builder.Services.AddScoped<IDatabaseService, SqlServerService>();

// Configure SQL Server connection pooling
builder.Services.AddOptions<SqlClientProviderOptions>()
    .Configure(options =>
    {
        // Set the maximum number of connections in the pool
        options.MaxPoolSize = builder.Configuration.GetValue<int>("DatabaseSettings:MaxPoolSize");
    });

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // Enable Swagger UI in development
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DBtoDB API V1");
        c.RoutePrefix = string.Empty; // Serve the Swagger UI at the root URL
    });
}

// Redirect HTTP to HTTPS
app.UseHttpsRedirection();

// Enable authorization
app.UseAuthorization();

// Map controller endpoints
app.MapControllers();

// Start the application
app.Run(); 