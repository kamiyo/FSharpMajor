module FSharpMajor.API.User

open Giraffe
open Microsoft.AspNetCore.Http
open FSharpMajor.DatabaseService
open FSharpMajor.API.Types
open FSharpMajor.TypeMappers
open FSharpMajor.DatabaseTypes
open Dapper.FSharp.PostgreSQL

open Error

let userHandler: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        use conn = ctx.GetDatabaseQueryContext().OpenConnection()

        match ctx.TryGetQueryStringValue "username" with
        | Some username when username = ctx.User.Identity.Name || ctx.User.IsInRole "Admin" ->

            let users =
                select {
                    for u in usersTable do
                        where (u.username = username)
                }
                |> conn.SelectAsync<users>

            match Seq.tryHead users.Result with
            | Some user ->
                let serializer = ctx.GetXmlSerializer()

                let body =
                    SubsonicResponse(children = XmlElements [| User(mapUserToAttributes user) |])
                    |> serializer.Serialize

                setBody body next ctx
            | None ->
                ctx.Items["subsonicCode"] <- ErrorEnum.NotFound
                subsonicError next ctx
        | Some _ ->
            ctx.Items["subsonicCode"] <- ErrorEnum.Unauthorized
            subsonicError next ctx
        | None ->
            ctx.Items["subsonicCode"] <- ErrorEnum.Params
            subsonicError next ctx
