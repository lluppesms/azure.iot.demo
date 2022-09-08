[assembly: FunctionsStartup(typeof(IoT.Processor.Startup))]
namespace IoT.Processor;

//// See https://damienbod.com/2020/07/12/azure-functions-configuration-and-secrets-management/

/// <summary>
/// IoT.Processor.Startup
/// </summary>
public class Startup : FunctionsStartup
{
    /// <summary>
    /// Configuration Intializer
    /// </summary>
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddOptions<MyConfiguration>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection("MyConfiguration").Bind(settings);
            });

        builder.Services.AddOptions<MySecrets>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection("MySecrets").Bind(settings);
            });

        // NOTE: this works locally but fails when deployed...?
        ////// ----- Get Configuration Settings ---------------------------------------------------------------
        ////var myConfig = builder.GetContext().Configuration.GetSection(nameof(MyConfiguration)).Get<MyConfiguration>();
        ////var mySecrets = builder.GetContext().Configuration.GetSection(nameof(MySecrets)).Get<MySecrets>();

        ////// ----- Configure Services -----------------------------------------------------------------------
        ////builder.Services.AddSingleton(new CosmosHelper(myConfig, mySecrets));
        ////builder.Services.AddSingleton(new SignalRHelper(myConfig, mySecrets));
    }

    /// <summary>
    /// Configuration Intializer
    /// </summary>
    public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
    {
        var builtConfig = builder.ConfigurationBuilder.Build();
        // ??? where does this come from...???
        var keyVaultEndpoint = builtConfig["AzureKeyVaultEndpoint"];

        if (!string.IsNullOrEmpty(keyVaultEndpoint))
        {
            // using Key Vault, either local dev or deployed
            builder.ConfigurationBuilder
                .SetBasePath(Environment.CurrentDirectory)
                .AddAzureKeyVault(new Uri(keyVaultEndpoint), new DefaultAzureCredential())
                .AddJsonFile("local.settings.json", true)
                .AddEnvironmentVariables()
                .Build();
        }
        else
        {
            // local dev - no Key Vault
            builder.ConfigurationBuilder
               .SetBasePath(Environment.CurrentDirectory)
               .AddJsonFile("local.settings.json", true)
               .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
               .AddEnvironmentVariables()
               .Build();
        }
    }
}
