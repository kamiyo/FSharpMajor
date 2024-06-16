module FSharpMajor.Database

open Microsoft.Extensions.Logging
open dotenv.net
open FsConfig
open Npgsql
open Serilog

DotEnv.Load(DotEnvOptions(envFilePaths = [| ".env" |], probeForEnv = true, probeLevelsToSearch = 8))

[<Convention("POSTGRES")>]
type Config =
    { User: string
      Password: string
      Db: string
      Port: int
      Host: string }

let config =
    match EnvConfig.Get<Config>() with
    | Ok config -> config
    | Error error ->
        match error with
        | NotFound envVarName -> failwithf $"Environment variable %s{envVarName} not found"
        | BadValue(envVarName, value) -> failwithf $"Environment variable %s{envVarName} has invalid value %s{value}"
        | NotSupported msg -> failwith msg

let connString =
    let { User = user
          Password = password
          Db = dbName
          Port = port
          Host = host } =
        config

    $"Host={host};Password={password};Username={user};Database={dbName};Port={port};Include Error Detail=true"

let private dataSourceBuilder = NpgsqlDataSourceBuilder(connString)

let logger =
    LoggerFactory.Create(fun logging -> logging.AddSerilog(Log.Logger) |> ignore)

// match System.Environment.GetEnvironmentVariable "ASPNETCORE_ENVIRONMENT" with
// | "Development" ->
//     dataSourceBuilder.UseLoggerFactory logger |> ignore
//     dataSourceBuilder.EnableParameterLogging() |> ignore
// | _ -> ()

let npgsqlSource = dataSourceBuilder.Build()
