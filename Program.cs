using ghCopilot.Models;
using ghCopilot.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure Azure settings from environment variables and configuration
builder.Services.Configure<AzureConfig>(options =>
{
    options.TenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID") ?? 
                       builder.Configuration["Azure:TenantId"] ?? string.Empty;
    options.ClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") ?? 
                       builder.Configuration["Azure:ClientId"] ?? string.Empty;
    options.ClientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET") ?? 
                           builder.Configuration["Azure:ClientSecret"] ?? string.Empty;
});

// Register Azure authentication service
builder.Services.AddSingleton<IAzureAuthService, AzureAuthService>();

// Register the Azure HTTP client handler
builder.Services.AddTransient<AzureHttpClientHandler>();

// Register named HttpClient for Azure REST API calls
builder.Services.AddHttpClient("AzureRestClient", client =>
{
    client.BaseAddress = new Uri("https://management.azure.com/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddHttpMessageHandler<AzureHttpClientHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
