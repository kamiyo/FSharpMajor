module Database

open SqlHydra.Query
open FsConfig
open Npgsql

[<Convention("POSTGRES")>]
type Config = {
    User: string;
    Password: string;
    Db: string;
    Port: int;
    Host: string;
}

let config =
    match EnvConfig.Get<Config>() with
    | Ok config -> config
    | Error error ->
      match error with
      | NotFound envVarName ->
        failwithf "Environment variable %s not found" envVarName
      | BadValue (envVarName, value) ->
        failwithf "Environment variable %s has invalid value %s" envVarName value
      | NotSupported msg ->
        failwith msg

let openContext() =
    let compiler = SqlKata.Compilers.PostgresCompiler()
    let {
        User = user;
        Password = password;
        Db = dbName;
        Port = port;
        Host = host;
    } = config
    let connString = $"Host={host};Password={password};Username={user};Database={dbName};Port={port}"
    let conn = new NpgsqlConnection(connString)
    conn.Open();
    new QueryContext(conn, compiler)