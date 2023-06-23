module FSharpMajor.Utils.Logging

open Serilog
open Microsoft.Extensions.Logging
let serilog =
    LoggerConfiguration()
        .MinimumLevel.Verbose()
        .WriteTo.Console()
        .Enrich.FromLogContext()
        .CreateLogger()

Log.Logger <- serilog

let logger = LoggerFactory.Create(fun logging -> logging.AddSerilog(Log.Logger) |> ignore)