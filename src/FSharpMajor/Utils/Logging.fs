module FSharpMajor.Utils.Logging

open Serilog

let initLogger () =
    let serilog =
        LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .Enrich.FromLogContext()
            .CreateLogger()

    Log.Logger <- serilog
