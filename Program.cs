using CSCourse.Interfaces;
using CSCourse.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<IEventService, EventMemoryService>();

builder.Services.AddSwaggerGen();

builder.Services.AddControllers();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
