module FSharpMajor.DatabaseService

open System
open System.Runtime.CompilerServices

open Microsoft.AspNetCore.Http

open SqlHydra.Query
open FSharpMajor.Database

type IDatabaseService =
    abstract member CreateContext: unit -> (unit -> QueryContext)

type DatabaseService() =
    interface IDatabaseService with
        member __.CreateContext() = openContext

[<Extension>]
type HttpContextExtensions() =
    [<Extension>]
    static member GetDatabaseQueryContext(ctx: HttpContext) =
        match ctx.RequestServices.GetService typeof<IDatabaseService> with
        | null -> raise (Exception "Missing database query context service")
        | service -> service :?> IDatabaseService
