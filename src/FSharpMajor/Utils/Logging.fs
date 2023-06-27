module FSharpMajor.Utils.Logging

open Serilog
open Microsoft.Extensions.Logging

let initLogger () =
    let serilog =
        LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .Enrich.FromLogContext()
            .CreateLogger()

    Log.Logger <- serilog

let logger =
    LoggerFactory.Create(fun logging -> logging.AddSerilog(Log.Logger) |> ignore)
