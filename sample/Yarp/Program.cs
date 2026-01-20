using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Yarp.Configs;
using Yarp.Extensions;
using Yarp.ReverseProxy.Swagger;
using Yarp.ReverseProxy.Swagger.Extensions;
using Yarp.Transformations;
using Duende.AccessTokenManagement;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen();



builder.Services.AddClientCredentialsTokenManagement()
    .AddClient("yard", client =>
    {
        var identityConfig = builder.Configuration.GetSection("Identity").Get<IdentityConfig>()!;
        client.TokenEndpoint = new Uri($"{identityConfig.Url}/connect/token");
        client.ClientId = ClientId.Parse(identityConfig.ClientId);
        client.ClientSecret = ClientSecret.Parse(identityConfig.ClientSecret);
    });


var configuration = builder.Configuration.GetSection("ReverseProxy");
var configurationForOnlyPublishedRoutes = builder.Configuration.GetSection("ReverseProxyOnlyPublishedRoutes");
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(configuration)
    .LoadFromConfig(configurationForOnlyPublishedRoutes)
    .AddTransformFactory<HeaderTransformFactory>()
    .AddSwagger(configuration)
    .AddSwagger(configurationForOnlyPublishedRoutes);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var config = app.Services.GetRequiredService<IOptionsMonitor<ReverseProxyDocumentFilterConfig>>().CurrentValue;
        options.ConfigureSwaggerEndpoints(config);
    });
}

app.UseHttpsRedirection();

app.MapReverseProxy();

app.Run();
