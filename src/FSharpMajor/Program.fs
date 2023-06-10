namespace FSharpMajor

open System

open Serilog

open FSharpMajor.FsLibLog
open FSharpMajor.XmlSerializer

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

open Giraffe
open Giraffe.SerilogExtensions

module Program =
    open Initialize
    open Router
    open Authorization
    open DatabaseService

    let exitCode = 0


    let configureApp (app: IApplicationBuilder) =
        app.UseAuthentication() |> ignore
        app.UseGiraffe(SerilogAdapter.Enable rootRouter)

    let configureServices (services: IServiceCollection) =
        services.AddGiraffe() |> ignore

        services.AddSingleton<IDatabaseService, DatabaseService>() |> ignore

        services
            .AddAuthentication("Basic")
            .AddScheme<BasicAuthenticationOptions, BasicAuthHandler>("Basic", null)
        |> ignore

        services.AddSingleton<IAuthenticationManager, SubsonicAuthenticationManager>()
        |> ignore

        services.AddSingleton<Xml.ISerializer>(CustomXmlSerializer(xmlWriterSettings))
        |> ignore


    let log =
        LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .Enrich.FromLogContext()
            .CreateLogger()

    Log.Logger <- log

    [<EntryPoint>]
    let main args =

        makeAdminIfNotExists () |> ignore

        Host
            .CreateDefaultBuilder()
            .ConfigureWebHostDefaults(fun webHostBuilder ->
                webHostBuilder
                    .UseUrls("http://*:8080")
                    .Configure(configureApp)
                    .ConfigureServices(configureServices)
                |> ignore)
            .Build()
            .Run()

        exitCode
