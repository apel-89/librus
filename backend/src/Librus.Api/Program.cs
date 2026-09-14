using Librus.Api.Data.Seed;
using Librus.Api.Features.Users;
using Librus.Api.Features.Loans;
using Librus.Api.Features.Books;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

builder.Services.AddDbContext<LibrusDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
     .UseSnakeCaseNamingConvention());

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:3000")
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HeaderCurrentUser>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<LoanService>();
builder.Services.AddScoped<ReadingTimeService>();

builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LibrusDbContext>();
    await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(db);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options
        .WithTitle("Librus API")
        .WithDefaultHttpClient(ScalarTarget.Shell, ScalarClient.Curl));
}

app.UseCors();

app.MapMyLoanEndpoints();
app.MapLoanEndpoints();
app.MapMeEndpoints();
app.MapBookEndpoints();
app.MapDiscoverEndpoints();

app.Run();
