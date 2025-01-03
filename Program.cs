using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SixEightFoundation.Services;
using SixEightFoundation.Models;

var builder = WebApplication.CreateBuilder(args);

// Add configuration sources
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Configure email settings from environment variables
builder.Services.Configure<EmailSettings>(options =>
{
    options.ToEmail = builder.Configuration["EMAIL_TO"] ?? 
                     builder.Configuration["Email:ToEmail"] ?? 
                     throw new InvalidOperationException("Email recipient address is not configured.");
    options.SmtpUsername = builder.Configuration["EMAIL_SMTP_USERNAME"] ?? 
                          builder.Configuration["Email:SmtpUsername"] ?? 
                          throw new InvalidOperationException("SMTP username is not configured.");
    options.SmtpPassword = builder.Configuration["EMAIL_SMTP_PASSWORD"] ?? 
                          builder.Configuration["Email:SmtpPassword"] ?? 
                          throw new InvalidOperationException("SMTP password is not configured.");
});

builder.Services.AddScoped<IEmailService, EmailService>();

// Add HTTP logging in development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpLogging(logging =>
    {
        logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseHttpLogging();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Add health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

app.Run();
