module FSharpMajor.Program

open System

open Serilog

open FSharpMajor.XmlSerializer

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

open Giraffe
open Giraffe.SerilogExtensions
open dotenv.net

open Dapper.FSharp

open Initialize
open Router
open Authorization
open DatabaseService
open Scanner

DotEnv.Load(DotEnvOptions(envFilePaths = [| ".env" |], probeForEnv = true))

System.Console.OutputEncoding <- System.Text.Encoding.Unicode

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
    let scanTask = startTraverseDirectories ()

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
