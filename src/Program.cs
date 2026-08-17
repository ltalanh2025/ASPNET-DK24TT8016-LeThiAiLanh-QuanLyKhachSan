using Microsoft.Extensions.FileProviders;
using QLKS.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

AppConfig.Initialize(builder.Configuration, builder.Environment.ContentRootPath);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".QLKS.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SessionAccessor>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/ServerError");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Error/StatusCode", "?code={0}");
app.UseHttpsRedirection();

var contentRoot = app.Environment.ContentRootPath;
foreach (var staticDirectory in new[] { "Content", "Scripts" })
{
    var physicalPath = Path.Combine(contentRoot, staticDirectory);
    if (Directory.Exists(physicalPath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(physicalPath),
            RequestPath = "/" + staticDirectory
        });
    }
}

var faviconPath = Path.Combine(contentRoot, "favicon.ico");
if (File.Exists(faviconPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(contentRoot)
    });
}

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=CustomerHome}/{action=Index}/{id?}");

app.Run();
