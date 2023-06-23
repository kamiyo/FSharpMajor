module FSharpMajor.Database

open Microsoft.Extensions.Logging
open Serilog
open dotenv.net
open FsConfig
open Npgsql
open FSharpMajor.Utils.Logging

DotEnv.Load(DotEnvOptions(envFilePaths = [| ".env" |], probeForEnv = true))

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
        | NotFound envVarName -> failwithf "Environment variable %s not found" envVarName
        | BadValue(envVarName, value) -> failwithf "Environment variable %s has invalid value %s" envVarName value
        | NotSupported msg -> failwith msg

let connString =
    let { User = user
          Password = password
          Db = dbName
          Port = port
          Host = host } =
        config

    $"Host={host};Password={password};Username={user};Database={dbName};Port={port}"

let private dataSourceBuilder = new NpgsqlDataSourceBuilder(connString)
dataSourceBuilder.UseLoggerFactory logger |> ignore
let npgsqlSource = dataSourceBuilder.Build()