namespace MusiScore.Client

open Bolero
open Elmish
open Microsoft.AspNetCore.Components.WebAssembly.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.JSInterop
open System
open System.Net.Http

type App() =
    inherit ProgramComponent<Model, Message>()

    override this.Program =
        let router = Router.infer SetPage (fun (model: Model) -> fst model)
        let httpClient = this.Services.GetService<HttpClient>()
        let js = this.Services.GetService<IJSRuntime>()
        Program.mkProgram (fun _ -> Model.init) (update httpClient js) (view router)
        |> Program.withRouter router

module Program =
    [<EntryPoint>]
    let main args =
        let builder = WebAssemblyHostBuilder.CreateDefault(args)
        builder.RootComponents.Add<App>("#main")
        builder.Services.AddScoped<_>(fun _serviceProvider ->
            new HttpClient(BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))) |> ignore
        builder.Build().RunAsync() |> ignore
        0
