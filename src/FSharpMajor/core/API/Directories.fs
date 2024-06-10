module FSharpMajor.API.Directories

open System
open System.Text.RegularExpressions
open Giraffe
open Microsoft.AspNetCore.Http
open FSharpMajor.DatabaseService
open FSharpMajor.DatabaseTypes
open FSharpMajor.API.Types
open Dapper

// let directories: HttpHandler =
//     fun (next: HttpFunc) (ctx: HttpContext) ->
//         use conn = ctx.GetDatabaseQueryContext().OpenConnection()
//         let username = ctx.User.Identity.Name
//
//         let serializer = ctx.GetXmlSerializer()
//         let body =
//             SubsonicResponse(
//                 children =
//                     XmlElements [|
//                         Indexes(IndexesAttributes(modifiedSince.ToString(), ignoredArticles), indexes)
//                     |]
//             )
//             |> serializer.Serialize
//
//         setBody body next ctx
