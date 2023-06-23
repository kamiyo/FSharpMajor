namespace FSharpMajor

open System

open Serilog

open FSharpMajor.FsLibLog
open FSharpMajor.XmlSerializer

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

open Giraffe
open Giraffe.SerilogExtensions

module Program =
    open dotenv.net

    open Dapper.FSharp

    open Initialize
    open Router
    open Authorization
    open DatabaseService
    open FsConfig
    open Scanner

    DotEnv.Load(DotEnvOptions(envFilePaths = [| ".env" |], probeForEnv = true))

    let exitCode = 0


    let configureApp (app: IApplicationBuilder) =
        app.UseAuthentication() |> ignore
        app.UseGiraffe(SerilogAdapter.Enable rootRouter)

    let configureServices (services: IServiceCollection) =
        services.AddGiraffe() |> ignore

        services.AddScoped<IDatabaseService, DatabaseService>() |> ignore

        services
            .AddAuthentication("Basic")
            .AddScheme<BasicAuthenticationOptions, BasicAuthHandler>("Basic", null)
        |> ignore

        services.AddScoped<IAuthenticationManager, SubsonicAuthenticationManager>()
        |> ignore

        services.AddSingleton<Xml.ISerializer>(CustomXmlSerializer(xmlWriterSettings))
        |> ignore


    [<EntryPoint>]
    let main args =

        PostgreSQL.OptionTypes.register ()

        makeOrUpdateAdmin ()
        makeLibraryRoots () |> ignore
        let scanTask =
            startTraverseDirectories ()

        Host
            .CreateDefaultBuilder()
            .ConfigureWebHostDefaults(fun webHostBuilder ->
                webHostBuilder
                    .UseUrls("http://*:8080")
                    .Configure(configureApp)
                    .ConfigureServices(configureServices)
                |> ignore)
            .UseSerilog()
            .Build()
            .Run()

        scanTask.Result
        exitCode
