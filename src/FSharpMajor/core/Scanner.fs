module FSharpMajor.Scanner

open System
open System.IO
open System.Security.Cryptography
open FSharpMajor.DatabaseTypes.``public``
open Microsoft.AspNetCore.StaticFiles

open Dapper.FSharp.PostgreSQL

open FSharpMajor.FsLibLog
open FSharpMajor.DatabaseTypes.``public``
open FSharpMajor.Database
open FSharpMajor.InsertTasks

let MIMEProvider = FileExtensionContentTypeProvider()
let md5 = MD5.Create()

let getMimeType (file: string) =
    let mutable mimeType = ""

    match MIMEProvider.TryGetContentType(file, &mimeType) with
    | false -> "application/octet-stream"
    | true -> mimeType

let CaseInsensitiveEnumOption =
    EnumerationOptions(MatchCasing = MatchCasing.CaseInsensitive)

let rec createSetOfImages (images: TagLib.IPicture list) (accum: ImageInfo Set) =
    match images with
    | [] -> accum
    | image :: rest ->
        let imageHash = md5.ComputeHash image.Data.Data |> Convert.ToHexString

        let toInsert =
            { Guid = Guid.NewGuid()
              MimeType = image.MimeType
              Path = None
              Data = Some image.Data.Data
              Hash = imageHash }

        createSetOfImages rest (accum |> Set.add toInsert)

let getImagesFromTag (file: FileInfo) =
    let tags = TagLib.File.Create file.FullName

    match tags.Tag.Pictures with
    | [||] -> Set.empty
    | images -> createSetOfImages (images |> List.ofArray) Set.empty

let getMediaType (tagFile: TagLib.File) =
    let genres = tagFile.Tag.Genres

    if Array.contains "audiobook" genres then "audiobook"
    else if Array.contains "podcast" genres then "podcast"
    else if tagFile.MimeType.Contains "video" then "video"
    else "music"

let createItem (fi: FileInfo) (tags: TagLib.File) (parentId: Guid option) (albumFromPath: bool) (artistFromPath: bool) =
    let name =
        match tags.Tag.Title with
        | null -> (Path.GetFileNameWithoutExtension fi.Name)
        | title -> title

    { id = Guid.NewGuid()
      parent_id = parentId
      name = Some name
      is_dir = false
      track = Some(int tags.Tag.Track)
      year = Some(int tags.Tag.Year)
      size = Some fi.Length
      content_type = Some tags.MimeType
      suffix = Some(fi.Extension.Substring 1)
      duration = Some tags.Properties.Duration.Seconds
      bit_rate = Some tags.Properties.AudioBitrate
      path = Some fi.FullName
      is_video = Some(tags.MimeType.Contains "video")
      disc_number = Some(int tags.Tag.Disc)
      created = DateTime.UtcNow
      ``type`` = Some(getMediaType tags)
      album_from_path = albumFromPath
      artist_from_path = artistFromPath }

let createDirectory (currentDir: DirectoryInfo) (parentId: Guid option) =
    { id = Guid.NewGuid()
      parent_id = parentId
      name = Some currentDir.Name
      is_dir = true
      track = None
      year = None
      size = None
      content_type = None
      suffix = None
      duration = None
      bit_rate = None
      path = Some currentDir.FullName
      is_video = None
      disc_number = None
      created = DateTime.UtcNow
      ``type`` = None
      album_from_path = false
      artist_from_path = false }

type FileResult =
    { Album: AlbumInfo
      Images: ImageInfo list
      Genres: string list
      Artists: ArtistInfo list
      Item: directory_items
      ItemAlbum: (Guid * AlbumInfo)
      AlbumArtists: (AlbumInfo * ArtistInfo) list
      ItemArtists: (Guid * ArtistInfo) list
      AlbumCoverArt: (AlbumInfo * ImageInfo) list
      AlbumGenres: (AlbumInfo * string) list }

type ReducedResult =
    { Albums: AlbumInfo Set
      Images: ImageInfo Set
      Genres: string Set
      Artists: ArtistInfo Set
      Items: directory_items list
      ItemsAlbums: (Guid * AlbumInfo) Set
      AlbumsArtists: (AlbumInfo * ArtistInfo) Set
      ItemsArtists: (Guid * ArtistInfo) Set
      AlbumsCoverArt: (AlbumInfo * ImageInfo) Set
      AlbumsGenres: (AlbumInfo * string) Set }

    static member Default =
        { Albums = Set.empty
          Images = Set.empty
          Genres = Set.empty
          Artists = Set.empty
          Items = []
          ItemsAlbums = Set.empty
          AlbumsArtists = Set.empty
          ItemsArtists = Set.empty
          AlbumsCoverArt = Set.empty
          AlbumsGenres = Set.empty }

type ScanResult =
    | ImageResult of ImageInfo
    | FileResult of FileResult
    | NoResult

let scanImage (file: FileInfo) (mimeType: string) =
    let imageFile = file.OpenRead()
    let hash = md5.ComputeHash imageFile |> Convert.ToHexString

    let toInsert =
        { Guid = Guid.NewGuid()
          MimeType = mimeType
          Data = None
          Path = Some file.FullName
          Hash = hash }

    ImageResult toInsert

let imageMimeTypes = [ "jpeg"; "jpg"; "png" ]

let isImage (mimeType: string) =
    imageMimeTypes
    |> List.exists (fun t -> mimeType.Contains(t, StringComparison.InvariantCultureIgnoreCase))

let scanFile (rootDirInfo: DirectoryInfo) (fileInfo: FileInfo) (parentId: Guid option) =
    let logger = LogProvider.getLoggerByFunc ()

    try
        match fileInfo.Exists with
        | false -> NoResult
        | true ->
            let tags = TagLib.File.Create fileInfo.FullName

            match tags.MimeType with
            | mime when isImage mime ->
                // Image, so insert it as album art
                scanImage fileInfo mime
            | _ ->
                let logger = LogProvider.getLoggerByFunc ()
                logger.info (Log.setMessage $"Scanning File: {fileInfo.Name}, {fileInfo.FullName}")
                // Insert images in tag
                let images = getImagesFromTag fileInfo |> Seq.toList

                // let insertedImagesTask = insertCoverArt conn toInsert

                // Get Album from Tags
                let albumName, albumFromPath =
                    match tags.Tag.Album with
                    | null
                    | "" ->
                        let parentsParent = fileInfo.Directory.Parent

                        match rootDirInfo.Equals parentsParent with // if ../ dir is root, then label current dir as album
                        | true -> fileInfo.Directory.Name, true
                        | false -> fileInfo.Directory.Parent.Name, true
                    | name -> name, false

                let albumYear =
                    match int tags.Tag.Year with
                    | 0 -> None
                    | year -> Some year

                let genres =
                    tags.Tag.Genres
                    |> Array.map (fun g -> g.Split ';')
                    |> Array.reduce Array.append
                    |> List.ofArray

                let artistTags, artistsFromPath =
                    match tags.Tag.Performers with
                    | [||] ->
                        let parentsParent = fileInfo.Directory.Parent

                        match rootDirInfo.Equals parentsParent with // if ../ dir is root, then no artist
                        | true -> [], true
                        | false -> [ fileInfo.Directory.Parent.Name ], true
                    | artists ->
                        let separatedArtists =
                            artists
                            |> Array.map (fun a -> a.Split ';')
                            |> Array.reduce Array.append
                            |> List.ofArray

                        separatedArtists, false

                let diInstance = createItem fileInfo tags parentId albumFromPath artistsFromPath

                let album = { Name = albumName; Year = albumYear }

                let artists =
                    artistTags
                    |> List.map (fun a ->
                        { Guid = Guid.Empty
                          Name = a
                          ImageUrl = None })
                // Time for relations
                let albumArtists = [ for artist in artists -> (album, artist) ]

                let itemArtists = [ for artist in artists -> (diInstance.id, artist) ]

                let albumGenres = [ for genre in genres -> (album, genre) ]

                let albumCoverArt = [ for image in images -> (album, image) ]

                FileResult
                    { Album = album
                      Images = images
                      Genres = genres
                      Artists = artists
                      Item = diInstance
                      ItemAlbum = (diInstance.id, album)
                      AlbumArtists = albumArtists
                      ItemArtists = itemArtists
                      AlbumCoverArt = albumCoverArt
                      AlbumGenres = albumGenres }
    with
    | :? TagLib.UnsupportedFormatException as ex ->
        logger.info (Log.setMessage $"File {fileInfo.FullName} is not media type.")
        NoResult
    | error ->
        logger.error (Log.setMessage $"{error.Message}")
        raise error

// Recursive traverse directories
// Pop current directory from list,
// Scan all files, accumulating results,
// Insert into database,
// Then add found directories onto the list to feed into recursion.
let rec traverseDirectories (rootDirInfo: DirectoryInfo) (dirAndParents: (DirectoryInfo * Guid option) list) =
    match dirAndParents with
    | [] -> ()
    | (currentDir, parentId) :: rest ->
        let logger = LogProvider.getLoggerByFunc ()
        logger.info (Log.setMessage $"Current Dir: {currentDir}")

        try
            try
                match currentDir.GetFiles() |> Array.sortBy (fun d -> d.Name) with
                | [||] -> ()
                | files ->
                    logger.debug (Log.setMessage $"Found files in currentDir: {files.Length}")
                    let fileList = List.ofArray files

                    // Make a directory directoryItem
                    let directory = createDirectory currentDir parentId

                    // Insert directory info into DB as directory_item
                    let insertDirectoryTask = insertDirectoryItem [ directory ]

                    let insertedDirectory = insertDirectoryTask.Result |> Seq.head

                    // Scan children files
                    // Can parallelize this?
                    let scanResult =
                        fileList
                        |> List.map (fun f -> scanFile rootDirInfo f (Some insertedDirectory.id))

                    logger.debug (Log.setMessage $"Scanned in folder: {scanResult.Length}")

                    // Separate results into their types by folding
                    let (imageInfos, fileResults): (ImageInfo Set * FileResult list) =
                        scanResult
                        |> List.fold
                            (fun acc r ->
                                match r with
                                | NoResult -> acc
                                | ImageResult i -> (Set.add i (fst acc), snd acc)
                                | FileResult f ->
                                    let prev = (snd acc)
                                    (fst acc, f :: prev))
                            (Set.empty, List<FileResult>.Empty)

                    logger.debug (Log.setMessage $"Images: {imageInfos |> Set.count}, Files: {fileResults.Length}")

                    // Create an accumulator with everything empty, but add in the imageInfos we got from image files
                    let accumulator =
                        { ReducedResult.Default with
                            Images = imageInfos |> Set.fold (fun acc -> acc.Add) Set.empty }
                    // Merge them into sets, so we don't insert duplicates
                    let reduced =
                        fileResults
                        |> List.fold
                            (fun accum r ->
                                { Albums = accum.Albums.Add r.Album
                                  Images = r.Images |> List.fold (fun acc i -> acc.Add i) accum.Images
                                  Genres = r.Genres |> List.fold (fun acc g -> acc.Add g) accum.Genres
                                  Artists = r.Artists |> List.fold (fun acc a -> acc.Add a) accum.Artists
                                  Items = r.Item :: accum.Items
                                  ItemsAlbums = accum.ItemsAlbums.Add r.ItemAlbum
                                  AlbumsArtists =
                                    r.AlbumArtists |> List.fold (fun acc aa -> acc.Add aa) accum.AlbumsArtists
                                  ItemsArtists =
                                    r.ItemArtists |> List.fold (fun acc ia -> acc.Add ia) accum.ItemsArtists
                                  AlbumsCoverArt =
                                    r.AlbumCoverArt |> List.fold (fun acc aa -> acc.Add aa) accum.AlbumsCoverArt
                                  AlbumsGenres =
                                    r.AlbumGenres |> List.fold (fun acc ag -> acc.Add ag) accum.AlbumsGenres })
                            accumulator

                    logger.debug (Log.setMessage $"Items: {reduced.Items.Length}")

                    logger.debug (
                        Log.setMessage
                            $"""Items: {reduced.Items |> List.choose (fun i -> i.path) |> String.concat ", "}"""
                    )

                    // First insert albums (which will check existing)
                    let insertAlbumsTask = insertAlbums (reduced.Albums |> Set.toList)
                    // Insert cover art
                    let insertImagesTask = insertImages (reduced.Images |> Set.toList)
                    // Insert artists, TODO add image_url later
                    let insertArtistsTask = insertArtists (reduced.Artists |> Set.toList)
                    // Insert genres
                    let insertGenresTask = insertGenres (reduced.Genres |> Set.toList)
                    // Insert directory_items
                    let insertItemsTask = insertDirectoryItem reduced.Items

                    let albums = insertAlbumsTask.Result
                    let images = insertImagesTask.Result
                    let artists = insertArtistsTask.Result
                    let items = insertItemsTask.Result
                    let genres = insertGenresTask.Result

                    logger.debug (Log.setMessage $"albums: %A{albums}")
                    logger.debug (Log.setMessage $"images: %A{images}")
                    logger.debug (Log.setMessage $"artists: %A{artists}")
                    logger.debug (Log.setMessage $"items: %A{items}")
                    logger.debug (Log.setMessage $"genres: %A{genres}")

                    // Time for relations
                    // Create the objects to insert first, then insert
                    let albumsArtistsToInsert =
                        reduced.AlbumsArtists
                        |> Set.toList
                        |> List.map (fun (albumInfo, artistInfo) ->
                            let artist = artists |> Seq.find (fun a -> a.name = artistInfo.Name)

                            let album = albums |> Seq.find (albumsEqual albumInfo)

                            { artist_id = artist.id
                              album_id = album.id })

                    let insertAlbumsArtistsTask = insertAlbumsArtists albumsArtistsToInsert

                    let directoryArtists =
                        [ for artist in artists ->
                              { item_id = insertedDirectory.id
                                artist_id = artist.id } ]

                    let itemsArtistsToInsert =
                        reduced.ItemsArtists
                        |> Set.toList
                        |> List.map (fun (guid, artistInfo) ->
                            let artist = artists |> Seq.find (fun a -> a.name = artistInfo.Name)

                            { artist_id = artist.id
                              item_id = guid })
                        |> List.append directoryArtists

                    let insertItemsArtistsTask = insertItemsArtists itemsArtistsToInsert

                    let albumsCoverArtToInsert =
                        reduced.AlbumsCoverArt
                        |> Set.toList
                        |> List.map (fun (albumInfo, imageInfo) ->
                            let album = albums |> Seq.find (albumsEqual albumInfo)
                            let image = images |> Seq.find (fun i -> i.hash = imageInfo.Hash)

                            { cover_art_id = image.id
                              album_id = album.id })

                    let insertAlbumsCoverArtTask = insertAlbumsCoverArt albumsCoverArtToInsert

                    let albumsGenresToInsert =
                        reduced.AlbumsGenres
                        |> Set.toList
                        |> List.map (fun (albumInfo, genreString) ->
                            let album = albums |> Seq.find (albumsEqual albumInfo)
                            let genre = genres |> Seq.find (fun g -> g.name = genreString)

                            { album_id = album.id
                              genre_id = genre.id })

                    let insertAlbumsGenresTask = insertAlbumsGenres albumsGenresToInsert

                    let directoryAlbums =
                        [ for album in albums ->
                              { item_id = insertedDirectory.id
                                album_id = album.id } ]

                    let itemsAlbumsToInsert =
                        reduced.ItemsAlbums
                        |> Set.toList
                        |> List.map (fun (guid, albumInfo) ->
                            let album = albums |> Seq.find (albumsEqual albumInfo)
                            { item_id = guid; album_id = album.id })
                        |> List.append directoryAlbums

                    let insertItemsAlbumsTask = insertItemsAlbums itemsAlbumsToInsert

                    // Make sure all tasks done
                    let albumsArtists = insertAlbumsArtistsTask.Result
                    let itemsArtists = insertItemsArtistsTask.Result
                    let itemsAlbums = insertItemsAlbumsTask.Result
                    let albumsCoverArt = insertAlbumsCoverArtTask.Result
                    let albumsGenres = insertAlbumsGenresTask.Result

                    logger.debug (Log.setMessage $"albumsArtists: %A{albumsArtists}")
                    logger.debug (Log.setMessage $"itemsArtists: %A{itemsArtists}")
                    logger.debug (Log.setMessage $"itemsAlbums: %A{itemsAlbums}")
                    logger.debug (Log.setMessage $"albumsCoverArt: %A{albumsCoverArt}")
                    logger.debug (Log.setMessage $"albumsGenres: %A{albumsGenres}")
                    ()
            // Should notify mailboxprocessor of Seq.length items
            with exn ->
                logger.error (Log.setMessage $"%A{exn}")
        finally
            let newRest =
                try
                    match currentDir.GetDirectories() with
                    | [||] -> rest
                    | dirs ->
                        dirs
                        |> List.ofArray
                        |> List.fold (fun state dir -> (dir, parentId) :: state) rest
                with _ ->
                    rest
            // Recurse
            traverseDirectories rootDirInfo newRest


let startTraverseDirectories () =
    task {
        use! conn = npgsqlSource.OpenConnectionAsync()

        let! roots =
            select {
                for _root in libraryRootsTable do
                    selectAll
            }
            |> conn.SelectAsync<library_roots>


        for root in roots do
            let dirInfo = DirectoryInfo(root.path)

            // Later, also check if scan_completed is not null (Some)
            match dirInfo.Exists with
            | false ->
                let logger = LogProvider.getLoggerByFunc ()
                logger.warn (Log.setMessage $"Library root {root.name} at {root.path} does not exist.")
            | true ->
                let dirs: (DirectoryInfo * Guid option) list =
                    dirInfo.GetDirectories()
                    |> Array.sortBy (fun di -> di.Name)
                    |> List.ofArray
                    |> List.map (fun d -> (d, None))

                traverseDirectories dirInfo dirs

                let updatedRoot =
                    { root with
                        scan_completed = Some DateTime.UtcNow }

                let! _ =
                    update {
                        for r in libraryRootsTable do
                            set updatedRoot
                            includeColumn r.scan_completed
                            where (r.id = root.id)
                    }
                    |> conn.UpdateAsync<library_roots>

                ()
    }

let ScanForUpdates () =
    task {
        use! conn = npgsqlSource.OpenConnectionAsync()

        let! roots =
            select {
                for _root in libraryRootsTable do
                    selectAll
            }
            |> conn.SelectAsync<library_roots>


        for root in roots do
            let dirInfo = DirectoryInfo(root.path)

            // Get directories
            let! directories =
                select {
                    for items in directoryItemsTable do
                        where (items.is_dir = true)
                }
                |> conn.SelectAsync<directory_items>
    }