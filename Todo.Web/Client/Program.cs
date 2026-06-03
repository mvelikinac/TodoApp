using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Todo.Web.Client;
using Todo.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddHttpClient<TodoClient>(client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
});

builder.Services.AddScoped<ThemeService>();

await builder.Build().RunAsync();
