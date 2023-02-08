

using IMarkupLogger = WebMarkupMin.Core.Loggers.ILogger;
using MarkupNullLogger = WebMarkupMin.Core.Loggers.NullLogger;
using MetaWeblogService = LearnMsNet.CoreBlog.Services.MetaWeblogService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddSingleton<IUserService, FileUserService>();
builder.Services.AddSingleton<IBlogService, FileBlogService>();
builder.Services.Configure<BlogSettings>(builder.Configuration.GetSection("blog"));
builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddMetaWeblog<MetaWeblogService>();

builder.Services.AddProgressiveWebApp(
    new WebEssentials.AspNetCore.Pwa.PwaOptions
    {
        OfflineRoute = "shared/offline"
    });

builder.Services.AddOutputCaching(
    options =>
    {
        options.Profiles["default"] = new OutputCacheProfile
        {
            Duration = 3600
        };
    });

builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme
    ).AddCookie(
        options =>
        {
            options.LoginPath = "/login/";
            options.LogoutPath = "/logout/";
        });

builder.Services.AddWebMarkupMin(
    options =>
    {
        options.AllowMinificationInDevelopmentEnvironment = true;
        options.DisablePoweredByHttpHeaders = true;
    })
    .AddHtmlMinification(
        options =>
        {
            options.MinificationSettings.RemoveOptionalEndTags = false;
            options.MinificationSettings.WhitespaceMinificationMode = WhitespaceMinificationMode.Safe;
        });
builder.Services.AddSingleton<IMarkupLogger, MarkupNullLogger>();
builder.Services.AddWebOptimizer(
    pipeline =>
    {
        // pipeline.MinifyJsFiles();
        pipeline.CompileScssFiles()
            .InlineImages(1);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Shared/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.Use((context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    return next();
});
app.UseStatusCodePagesWithReExecute("/Shared/Error");
app.UseWebOptimizer();
app.UseStaticFilesWithCache();

if (builder.Configuration.GetValue<bool>("forcessl", false))
{
    app.UseHttpsRedirection();
}

app.UseMetaWeblog("/metaweblog");
app.UseAuthentication();
app.UseOutputCaching();
app.UseWebMarkupMin();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
