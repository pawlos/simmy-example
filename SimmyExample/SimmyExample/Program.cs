using System.Net.Sockets;
using Polly.Contrib.Simmy;
using Polly.Contrib.Simmy.Latency;
using Polly.Contrib.Simmy.Outcomes;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// https://github.com/Polly-Contrib/Simmy
var fault = new SocketException(errorCode: 10013);
var socketExceptionPolicy = MonkeyPolicy.InjectException(with =>
{
    with.Fault(fault)
        .InjectionRate(0.05)
        .Enabled();
});

var responsePolicy = MonkeyPolicy.InjectLatency(with =>
{
    with.Latency(TimeSpan.FromMinutes(2))
        .InjectionRate(0.05)
        .Enabled();
});
// Exception, Result, Latency, Behavior
// Enabled(bool), EnabledWhen(condition)
var chaosPolicy = socketExceptionPolicy.Wrap(responsePolicy);
app.MapGet("/weatherforecast", () =>
    {
        return chaosPolicy.Execute(() =>
        {
            var forecast = Enumerable.Range(1, 5).Select(index =>
                    new WeatherForecast
                    (
                        DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        Random.Shared.Next(-20, 55),
                        summaries[Random.Shared.Next(summaries.Length)]
                    ))
                .ToArray();
            return forecast;
        });
    })
    .WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}