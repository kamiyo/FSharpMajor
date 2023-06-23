module FSharpMajor.DatabaseService

open System
open System.Runtime.CompilerServices

open Microsoft.AspNetCore.Http

open SqlHydra.Query
open FSharpMajor.Database
open Dapper.FSharp.PostgreSQL
open System.Data
open Npgsql

type IDatabaseService =
    abstract member Connection: IDbConnection

type DatabaseService() =
    interface IDatabaseService with
        member _.Connection = npgsqlSource.OpenConnection()

[<Extension>]
type HttpContextExtensions() =
    [<Extension>]
    static member GetDatabaseQueryContext(ctx: HttpContext) =
        match ctx.RequestServices.GetService typeof<IDatabaseService> with
        | null -> raise (Exception "Missing database query context service")
        | service -> service :?> IDatabaseService
