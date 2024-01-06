module MusiScore.Server.App

open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open System
open System.IO
open System.Reflection
open System.Text.Json.Serialization

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    builder.Services.AddSingleton<Db>(fun (serviceProvider: IServiceProvider) ->
        let connectionString =
            let config = serviceProvider.GetService<IConfiguration>()
            config.GetConnectionString("Db")
            |> Option.ofObj
            |> Option.orElseWith (fun () ->
                config.GetValue("Db_ConnectionString_File")
                |> Option.ofObj
                |> Option.map File.ReadAllText
            )
            |> Option.defaultWith (fun () -> failwith "DB connection string not found")
        Db(connectionString)
    ) |> ignore

    builder.Services.AddSingleton<Printer>(fun (serviceProvider: IServiceProvider) ->
        let printConfig = serviceProvider.GetService<IConfiguration>().GetSection("Print")
        let printServer = printConfig.GetValue("Server")
        let printerName = printConfig.GetValue("Printer")
        Printer(printServer, printerName)
    ) |> ignore

    builder.Services
        .AddControllers()
        .AddJsonOptions(fun o ->
            Assembly.GetExecutingAssembly().ExportedTypes
            |> Seq.filter(fun v -> v.BaseType <> null && not v.ContainsGenericParameters && typeof<JsonConverter>.IsAssignableFrom(v))
            |> Seq.map Activator.CreateInstance
            |> Seq.cast<JsonConverter>
            |> Seq.iter o.JsonSerializerOptions.Converters.Add
        ) |> ignore

    builder.Services.AddMvc() |> ignore
    builder.Services.AddServerSideBlazor() |> ignore
    builder.Services.AddAuthorization() |> ignore
    builder.Services
        .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie()
    |> ignore

    let app = builder.Build()

    if app.Environment.IsProduction() then
        app
            .UseDefaultFiles()
            .UseStaticFiles()
        |> ignore

    app.UseAuthentication() |> ignore
    app.UseAuthorization() |> ignore
    app.MapControllers() |> ignore

    if app.Environment.IsProduction() then
        app.UseRouting().UseEndpoints(fun endpoints ->
            endpoints.MapFallbackToFile("/index.html") |> ignore
        ) |> ignore

    app.Run()

    0
