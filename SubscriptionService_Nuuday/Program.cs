using Microsoft.EntityFrameworkCore;
using SubscriptionService_Nuuday.Models.CacheModels;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddMemoryCache(); //for memory
builder.Services.AddControllers();

//builder.Services.AddSingleton<MyMemoryCache>(); //for memory

//builder.Services.AddSingleton<DbContext, CustomerDbContext>();
//var connectionString = @"Server=.\\SQLExpress;Database=Customers;Trusted_Connection=True;";
//builder.Services.AddDbContext<CustomerDbContext>(opt => opt.UseSqlServer(connectionString));
//builder.Services.AddScoped<DbContext>(sp => sp.GetService<CustomerDbContext>());
//Container = builder.Services.BuildServiceProvider();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

//builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
