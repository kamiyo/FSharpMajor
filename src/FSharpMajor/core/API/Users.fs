module FSharpMajor.API.Users

open Giraffe
open Microsoft.AspNetCore.Http
open FSharpMajor.DatabaseService
open SqlHydra.Query
open FSharpMajor.DatabaseTypes
open FSharpMajor.DatabaseTypes.``public``
open FSharpMajor.API.Types
open FSharpMajor.TypeMappers

let usersHandler: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        let openContext = ctx.GetDatabaseQueryContext().CreateContext()

        let usersTask =
            selectTask HydraReader.Read (Create openContext) {
                for u in users do
                    select u
            }

        let users =
            usersTask.Result
            |> Array.ofSeq
            |> Array.map (fun u -> User(mapUserToAttributes u))

        let serializer = ctx.GetXmlSerializer()

        let body =
            SubsonicResponse(children = XmlElements [| Users(children = users) |])
            |> serializer.Serialize

        setBody body next ctx
