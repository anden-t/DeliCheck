using BlazorCurrentDevice;
using Blazored.LocalStorage;
using Cropper.Blazor.Extensions;
using DeliCheck.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;
using Blazorise.Components;
using Blazorise.Providers;
using Blazorise;
using Blazorise.Bootstrap5;
using Append.Blazor.WebShare;

namespace DeliCheck.Web
{

    public class Program
    {
        public const string AppUrl = "https://api.deli-check.ru/";
        public const string SiteUrl = "https://deli-check.ru/";

        public static async Task Main(string[] args)
        {
#pragma warning disable 024

            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddScoped(sp => new HttpClient() { BaseAddress = new Uri(AppUrl) })
                .AddScoped<DeliCheckClient>()
                .AddSingleton<AlertService>();
            builder.Services.AddRadzenComponents();
            builder.Services.AddBlazoredLocalStorageAsSingleton();
            builder.Services.AddCropper().AddBlazorCurrentDevice().AddBlazorise(x => x.Immediate = true).AddBootstrap5Providers().AddWebShare();


            await builder.Build().RunAsync();
        }
    }
}
