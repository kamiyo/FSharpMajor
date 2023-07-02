module FSharpMajor.DatabaseService

open System
open System.Runtime.CompilerServices

open Microsoft.AspNetCore.Http

open FSharpMajor.Database
open System.Data

type IDatabaseService =
    abstract member OpenConnection: unit -> IDbConnection

type DatabaseService() =
    interface IDatabaseService with
        member _.OpenConnection() = npgsqlSource.OpenConnection()

[<Extension>]
type HttpContextExtensions() =
    [<Extension>]
    static member GetDatabaseQueryContext(ctx: HttpContext) =
        match ctx.RequestServices.GetService typeof<IDatabaseService> with
        | null -> raise (Exception "Missing database query context service")
        | service -> service :?> IDatabaseService
