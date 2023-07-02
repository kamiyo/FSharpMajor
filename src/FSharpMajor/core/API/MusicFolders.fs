module FSharpMajor.API.MusicFolders

open Giraffe
open Microsoft.AspNetCore.Http
open FSharpMajor.DatabaseService
open FSharpMajor.DatabaseTypes
open FSharpMajor.API.Types
open Dapper.FSharp.PostgreSQL

let musicFoldersHandler: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        use conn = ctx.GetDatabaseQueryContext().OpenConnection()

        let foldersTask =
            select {
                for u in libraryRootsTable do
                    selectAll
            }
            |> conn.SelectAsync<library_roots>

        let libraryFolders =
            foldersTask.Result
            |> Array.ofSeq
            |> Array.map (fun r -> MusicFolder(MusicFolderAttributes(id = r.id.ToString(), name = Some r.name)))

        let serializer = ctx.GetXmlSerializer()

        let body =
            SubsonicResponse(children = XmlElements [| MusicFolders(children = libraryFolders) |])
            |> serializer.Serialize

        setBody body next ctx
