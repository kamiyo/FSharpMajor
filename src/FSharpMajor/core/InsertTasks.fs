module FSharpMajor.InsertTasks

open System.Threading.Tasks
open FSharpMajor.DatabaseTypes.``public``
open FSharpMajor.Database
open Dapper.FSharp.PostgreSQL
open Dapper
open System.Data
open System
open System.IO
open System.Globalization
open Dapper
open FSharpMajor.FsLibLog

let coverArtTable = table<cover_art>
let directoryItemsTable = table<directory_items>
let artistsTable = table<artists>
let albumsTable = table<albums>
let genresTable = table<genres>
let itemsArtistsTable = table<items_artists>
let libraryRootsTable = table<library_roots>
let albumsCoverArtTable = table<albums_cover_art>
let itemsAlbumsTable = table<items_albums>
let artistsAlbumsTable = table<artists_albums>
let albumsGenresTable = table<albums_genres>

[<CustomComparison; CustomEquality>]
type ImageInfo =
    { Guid: Guid
      MimeType: string
      Path: string option
      Data: byte array option
      Hash: string }

    override __.Equals(obj: obj) =
        match obj with
        | :? ImageInfo as other -> __.Hash.Equals other.Hash
        | _ -> invalidArg (nameof obj) "Object is not an ImageInfo Record"

    override __.GetHashCode() =
        Int32.Parse(__.Hash, NumberStyles.HexNumber)

    interface IComparable with
        member __.CompareTo(obj: obj) =
            match obj with
            | null -> 1
            | :? ImageInfo as other -> __.Hash.CompareTo other.Hash
            | _ -> invalidArg (nameof obj) "Object is not an ImageInfo Record"

[<CustomComparison; CustomEquality>]
type ArtistInfo =
    { Guid: Guid
      Name: string
      ImageUrl: string option }

    override __.Equals(obj: obj) =
        match obj with
        | :? ArtistInfo as other -> __.Name.Equals other.Name
        | _ -> invalidArg (nameof obj) "Object is not an ImageInfo Record"

    override __.GetHashCode() = __.Name.GetHashCode()

    interface IComparable with
        member __.CompareTo(obj: obj) =
            match obj with
            | null -> 1
            | :? ArtistInfo as other -> __.Name.CompareTo other.Name
            | _ -> invalidArg (nameof obj) "Object is not an ImageInfo Record"

type AlbumInfo = { Name: string; Year: int option }

let albumsEqual (ai: AlbumInfo) (a: albums) =
    ai.Name.Equals a.name && ai.Year = a.year


let insertArtists (artistInfos: ArtistInfo list) =
    task {
        match artistInfos with
        | [] -> return Seq.empty
        | _ ->
            use! conn = npgsqlSource.OpenConnectionAsync()
            let artistNames = (artistInfos |> List.map (fun a -> a.Name))
            let! existingArtists =
                select {
                    for artist in artistsTable do
                        where (isIn artist.name artistNames)
                }
                |> conn.SelectAsync<artists>

            let foundNames = existingArtists |> List.ofSeq |> List.map (fun a -> a.name)

            let artistsToInsert =
                artistInfos
                |> List.filter (fun a -> foundNames |> List.contains a.Name |> not)
                |> List.map (fun a ->
                    { id = a.Guid
                      name = a.Name
                      image_url = a.ImageUrl }) // Use external services for image url

            match artistsToInsert with
            | [] -> return existingArtists
            | _ ->
                
                let! inserted =
                    insert {
                        for a in artistsTable do
                            values artistsToInsert
                            excludeColumn a.id
                    }
                    |> conn.InsertOutputAsync<artists, artists>

                return Seq.append inserted existingArtists
            }


let insertAlbums (albums: AlbumInfo list) =
    task {
        match albums with
        | [] -> return Seq.empty
        | _ ->
            use! conn = npgsqlSource.OpenConnectionAsync()

            let namesToCheck = albums |> List.map (fun a -> a.Name)

            let! existingAlbums =
                select {
                    for alb in albumsTable do
                        where (isIn alb.name namesToCheck) 
                }
                |> conn.SelectAsync<albums>

            let foundAlbumNames =
                existingAlbums
                |> List.ofSeq
                |> List.map (fun a -> { Name = a.name; Year = a.year })

            let albumsToInsert =
                albums
                |> List.filter (fun a -> foundAlbumNames |> List.contains a |> not)
                |> List.map (fun a ->
                    { id = Guid.Empty
                      name = a.Name
                      year = a.Year })

            match albumsToInsert with
            | [] -> return existingAlbums
            | _ ->
                let! inserted =
                    insert {
                        for alb in albumsTable do
                            values albumsToInsert
                            excludeColumn alb.id
                    }
                    |> conn.InsertOutputAsync<albums, albums>

                return Seq.append inserted existingAlbums
    }

let insertImages (images: ImageInfo list) =
    task {
        match images with
        | [] -> return Seq.empty
        | _ ->
            use! conn = npgsqlSource.OpenConnectionAsync()

            let hashesToCheck = images |> List.map (fun a -> a.Hash)

            let! existingImages =
                select {
                    for art in coverArtTable do
                        where (isIn art.hash hashesToCheck)
                }
                |> conn.SelectAsync<cover_art>

            let foundImageHashes = existingImages |> List.ofSeq |> List.map (fun a -> a.hash)

            let imagesToInsert =
                images
                |> List.filter (fun a -> foundImageHashes |> List.contains a.Hash |> not)
                |> List.map (fun a ->
                    { id = Guid.Empty
                      mime = a.MimeType
                      image = a.Data
                      path = a.Path
                      hash = a.Hash
                      created = DateTime.UtcNow })

            match imagesToInsert with
            | [] -> return existingImages
            | _ ->
                let! inserted =
                    insert {
                        for alb in coverArtTable do
                            values imagesToInsert
                            excludeColumn alb.id
                    }
                    |> conn.InsertOutputAsync<cover_art, cover_art>

                return Seq.append inserted existingImages

    }

let insertGenres (genres: string list) =
    task {
        match genres with
        | [] -> return Seq.empty
        | _ ->
            use! conn = npgsqlSource.OpenConnectionAsync()

            let! existingGenres =
                select {
                    for g in genresTable do
                        where (isIn g.name genres)
                }
                |> conn.SelectAsync<genres>

            let found = existingGenres |> Seq.map (fun g -> g.name) |> Set.ofSeq

            let needed =
                genres
                |> Set.ofList
                |> (fun s -> Set.difference s found)
                |> Set.toList
                |> List.map (fun g -> { id = Guid.Empty; name = g })

            match needed with
            | [] -> return existingGenres
            | _ ->
                let! inserted =
                    insert {
                        for g in genresTable do
                            values needed
                            excludeColumn g.id
                    }
                    |> conn.InsertOutputAsync<genres, genres>

                return Seq.append inserted existingGenres
    }

let insertAlbumsArtists (albumArtists: artists_albums list) =
    task {
        match albumArtists with
        | [] -> return Seq.empty
        | _ ->
            use! conn = npgsqlSource.OpenConnectionAsync()
            // FSharp.Dapper does not allow where with composite keys, so use Dapper
            let query =
                """select * from "public"."artists_albums" where ("artist_id", "album_id") in ( @InString )"""

            let inString =
                albumArtists
                |> List.map (fun aa -> $"('{aa.artist_id.ToString()}', '{aa.album_id.ToString()}')")
                |> String.concat ", "

            let! existingResult = conn.QueryAsync<artists_albums>(query, {| InString = inString |})

            let found = existingResult |> Set.ofSeq

            let needed =
                albumArtists |> Set.ofList |> (fun s -> Set.difference s found) |> Set.toList

            match needed with
            | [] -> return existingResult
            | _ ->
                let! inserted =
                    insert {
                        into artistsAlbumsTable
                        values needed
                    }
                    |> conn.InsertOutputAsync<artists_albums, artists_albums>

                return Seq.append inserted existingResult
    }

let insertItemsArtists (itemsArtists: items_artists list) =
    task {
        match itemsArtists with
        | [] -> return Seq.empty
        | _ ->
            use! conn = npgsqlSource.OpenConnectionAsync()
            // FSharp.Dapper does not allow where with composite keys, so use Dapper
            let query =
                """select * from "public"."items_artists" where ("item_id", "artist_id") in ( @InString )"""

            let inString =
                itemsArtists
                |> List.map (fun aa -> $"('{aa.item_id.ToString()}', '{aa.artist_id.ToString()}')")
                |> String.concat ", "

            let! existingResult = conn.QueryAsync<items_artists>(query, {| InString = inString |})

            let found = existingResult |> Set.ofSeq

            let needed =
                itemsArtists |> Set.ofList |> (fun s -> Set.difference s found) |> Set.toList

            match needed with
            | [] -> return existingResult
            | _ ->
                let! inserted =
                    insert {
                        into itemsArtistsTable
                        values needed
                    }
                    |> conn.InsertOutputAsync<items_artists, items_artists>

                return Seq.append inserted existingResult
    }

let insertAlbumsGenres (albumsGenres: albums_genres list) =
    task {
        match albumsGenres with
        | [] -> return Seq.empty
        | _ ->
            use! conn = npgsqlSource.OpenConnectionAsync()
            // FSharp.Dapper does not allow where with composite keys, so use Dapper
            let query =
                """select * from "public"."albums_genres" where ("album_id", "genre_id") in ( @InString )"""

            let inString =
                albumsGenres
                |> List.map (fun aa -> $"('{aa.album_id.ToString()}', '{aa.genre_id.ToString()}')")
                |> String.concat ", "

            let! existingResult = conn.QueryAsync<albums_genres>(query, {| InString = inString |})

            let found = existingResult |> Set.ofSeq

            let needed =
                albumsGenres |> Set.ofList |> (fun s -> Set.difference s found) |> Set.toList

            match needed with
            | [] -> return existingResult
            | _ ->
                let! inserted =
                    insert {
                        into albumsGenresTable
                        values needed
                    }
                    |> conn.InsertOutputAsync<albums_genres, albums_genres>

                return Seq.append inserted existingResult
    }

let insertAlbumsCoverArt (albumsArt: albums_cover_art list) =
    task {
        match albumsArt with
        | [] -> return Seq.empty
        | _ ->
            use! conn = npgsqlSource.OpenConnectionAsync()
            // FSharp.Dapper does not allow where with composite keys, so use Dapper
            let query =
                """select * from "public"."albums_art" where ("album_id", "cover_art_id") in ( @InString )"""

            let inString =
                albumsArt
                |> List.map (fun aa -> $"('{aa.album_id.ToString()}', '{aa.cover_art_id.ToString()}')")
                |> String.concat ", "

            let! existingResult = conn.QueryAsync<albums_cover_art>(query, {| InString = inString |})

            let found = existingResult |> Set.ofSeq

            let needed =
                albumsArt |> Set.ofList |> (fun s -> Set.difference s found) |> Set.toList

            match needed with
            | [] -> return existingResult
            | _ ->
                let! inserted =
                    insert {
                        into albumsCoverArtTable
                        values needed
                    }
                    |> conn.InsertOutputAsync<albums_cover_art, albums_cover_art>

                return Seq.append inserted existingResult          
    }

let insertItemsAlbums (itemsAlbums: items_albums list) =
    task {
        match itemsAlbums with
        | [] -> return Seq.empty
        | _ ->
            use! conn = npgsqlSource.OpenConnectionAsync()
            // FSharp.Dapper does not allow where with composite keys, so use Dapper
            let query =
                """select * from "public"."items_albums" where ("item_id", "album_id") in ( @InString )"""

            let inString =
                itemsAlbums
                |> List.map (fun aa -> $"('{aa.item_id.ToString()}', '{aa.album_id.ToString()}')")
                |> String.concat ", "

            let! existingResult = conn.QueryAsync<items_albums>(query, {| InString = inString |})

            let found = existingResult |> Set.ofSeq

            let needed =
                itemsAlbums |> Set.ofList |> (fun s -> Set.difference s found) |> Set.toList

            match needed with
            | [] -> return existingResult
            | _ ->
            
                let! inserted =
                    insert {
                        into itemsAlbumsTable
                        values needed
                    }
                    |> conn.InsertOutputAsync<items_albums, items_albums>

                return Seq.append inserted existingResult
    }        

let insertDirectoryItem (directoryItems: directory_items list) =
    task {
        use! conn = npgsqlSource.OpenConnectionAsync()
        let logger = LogProvider.getLoggerByFunc()

        match directoryItems with
        | [] -> return Seq.empty
        | [ directoryItem ] ->
            let! exists =
                match directoryItem.path with
                | None -> Task.FromResult Seq.empty
                | Some _ ->
                    select {
                        for di in directoryItemsTable do
                            where (di.path = directoryItem.path)
                    }
                    |> conn.SelectAsync<directory_items>

            match exists |> Seq.tryHead with
            | None ->
                logger.info(Log.setMessage $"{directoryItem.id} to be inserted")
                return!
                    insert {
                        into directoryItemsTable
                        value directoryItem
                    }
                    |> conn.InsertOutputAsync<directory_items, directory_items>
            | Some _ -> return exists
        | _ ->
            let paths =
                directoryItems
                |> List.choose (fun di -> di.path)
                |> List.map Some

            let! exists =
                select {
                    for di in directoryItemsTable do
                        where (isIn di.path paths)
                }
                |> conn.SelectAsync<directory_items>

            let existingPaths = exists |> Seq.choose (fun di -> di.path) |> List.ofSeq

            let toInsert =
                directoryItems
                |> List.filter (fun di ->
                    match di.path with
                    | None -> true
                    | Some path -> existingPaths |> List.contains path |> not)

            return!
                insert {
                    into directoryItemsTable
                    values toInsert
                }
                |> conn.InsertOutputAsync<directory_items, directory_items>
    }