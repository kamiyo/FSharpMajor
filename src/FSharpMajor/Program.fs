namespace FSharpMajor

open System

open Serilog

open FSharpMajor.Utils.Logging
open FSharpMajor.XmlSerializer
open FSharpMajor.FsLibLog

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

module Program =


    DotEnv.Load(DotEnvOptions(envFilePaths = [| ".env" |], probeForEnv = true))

    Console.OutputEncoding <- System.Text.Encoding.Unicode

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

    let rec keyboardLoop () =
        task {
            let c = Console.ReadKey()

            match c.Key with
            | ConsoleKey.R ->
                Console.WriteLine("\n")
                let scanTask = scanForUpdates ()
                scanTask.Result
                return! keyboardLoop ()
            | _ -> return! keyboardLoop ()
        }

    [<EntryPoint>]
    let main _ =
        PostgreSQL.OptionTypes.register ()

        initLogger ()
        makeOrUpdateAdmin ()
        makeLibraryRoots () |> ignore
        let scanTask = scanMusicLibrary ()

        Host
            .CreateDefaultBuilder()
            .ConfigureWebHostDefaults(fun webHostBuilder ->
                webHostBuilder
                    .UseUrls("http://*:8080")
                    .Configure(configureApp)
                    .ConfigureServices(configureServices)
                |> ignore)
            .UseSerilog(Log.Logger)
            .Build()
            .Run()

        scanTask.Result

        exitCode
