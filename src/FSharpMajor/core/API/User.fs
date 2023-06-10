module FSharpMajor.API.User

open Giraffe
open Microsoft.AspNetCore.Http
open FSharpMajor.DatabaseService
open SqlHydra.Query
open FSharpMajor.DatabaseTypes
open FSharpMajor.DatabaseTypes.``public``
open FSharpMajor.API.Types
open FSharpMajor.TypeMappers
open Error

let userHandler: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        let openContext = ctx.GetDatabaseQueryContext().CreateContext()

        match ctx.TryGetQueryStringValue "username" with
        | Some username ->
            let users =
                selectTask HydraReader.Read (Create openContext) {
                    for u in users do
                        where (u.username = username)
                        select u
                }

            match Seq.tryHead users.Result with
            | Some user ->
                let serializer = ctx.GetXmlSerializer()

                let body =
                    SubsonicResponse(children = XmlElements [| User(mapUserToAttributes (user)) |])
                    |> serializer.Serialize

                setBody body next ctx
            | None ->
                ctx.Items["subsonicCode"] <- ErrorEnum.NotFound
                subsonicError () next ctx
        | None ->
            ctx.Items["subsonicCode"] <- ErrorEnum.Params
            subsonicError () next ctx
