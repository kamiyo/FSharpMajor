module FSharpMajor.InsertTasks

open System
open System.Dynamic
open System.Threading.Tasks
open System.Globalization
open FSharp.Interop.Dynamic

open Dapper.FSharp.PostgreSQL
open Dapper

open FSharpMajor.DatabaseTypes.``public``
open FSharpMajor.DatabaseTypes.``public``
open FSharpMajor.DatabaseTypes.``public``
open FSharpMajor.DatabaseTypes.``public``
open FSharpMajor.FsLibLog
open FSharpMajor.Database

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
        | [ single ] ->
            use! conn = npgsqlSource.OpenConnectionAsync()
            let! existing =
                select {
                    for a in artistsTable do
                        where (a.name = single.Name)
                }
                |> conn.SelectAsync<artists>
            match existing |> Seq.tryHead with
            | None ->
                return!
                    insert {
                        into artistsTable
                        value { id = Guid.NewGuid()
                                name = single.Name
                                image_url = single.ImageUrl }
                    }
                    |> conn.InsertOutputAsync<artists, artists>
            | Some _ ->
                return existing
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
        | [ single ] ->
            use! conn = npgsqlSource.OpenConnectionAsync()
            let! existing =
                select {
                    for alb in albumsTable do
                        where (alb.name = single.Name)
                }
                |> conn.SelectAsync<albums>
            match existing |> Seq.tryHead with
            | None ->
                return!
                    insert {
                        into albumsTable
                        value { id = Guid.NewGuid()
                                name = single.Name
                                year = single.Year }
                    }
                    |> conn.InsertOutputAsync<albums, albums>
            | Some _ ->
                return existing
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
        | [ single ] ->
            use! conn = npgsqlSource.OpenConnectionAsync()
            let! existing =
                select {
                    for art in coverArtTable do
                        where (art.hash = single.Hash)
                }
                |> conn.SelectAsync<cover_art>
            match existing |> Seq.tryHead with
            | None ->
                return!
                    insert {
                        into coverArtTable
                        value { id = Guid.NewGuid()
                                mime = single.MimeType
                                image = single.Data
                                path = single.Path
                                hash = single.Hash
                                created = DateTime.UtcNow }
                    }
                    |> conn.InsertOutputAsync<cover_art, cover_art>
            | Some _ ->
                return existing
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
        | [ single ] ->
            use! conn = npgsqlSource.OpenConnectionAsync()
            let! existing =
                select {
                    for g in genresTable do
                        where (g.name = single)
                }
                |> conn.SelectAsync<genres>
            
            match existing |> Seq.tryHead with
            | None ->
                return!
                    insert {
                        into genresTable
                        value { id = Guid.NewGuid(); name = single }
                    }
                    |> conn.InsertOutputAsync<genres, genres>
            | Some _ ->
                return existing
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

let insertArtistsAlbums (artistsAlbums: artists_albums list) =
    task {
        match artistsAlbums with
        | [] -> return Seq.empty
        | [ single ] ->
            use! conn = npgsqlSource.OpenConnectionAsync()
            let! existing =
                select {
                    for aa in artistsAlbumsTable do
                        where (aa.artist_id = single.artist_id && aa.album_id = single.album_id)
                }
                |> conn.SelectAsync<artists_albums>
                
            match existing |> Seq.tryHead with
            | None ->
                return!
                    insert {
                        into artistsAlbumsTable
                        value single
                    }
                    |> conn.InsertOutputAsync<artists_albums, artists_albums>
            | Some _ ->
                return existing
        | _ ->
            use! conn = npgsqlSource.OpenConnectionAsync()
            // FSharp.Dapper does not allow where with composite keys, so use Dapper
            // Instead of using IN or = ANY, we use unnest to zip the two arrays together
            // Select them as rows and inner join with the table.
            let query =
                "SELECT aa.* \
                 FROM UNNEST (@ArtistIds, @AlbumIds) AS params (artist, album) \
                 INNER JOIN artists_albums AS aa \
                    ON aa.artist_id = params.artist \
                    AND aa.album_id = params.album"

            let artistIds, albumIds =
                artistsAlbums
                |> List.map (fun aa -> aa.artist_id, aa.album_id)
                |> Array.ofList
                |> Array.unzip

            let existingResult = conn.Query<artists_albums>(query, {| ArtistIds = artistIds; AlbumIds = albumIds |})

            let found = existingResult |> Set.ofSeq

            let needed =
                artistsAlbums |> Set.ofList |> (fun s -> Set.difference s found) |> Set.toList

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
        | [ single ] ->
            use! conn = npgsqlSource.OpenConnectionAsync()
            let! existing =
                select {
                    for aa in itemsArtistsTable do
                        where (aa.item_id = single.item_id && aa.artist_id = single.artist_id)
                }
                |> conn.SelectAsync<items_artists>
                
            match existing |> Seq.tryHead with
            | None ->
                return!
                    insert {
                        into itemsArtistsTable
                        value single
                    }
                    |> conn.InsertOutputAsync<items_artists, items_artists>
            | Some _ ->
                return existing
        | _ ->
            use! conn = npgsqlSource.OpenConnectionAsync()
            // FSharp.Dapper does not allow where with composite keys, so use Dapper
            // Instead of using IN or = ANY, we use unnest to zip the two arrays together
            // Select them as rows and inner join with the table.
            let query =
                "SELECT ia.* \
                 FROM UNNEST (@ItemIds, @ArtistIds) AS params (item, artist) \
                 INNER JOIN items_artists AS ia \
                    ON ia.item_id = params.item \
                    AND ia.artist_id = params.artist"

            let itemIds, artistIds =
                itemsArtists
                |> List.map (fun ia -> ia.item_id, ia.artist_id)
                |> Array.ofList
                |> Array.unzip

            let existingResult = conn.Query<items_artists>(query, {| ItemIds = itemIds; ArtistIds = artistIds |})

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
        | [ single ] ->
            use! conn = npgsqlSource.OpenConnectionAsync()
            let! existing =
                select {
                    for ag in albumsGenresTable do
                        where (ag.album_id = single.album_id && ag.genre_id = single.genre_id)
                }
                |> conn.SelectAsync<albums_genres>
                
            match existing |> Seq.tryHead with
            | None ->
                return!
                    insert {
                        into albumsGenresTable
                        value single
                    }
                    |> conn.InsertOutputAsync<albums_genres, albums_genres>
            | Some _ ->
                return existing
        | _ ->
            use! conn = npgsqlSource.OpenConnectionAsync()
            // FSharp.Dapper does not allow where with composite keys, so use Dapper
            // Instead of using IN or = ANY, we use unnest to zip the two arrays together
            // Select them as rows and inner join with the table.
            let query =
                "SELECT ag.* \
                 FROM UNNEST (@AlbumIds, @GenreIds) AS params (album, genre) \
                 INNER JOIN albums_genres AS ag \
                    ON ag.album_id = params.album \
                    AND ag.genre_id = params.genre"

            let albumIds, genreIds =
                albumsGenres
                |> List.map (fun ag -> ag.album_id, ag.genre_id)
                |> Array.ofList
                |> Array.unzip

            let existingResult = conn.Query<albums_genres>(query, {| AlbumIds = albumIds; GenreIds = genreIds |})

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

let insertAlbumsCoverArt (albumsCoverArt: albums_cover_art list) =
    task {
        match albumsCoverArt with
        | [] -> return Seq.empty
        | [ single ] ->
            use! conn = npgsqlSource.OpenConnectionAsync()
            let! existing =
                select {
                    for ac in albumsCoverArtTable do
                        where (ac.album_id = single.album_id && ac.cover_art_id = single.cover_art_id)
                }
                |> conn.SelectAsync<albums_cover_art>
                
            match existing |> Seq.tryHead with
            | None ->
                return!
                    insert {
                        into albumsCoverArtTable
                        value single
                    }
                    |> conn.InsertOutputAsync<albums_cover_art, albums_cover_art>
            | Some _ ->
                return existing
        | _ ->
            use! conn = npgsqlSource.OpenConnectionAsync()
            // FSharp.Dapper does not allow where with composite keys, so use Dapper
            // Instead of using IN or = ANY, we use unnest to zip the two arrays together
            // Select them as rows and inner join with the table.
            let query =
                "SELECT ac.* \
                 FROM UNNEST (@AlbumIds, @CoverArtIds) AS params (album, cover) \
                 INNER JOIN albums_cover_art AS ac \
                    ON ac.album_id = params.album \
                    AND ac.cover_art_id = params.cover"

            let albumIds, coverArtIds =
                albumsCoverArt
                |> List.map (fun ac -> ac.album_id, ac.cover_art_id)
                |> Array.ofList
                |> Array.unzip

            let existingResult = conn.Query<albums_cover_art>(query, {| AlbumIds = albumIds; CoverArtIds = coverArtIds |})

            let found = existingResult |> Set.ofSeq

            let needed =
                albumsCoverArt |> Set.ofList |> (fun s -> Set.difference s found) |> Set.toList

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
        | [ single ] ->
            use! conn = npgsqlSource.OpenConnectionAsync()
            let! existing =
                select {
                    for aa in itemsAlbumsTable do
                        where (aa.item_id = single.item_id && aa.album_id = single.album_id)
                }
                |> conn.SelectAsync<items_albums>
                
            match existing |> Seq.tryHead with
            | None ->
                return!
                    insert {
                        into itemsAlbumsTable
                        value single
                    }
                    |> conn.InsertOutputAsync<items_albums, items_albums>
            | Some _ ->
                return existing
        | _ ->
            use! conn = npgsqlSource.OpenConnectionAsync()
            // FSharp.Dapper does not allow where with composite keys, so use Dapper
            // Instead of using IN or = ANY, we use unnest to zip the two arrays together
            // Select them as rows and inner join with the table.
            let query =
                "SELECT ia.* \
                 FROM UNNEST (@ItemIds, @AlbumIds) AS params (item, album) \
                 INNER JOIN items_albums AS ia \
                    ON ia.item_id = params.item \
                    AND ia.album_id = params.album"

            let itemIds, albumIds =
                itemsAlbums
                |> List.map (fun ia -> ia.item_id, ia.album_id)
                |> Array.ofList
                |> Array.unzip

            let existingResult = conn.Query<items_albums>(query, {| ItemIds = itemIds; AlbumIds = albumIds |})

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

let rec updateItemsLoop (updateList: directory_items list) (updated: directory_items list) (conn: Npgsql.NpgsqlConnection) =
    task {
        match updateList with
        | [] -> return updated
        | curr::rest ->
            let! result =
                update {
                    for d in directoryItemsTable do
                        set curr
                        where (d.id = curr.id)
                        excludeColumn d.id
                }
                |> conn.UpdateOutputAsync<directory_items, directory_items>
            let newUpdated = (result |> Seq.head)::updated
            return! updateItemsLoop rest newUpdated conn
    }

let insertOrUpdateDirectoryItem (directoryItems: directory_items list) =
    task {
        use! conn = npgsqlSource.OpenConnectionAsync()
        let logger = LogProvider.getLoggerByFunc()

        match directoryItems with
        | [] -> return Seq.empty
        | [ directoryItem ] ->
            let! exists =
                select {
                    for di in directoryItemsTable do
                        where (di.path = directoryItem.path)
                }
                |> conn.SelectAsync<directory_items>

            match exists |> Seq.tryHead with
            | None ->
                logger.info(Log.setMessage $"%A{directoryItem.path} to be inserted")
                return!
                    insert {
                        into directoryItemsTable
                        value directoryItem
                    }
                    |> conn.InsertOutputAsync<directory_items, directory_items>
            | Some _ ->
                logger.info(Log.setMessage $"%A{directoryItem.path} to be updated")
                return!
                    update {
                        for di in directoryItemsTable do
                            set directoryItem
                            where (di.path = directoryItem.path)
                            excludeColumn di.id
                    }
                    |> conn.UpdateOutputAsync<directory_items, directory_items>
        | _ ->
            let paths =
                directoryItems
                |> List.map (fun di -> di.path)

            let! exists =
                select {
                    for di in directoryItemsTable do
                        where (isIn di.path paths)
                }
                |> conn.SelectAsync<directory_items>

            let existingPaths =
                exists |> Seq.map (fun di -> di.path) |> List.ofSeq

            let toUpdate, toInsert =
                directoryItems
                |> List.partition (fun di -> existingPaths |> List.contains di.path)

            let! inserted =
                match toInsert with
                | [] -> Task.FromResult Seq.empty
                | _ ->
                    insert {
                        into directoryItemsTable
                        values toInsert
                    }
                    |> conn.InsertOutputAsync<directory_items, directory_items>
                
            let! updated =
                match toUpdate with
                | [] -> Task.FromResult Seq.empty
                | _ -> task {
                    let! result = updateItemsLoop toUpdate [] conn
                    return result |> Seq.ofList
                }              
                    
            return Seq.append inserted updated
    }