module FSharpMajor.API.Indexes


let queryParamsKeys = [ "musicFolderId"; "ifModifiedSince" ]
//
// let indexesHandler: HttpHandler =
//     fun (next: HttpFunc) (ctx: HttpContext) ->
//         use conn = ctx.GetDatabaseQueryContext().OpenConnection()
//
//         let query =
//             match queryParamsKeys |> List.map ctx.TryGetQueryStringValue with
//             | None::[ None ] ->
//                 select {
//                     for a in artistsTable do
//                         selectAll
//                 }
//             | (Some )
//
//         let libraryFolders =
//             foldersTask.Result
//             |> Array.ofSeq
//             |> Array.map (fun r -> MusicFolder(MusicFolderAttributes(id = r.id.ToString(), name = Some r.name)))
//
//         let serializer = ctx.GetXmlSerializer()
//
//         let body =
//             SubsonicResponse(children = XmlElements [| MusicFolders(children = libraryFolders) |])
//             |> serializer.Serialize
//
//         setBody body next ctx
//
