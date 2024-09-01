module MusiScore.Server.App

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open System
open System.IO

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    builder.Services.AddSingleton<Db>(fun (serviceProvider: IServiceProvider) ->
        let connectionString =
            builder.Configuration.GetValue("Db_ConnectionString_File")
            |> Option.ofObj
            |> Option.map File.ReadAllText
            |> Option.orElseWith (fun () ->
                builder.Configuration.GetConnectionString("Db")
                |> Option.ofObj
            )
            |> Option.defaultWith (fun () -> failwith "'Db_ConnectionString_File' and 'ConnectionStrings:Db' not found in configuration.")
        Db(connectionString)
    ) |> ignore

    builder.Services.AddSingleton<Printer>(fun _serviceProvider ->
        let printServer = builder.Configuration.GetRequiredSection("Print:Server").Value
        let printerName = builder.Configuration.GetRequiredSection("Print:Printer").Value
        Printer(printServer, printerName)
    ) |> ignore

    builder.Services
        .AddControllers(fun o ->
            if builder.Environment.IsDevelopment() then
                o.Filters.Add(FaultInjectionFilter()) |> ignore
        ) |> ignore

    builder.Services.AddMvc() |> ignore
    builder.Services.AddAuthorization(fun options ->
        options.AddPolicy("Notenarchivar", fun policy -> policy.RequireRole("Notenarchivar") |> ignore)
    ) |> ignore
    builder.Services.AddAuthentication("APIKey").AddScheme<APIKeyAuthenticationSchemeOptions, APIKeyAuthenticationHandler>("APIKey", fun o ->
        o.APIKeys <-
            builder.Configuration.GetRequiredSection("APIKeys").GetChildren()
            |> Seq.map (fun v -> v.Key, v.Get<string[]>() |> Array.toList)
            |> Seq.toList
    )
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
