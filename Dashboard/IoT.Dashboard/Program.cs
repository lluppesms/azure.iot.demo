var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------------------------------------------
// Add services to the container.
// ----------------------------------------------------------------------------------------------------
// Get Application Settings into object
var settings = new AppSettings(
    builder.Configuration["IoTHubConnectionString"].ToStringNullable(),
    builder.Configuration["StorageConnectionString"].ToStringNullable(),
    builder.Configuration["CosmosConnectionString"].ToStringNullable(),
    builder.Configuration["SignalRConnectionString"].ToStringNullable(),
    builder.Configuration["ApplicationInsightsKey"].ToStringNullable()
);

builder.Logging.SetMinimumLevel(LogLevel.Warning);
// TODO: change the aapplication logger to use a custom logger and/or Serilog...
// https://docs.microsoft.com/en-us/aspnet/core/blazor/fundamentals/logging?view=aspnetcore-6.0
MyLogger.InitializeLogger(settings);

builder.Services.AddSingleton(builder.Configuration);

// ----- Configure Repositories -----------------------------------------------------------------------
builder.Services.AddSingleton(settings);
builder.Services.AddSingleton(new DeviceRepository(settings));

var enableAuthSetting = builder.Configuration["EnableAuthentication"].ToStringNullable("N");
var enableAuthentication = !string.IsNullOrEmpty(enableAuthSetting) && (enableAuthSetting.StartsWith("T", StringComparison.InvariantCultureIgnoreCase) || enableAuthSetting.StartsWith("Y", StringComparison.InvariantCultureIgnoreCase));

//// ----- Configure Authentication ---------------------------------------------------------------------
if (enableAuthentication)
{
    //// Usually I would just read this object right from appsettings or secrets or from the environment config...
    //// However, when deployed to Azure Container Service, ACS only allows creating secrets with letters (no ":" sign for a section...)
    //// Therefore, it doesn't read these right and the program can't figure out how to build this "section" from there
    //// Warning: many configuration options work locally in Docker, but fail when promoted out to ACS...

    //// This works locally in Docker -and- when deployed to ACS: create a dummy memory structure populated with flat secrets from config
    var structuredKeys = new List<KeyValuePair<string, string>> {
        new KeyValuePair<string, string>("AzureAd:Instance", builder.Configuration["AzureAdInstance"].ToStringNullable()),
        new KeyValuePair<string, string>("AzureAd:Domain", builder.Configuration["AzureAdDomain"].ToStringNullable()),
        new KeyValuePair<string, string>("AzureAd:TenantId", builder.Configuration["AzureAdTenantId"].ToStringNullable()),
        new KeyValuePair<string, string>("AzureAd:CallbackPath", builder.Configuration["AzureAdCallbackpath"].ToStringNullable()),
        new KeyValuePair<string, string>("AzureAd:SignedOutCallbackPath",  builder.Configuration["AzureAdSignedoutCallbackpath"].ToStringNullable()),
        new KeyValuePair<string, string>("AzureAd:ClientId", builder.Configuration["AzureAdClientId"].ToStringNullable())
    };
    var azureAdConfig = new ConfigurationBuilder().AddInMemoryCollection(structuredKeys).Build().GetSection("AzureAd");
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApp(azureAdConfig);
    
    // ----- Configure Authorization ----------------------------------------------------------------------
    builder.Services.AddAuthorization(options =>
	{
		// By default, all incoming requests will be authorized according to the default policy.
		options.FallbackPolicy = options.DefaultPolicy;
	});
}

// ----- Configure Context Accessor -------------------------------------------------------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HttpContextAccessor>();


// ---- Add third party component startups ------------------------------------------------------------
builder.Services.AddToaster(config =>
{
    config.PositionClass = Defaults.Classes.Position.TopRight;
    config.PreventDuplicates = true;
    config.NewestOnTop = false;
});
builder.Services.AddSweetAlert2(options =>
{
    options.Theme = SweetAlertTheme.Default;
});
builder.Services
      .AddBlazorise(options =>
      {
          // options.ChangeTextOnKeyPress = true;
      })
      .AddBootstrapProviders()
      .AddFontAwesomeIcons();

builder.Services.AddSweetAlert2(options =>
{
    options.Theme = SweetAlertTheme.Default;
});

builder.Services.AddBlazoredLocalStorage();

// ----------------------------------------------------------------------------------------------------
// Configure application
// ----------------------------------------------------------------------------------------------------
if (enableAuthentication)
{
    builder.Services.AddRazorPages().AddMicrosoftIdentityUI();
}
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// ----------------------------------------------------------------------------------------------------
// API application
// ----------------------------------------------------------------------------------------------------
//builder.Services.AddControllers().AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Program>());

//// Configure the API versioning properties of the project. 
//builder.Services.AddApiVersioningConfigured();

//// Add a Swagger generator and Automatic Request and Response annotations:
//builder.Services.AddSwaggerSwashbuckleConfigured();

// ----------------------------------------------------------------------------------------------------
// Start up application
// ----------------------------------------------------------------------------------------------------
var app = builder.Build();

// ----------------------------------------------------------------------------------------------------
// Configure the HTTP request pipeline.
// ----------------------------------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//if (settings.EnableSwagger)
//{
//    // Enable middleware to serve the generated OpenAPI definition as JSON files.
//    app.UseSwagger();
//    // Enable middleware to serve Swagger-UI (HTML, JS, CSS, etc.) by specifying the Swagger JSON files(s).
//    var descriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
//    app.UseSwaggerUI(options =>
//    {
//        // Build a swagger endpoint for each discovered API version
//        foreach (var description in descriptionProvider.ApiVersionDescriptions)
//        {
//            options.SwaggerEndpoint($"{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
//        }
//    });
//}


// NOTE: when doing Docker and Azure Container Services, the authentication redirect is failing and is redirecting to
// 'HTTP', not 'HTTPS', which causes the auth to fail.
// See https://stackoverflow.com/questions/53353601/redirect-after-authentication-is-to-http-when-it-should-be-https
// See https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-6.0&viewFallbackFrom=aspnetcore-2.1
app.Use((context, next) =>
{
    context.Request.Scheme = "https";
    return next();
});
app.UseForwardedHeaders();

// required if you want to use html, css, or image files...
app.UseStaticFiles();
app.UseHttpsRedirection();

// routing matches HTTP request and dispatches them to proper endpoings
app.UseRouting();
if (enableAuthentication)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseEndpoints(endpoints =>
{
    endpoints.MapBlazorHub();
    endpoints.MapControllers();
    endpoints.MapFallbackToPage("/_Host");
});
app.Run();
