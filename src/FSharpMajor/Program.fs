namespace FSharpMajor

open System

open FSharpMajor.Scheduler
open FsConfig
open Serilog

open FSharpMajor.Utils.Logging
open FSharpMajor.XmlSerializer

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

open Giraffe
open Giraffe.SerilogExtensions
open dotenv.net

open Quartz

open Dapper.FSharp

open FSharpMajor.FsLibLog
open Initialize
open Router
open Authorization
open DatabaseService
open Scanner

module Program =

    type TimerConfig = { ScanTime: string }

    DotEnv.Load(DotEnvOptions(envFilePaths = [| ".env" |], probeForEnv = true, probeLevelsToSearch = 8))

    let scanTime =
        match EnvConfig.Get<TimerConfig>() with
        | Ok config -> TimeSpan.ParseExact(config.ScanTime, @"h\:m", null)
        | Error _ -> TimeSpan(2, 0, 0)

    let cronTime = $"0 {scanTime.Minutes} {scanTime.Hours} * * ?"

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

        services.AddQuartz(fun q ->
            q.SchedulerId <- "Core Scheduler"
            let jobKey = JobKey("update library", "default group")

            q
                .AddJob<UpdateJob>(jobKey, (fun j -> j.WithDescription("Run library updater") |> ignore))
                .AddTrigger(fun t ->
                    t
                        .WithIdentity("Cron Trigger")
                        .ForJob(jobKey)
                        .WithCronSchedule(cronTime)
                        .WithDescription("Run everyday at 2am")
                    |> ignore)
            |> ignore)
        |> ignore

        services.AddQuartzHostedService() |> ignore

    // let mailbox =
    //     MailboxProcessor.Start(fun inbox ->
    //         let logger = LogProvider.getLoggerByFunc ()
    //         let rec loop n =
    //             async {
    //                 do logger.info(Log.setMessage $"")
    //             }
    //         loop 0)

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
