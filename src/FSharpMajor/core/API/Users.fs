module FSharpMajor.API.Users

open Giraffe
open Microsoft.AspNetCore.Http
open FSharpMajor.DatabaseService
open FSharpMajor.DatabaseTypes
open FSharpMajor.API.Types
open FSharpMajor.TypeMappers
open Dapper.FSharp.PostgreSQL

let usersHandler: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        let conn = ctx.GetDatabaseQueryContext().Connection
        let usersTable = table<users>

        let usersTask =
            select {
                for u in usersTable do
                    selectAll
            }
            |> conn.SelectAsync<users>

        let users =
            usersTask.Result
            |> Array.ofSeq
            |> Array.map (fun u -> User(mapUserToAttributes u))

        let serializer = ctx.GetXmlSerializer()

        let body =
            SubsonicResponse(children = XmlElements [| Users(children = users) |])
            |> serializer.Serialize

        setBody body next ctx
