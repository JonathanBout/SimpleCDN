using SimpleCDN;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddOptions<CDNConfiguration>();

builder.Services.AddSingleton<CDNLoader>();

builder.Services.AddMemoryCache();

var app = builder.Build();

app.MapCDNEndpoints();

app.Run();
