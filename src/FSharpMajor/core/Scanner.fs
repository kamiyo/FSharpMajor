module FSharpMajor.Scanner

#nowarn "3391"

open System
open System.IO
open System.Diagnostics
open System.Security.Cryptography
open System.Threading.Tasks
open FSharpMajor.DatabaseTypes
open Microsoft.AspNetCore.StaticFiles

open Dapper.FSharp.PostgreSQL

open FSharpMajor.FsLibLog
open FSharpMajor.Database
open FSharpMajor.InsertTasks

let MIMEProvider = FileExtensionContentTypeProvider()
MIMEProvider.Mappings.Add(".flac", "audio/flac")
let md5 = MD5.Create()

let getMimeType (file: string) =
    let mutable mimeType = ""

    match MIMEProvider.TryGetContentType(file, &mimeType) with
    | false -> "application/octet-stream"
    | true -> mimeType

let CaseInsensitiveEnumOption =
    EnumerationOptions(MatchCasing = MatchCasing.CaseInsensitive)

let rec createSetOfImages (images: TagLib.IPicture list) (accum: cover_art Set) =
    match images with
    | [] -> accum
    | image :: rest ->
        let imageHash = md5.ComputeHash image.Data.Data |> Convert.ToHexString

        let toInsert =
            { id = Guid.NewGuid()
              mime = image.MimeType
              path = None
              image = Some image.Data.Data
              hash = imageHash
              created = DateTime.UtcNow }

        createSetOfImages rest (accum |> Set.add toInsert)

let getImagesFromTag (file: FileInfo) =
    let tags = TagLib.File.Create file.FullName

    match tags.Tag.Pictures with
    | [||] -> Set.empty
    | images -> createSetOfImages (images |> List.ofArray) Set.empty

let getMediaType (tagFile: TagLib.File) =
    let genres = tagFile.Tag.Genres

    if Array.contains "audiobook" genres then
        Some "audiobook"
    else if Array.contains "podcast" genres then
        Some "podcast"
    else
        match getMimeType tagFile.Name with
        | mime when mime.Contains "video" -> Some "video"
        | mime when mime.Contains "audio" -> Some "audio"
        | _ -> None

let createItem (fi: FileInfo) (tags: TagLib.File) (musicFolderId: Guid) (parentId: Guid option) =
    let name =
        match tags.Tag.Title with
        | null -> (Path.GetFileNameWithoutExtension fi.Name)
        | title -> title

    let mime = getMimeType fi.Extension

    { id = Guid.NewGuid()
      parent_id = parentId
      music_folder_id = musicFolderId
      name = Some name
      is_dir = false
      track = Some(int tags.Tag.Track)
      year = Some(int tags.Tag.Year)
      size = Some fi.Length
      content_type = Some mime
      suffix = Some(fi.Extension.Substring 1)
      duration = Some tags.Properties.Duration.Seconds
      bit_rate = Some tags.Properties.AudioBitrate
      path = fi.FullName
      is_video = Some(mime.Contains "video")
      disc_number = Some(int tags.Tag.Disc)
      created = DateTime.UtcNow
      ``type`` = getMediaType tags }

let createDirectory (currentDir: DirectoryInfo) (musicFolderId: Guid) (parentId: Guid option) =
    { id = Guid.NewGuid()
      parent_id = parentId
      music_folder_id = musicFolderId
      name = Some currentDir.Name
      is_dir = true
      track = None
      year = None
      size = None
      content_type = None
      suffix = None
      duration = None
      bit_rate = None
      path = currentDir.FullName
      is_video = None
      disc_number = None
      created = DateTime.UtcNow
      ``type`` = None }

type FileResult =
    { Album: albums
      Images: cover_art list
      Genres: genres list
      Artists: artists list
      Item: directory_items
      ItemAlbum: string * albums
      AlbumArtists: (albums * artists) list
      ItemArtists: (string * artists) list
      AlbumCoverArt: (albums * cover_art) list
      AlbumGenres: (albums * genres) list }

type ReducedResult =
    { Albums: albums Set
      Images: cover_art Set
      Genres: genres Set
      Artists: artists Set
      Items: directory_items list
      ItemsAlbums: (string * albums) Set
      AlbumsArtists: (albums * artists) Set
      ItemsArtists: (string * artists) Set
      AlbumsCoverArt: (albums * cover_art) Set
      AlbumsGenres: (albums * genres) Set }

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
    | ImageResult of cover_art
    | FileResult of FileResult
    | NoResult

let scanImage (file: FileInfo) (mimeType: string) =
    let imageFile = file.OpenRead()
    let hash = md5.ComputeHash imageFile |> Convert.ToHexString

    let toInsert =
        { id = Guid.NewGuid()
          mime = mimeType
          image = None
          path = Some file.FullName
          hash = hash
          created = DateTime.UtcNow }

    ImageResult toInsert

let imageMimeTypes = [ "jpeg"; "jpg"; "png" ]

let isImage (mimeType: string) =
    imageMimeTypes
    |> List.exists (fun t -> mimeType.Contains(t, StringComparison.InvariantCultureIgnoreCase))

let rec findFirstLevelDir (rootDirInfo: DirectoryInfo) (currentDir: DirectoryInfo) =
    match currentDir.Parent with
    | d when d = rootDirInfo -> currentDir
    | d -> findFirstLevelDir rootDirInfo d

let scanFile (rootDirInfo: DirectoryInfo) (musicFolderId: Guid) (fileInfo: FileInfo) (parentId: Guid option) =

    match fileInfo.Exists with
    | false -> Task.FromResult NoResult
    | true ->
        task {
            let logger = LogProvider.getLoggerByFunc ()

            try

                let tags = TagLib.File.Create fileInfo.FullName

                match tags.MimeType with
                | mime when isImage mime ->
                    // Image, return it as album art
                    return scanImage fileInfo mime
                | _ ->
                    let logger = LogProvider.getLoggerByFunc ()
                    logger.debug (Log.setMessage $"Scanning File: {fileInfo.Name}, {fileInfo.FullName}")
                    // Insert images in tag
                    let images = getImagesFromTag fileInfo |> Seq.toList

                    // Get Album from Tags
                    let albumName, albumFromPath =
                        match tags.Tag.Album with
                        | null
                        | "" -> fileInfo.Directory.Name, true // If no album tag, then get from dir name
                        | name -> name, false

                    let albumYear =
                        match int tags.Tag.Year with
                        | 0 -> None
                        | year -> Some year

                    let genres =
                        match tags.Tag.Genres |> Array.map (fun g -> g.Split ';') with
                        | [||] -> List.empty
                        | arrayArray ->
                            arrayArray
                            |> Array.reduce Array.append
                            |> List.ofArray
                            |> List.map (fun g -> { genres.id = Guid.NewGuid(); name = g })

                    let artistTags, artistsFromPath =
                        match tags.Tag.Performers with
                        | [||] -> // We will get artist from first level dir under library root
                            match findFirstLevelDir rootDirInfo fileInfo.Directory with
                            | d when d = fileInfo.Directory -> // If file's dir is first level, then no artist
                                [ "[Unknown Artist]" ], true
                            | d -> [ d.Name ], true
                        | artists ->
                            let separatedArtists =
                                match artists |> Array.map (fun a -> a.Split ';') with
                                | [||] -> List.empty
                                | arrayArray -> arrayArray |> Array.reduce Array.append |> List.ofArray

                            separatedArtists, false

                    let diInstance = createItem fileInfo tags musicFolderId parentId

                    let album =
                        { id = Guid.NewGuid()
                          albums.name = albumName
                          albums.year = albumYear
                          albums.from_path = albumFromPath }

                    let artists =
                        artistTags
                        |> List.map (fun a ->
                            { id = Guid.NewGuid()
                              name = a
                              image_url = None
                              from_path = artistsFromPath })
                    // Time for relations
                    let albumArtists = [ for artist in artists -> (album, artist) ]

                    let itemArtists = [ for artist in artists -> (diInstance.path, artist) ]

                    let albumGenres = [ for genre in genres -> (album, genre) ]

                    let albumCoverArt = [ for image in images -> (album, image) ]

                    return
                        FileResult
                            { Album = album
                              Images = images
                              Genres = genres
                              Artists = artists
                              Item = diInstance
                              ItemAlbum = (diInstance.path, album)
                              AlbumArtists = albumArtists
                              ItemArtists = itemArtists
                              AlbumCoverArt = albumCoverArt
                              AlbumGenres = albumGenres }
            with
            | :? TagLib.UnsupportedFormatException as _ex ->
                logger.error (Log.setMessage $"File {fileInfo.FullName} is not media type.")
                return NoResult
            | :? TagLib.CorruptFileException as _ex ->
                logger.error (Log.setMessage $"File {fileInfo.FullName} has corrupt/missing headers.")
                return NoResult
            | error ->
                logger.error (Log.setMessage $"{error.Message}")
                return NoResult
        }

let resultsSeparator (acc: cover_art Set * FileResult list) (r: ScanResult) =
    match r with
    | NoResult -> acc
    | ImageResult i -> (Set.add i (fst acc), snd acc)
    | FileResult f ->
        let prev = (snd acc)
        (fst acc, f :: prev)

let scanResultAccumulator (accum: ReducedResult) (r: FileResult) =
    { Albums = accum.Albums.Add r.Album
      Images = r.Images |> List.fold (_.Add) accum.Images
      Genres = r.Genres |> List.fold (_.Add) accum.Genres
      Artists = r.Artists |> List.fold (_.Add) accum.Artists
      Items = r.Item :: accum.Items
      ItemsAlbums = accum.ItemsAlbums.Add r.ItemAlbum
      AlbumsArtists = r.AlbumArtists |> List.fold (_.Add) accum.AlbumsArtists
      ItemsArtists = r.ItemArtists |> List.fold (_.Add) accum.ItemsArtists
      AlbumsCoverArt = r.AlbumCoverArt |> List.fold (_.Add) accum.AlbumsCoverArt
      AlbumsGenres = r.AlbumGenres |> List.fold (_.Add) accum.AlbumsGenres }

// Recursive traverse directories
// Pop current directory from list,
// Scan all files, accumulating results,
// Insert into database,
// Then add found directories onto the list to feed into recursion.
let rec traverseDirectories
    (musicFolderId: Guid)
    (rootDirInfo: DirectoryInfo)
    (dirAndParents: (DirectoryInfo * Guid option) list)
    (lastScannedTime: DateTime option)
    =
    match dirAndParents with
    | [] -> ()
    | (currentDir, parentId) :: rest ->
        let logger = LogProvider.getLoggerByFunc ()
        logger.info (Log.setMessage $"Scanning dir: {currentDir}")

        try
            try
                let currentFiles =
                    match lastScannedTime with
                    | None -> currentDir.GetFiles()
                    | Some time ->
                        currentDir.EnumerateFiles()
                        |> Seq.filter (fun file -> file.CreationTimeUtc > time || file.LastWriteTimeUtc > time)
                        |> Array.ofSeq
                    |> Array.sortBy (_.Name)

                match currentFiles with
                | [||] -> ()
                | files ->
                    logger.debug (Log.setMessage $"Found files in currentDir: {files.Length}")
                    let fileList = List.ofArray files

                    // Make a directory directoryItem
                    let directory = createDirectory currentDir musicFolderId parentId

                    // Insert directory info into DB as directory_item
                    let insertDirectoryTask = insertOrUpdateDirectoryItem [ directory ]

                    let insertedDirectory = insertDirectoryTask.Result |> Seq.head

                    // Scan children files as tasks for possible multi-threading
                    let scanTask =
                        fileList
                        |> List.map (fun f -> scanFile rootDirInfo musicFolderId f (Some insertedDirectory.id))
                        |> Task.WhenAll

                    let scanResult = scanTask.Result |> List.ofArray

                    logger.info (Log.setMessage $"{scanResult.Length} media files scanned in {currentDir.FullName}")

                    // Separate results into their types by folding
                    let imageInfos, fileResults =
                        scanResult |> List.fold resultsSeparator (Set.empty, List<FileResult>.Empty)

                    logger.debug (Log.setMessage $"Images: {imageInfos |> Set.count}, Files: {fileResults.Length}")

                    // Create an accumulator with everything empty, but add in the imageInfos we got from image files
                    let accumulator =
                        { ReducedResult.Default with
                            Images = imageInfos |> Set.fold (_.Add) Set.empty }
                    // Merge them into sets, so we don't insert duplicates
                    let reduced = fileResults |> List.fold scanResultAccumulator accumulator

                    logger.debug (Log.setMessage $"Items: {reduced.Items.Length}")

                    logger.debug (
                        Log.setMessage $"""Items: {reduced.Items |> List.map (_.path) |> String.concat ", "}"""
                    )

                    // First insert albums (which will check existing)
                    let insertAlbumsTask = insertTask (reduced.Albums |> Set.toList)
                    // Insert cover art
                    let insertImagesTask = insertTask (reduced.Images |> Set.toList)
                    // Insert artists, TODO add image_url later
                    let insertArtistsTask = insertTask (reduced.Artists |> Set.toList)
                    // Insert genres
                    let insertGenresTask = insertTask (reduced.Genres |> Set.toList)
                    // Insert directory_items
                    let insertItemsTask = insertOrUpdateDirectoryItem reduced.Items

                    let albums = insertAlbumsTask.Result
                    let images = insertImagesTask.Result
                    let artists = insertArtistsTask.Result
                    let items = insertItemsTask.Result
                    let genres = insertGenresTask.Result

                    logger.debug (Log.setMessage $"albums: %A{albums |> Seq.map (_.name)}")
                    logger.debug (Log.setMessage $"images: %A{images |> Seq.map (_.path)}")
                    logger.debug (Log.setMessage $"artists: %A{artists |> Seq.map (_.name)}")
                    logger.debug (Log.setMessage $"items: %A{items |> Seq.map (_.path)}")
                    logger.debug (Log.setMessage $"genres: %A{genres |> Seq.map (_.name)}")
                    logger.debug (Log.setMessage $"albumsArtists: %A{reduced.AlbumsArtists}")

                    // Time for relations
                    // Create the objects to insert first, then insert
                    let artistsAlbumsToInsert =
                        reduced.AlbumsArtists
                        |> Set.toList
                        |> List.map (fun (alb, art) ->
                            let artist = artists |> Seq.find art.Equals
                            let album = albums |> Seq.find alb.Equals

                            { artist_id = artist.id
                              album_id = album.id })

                    let insertArtistsAlbumsTask = insertJoinTable artistsAlbumsToInsert

                    let itemsArtistsToInsert =
                        reduced.ItemsArtists
                        |> Set.toList
                        |> List.map (fun (itemPath, art) ->
                            let artist = artists |> Seq.find art.Equals
                            let item = items |> Seq.find (fun a -> a.path = itemPath)

                            { artist_id = artist.id
                              item_id = item.id })

                    let insertItemsArtistsTask = insertJoinTable itemsArtistsToInsert

                    let albumsCoverArtToInsert =
                        reduced.AlbumsCoverArt
                        |> Set.toList
                        |> List.map (fun (alb, ca) ->
                            let album = albums |> Seq.find alb.Equals
                            let image = images |> Seq.find ca.Equals

                            { cover_art_id = image.id
                              album_id = album.id })

                    let insertAlbumsCoverArtTask = insertJoinTable albumsCoverArtToInsert

                    let albumsGenresToInsert =
                        reduced.AlbumsGenres
                        |> Set.toList
                        |> List.map (fun (alb, gen) ->
                            let album = albums |> Seq.find alb.Equals
                            let genre = genres |> Seq.find gen.Equals

                            { album_id = album.id
                              genre_id = genre.id })

                    let insertAlbumsGenresTask = insertJoinTable albumsGenresToInsert

                    // let directoryAlbums =
                    //     [ for album in albums ->
                    //           { item_id = insertedDirectory.id
                    //             album_id = album.id } ]

                    let itemsAlbumsToInsert =
                        reduced.ItemsAlbums
                        |> Set.toList
                        |> List.map (fun (itemPath, alb) ->
                            let album = albums |> Seq.find alb.Equals
                            let item = items |> Seq.find (fun a -> a.path = itemPath)

                            { item_id = item.id
                              album_id = album.id })

                    let insertItemsAlbumsTask = insertJoinTable itemsAlbumsToInsert

                    // Make sure all tasks done
                    let albumsArtists = insertArtistsAlbumsTask.Result
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
            // Should notify eventual mailboxprocessor of Seq.length items
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
            traverseDirectories musicFolderId rootDirInfo newRest lastScannedTime

let scanMusicLibrary () =
    task {
        let logger = LogProvider.getLoggerByFunc ()

        use! conn = npgsqlSource.OpenConnectionAsync()

        let! roots =
            select {
                for _root in libraryRootsTable do
                    selectAll
            }
            |> conn.SelectAsync<library_roots>


        for root in roots do
            match root.initial_scan, root.is_scanning, DirectoryInfo(root.path) with
            | Some _, _, _ -> logger.info (Log.setMessage $"Library root {root.path} already scanned initially.")
            | _, true, _ ->
                logger.info (Log.setMessage $"Library root {root.path} is currently being scanned in another thread.")
            | _, _, dirInfo when dirInfo.Exists |> not ->
                logger.warn (Log.setMessage $"Library root {root.name} at {root.path} does not exist.")
            | _, _, dirInfo ->
                logger.info (Log.setMessage $"Scanning library root: {root.path}")

                let! _ =
                    update {
                        for r in libraryRootsTable do
                            set { root with is_scanning = true }
                            includeColumn r.is_scanning
                            where (r.id = root.id)
                    }
                    |> conn.UpdateAsync<library_roots>

                let stopWatch = Stopwatch()
                stopWatch.Start()

                let dirs: (DirectoryInfo * Guid option) list =
                    dirInfo.GetDirectories()
                    |> Array.sortBy (_.Name)
                    |> List.ofArray
                    |> List.map (fun d -> (d, None))

                traverseDirectories root.id dirInfo dirs None

                let updatedRoot =
                    { root with
                        initial_scan = Some DateTime.UtcNow
                        is_scanning = false }

                let! _ =
                    update {
                        for r in libraryRootsTable do
                            set updatedRoot
                            includeColumn r.initial_scan
                            includeColumn r.is_scanning
                            where (r.id = root.id)
                    }
                    |> conn.UpdateAsync<library_roots>

                stopWatch.Stop()
                logger.info (Log.setMessage $"Traversed {dirInfo.FullName} in {stopWatch.ElapsedMilliseconds}ms")
                ()
    }

let scanForUpdates () =
    task {
        use! conn = npgsqlSource.OpenConnectionAsync()
        let logger = LogProvider.getLoggerByFunc ()

        let! roots =
            select {
                for _root in libraryRootsTable do
                    selectAll
            }
            |> conn.SelectAsync<library_roots>


        for root in roots do
            match root.is_scanning, root.initial_scan with
            | true, _ -> logger.warn (Log.setMessage $"Library root {root.path} is currently being scanned already.")
            | false, None ->
                logger.warn (Log.setMessage $"Library root {root.path} has not been initially scanned yet.")
            | _ ->
                let stopWatch = Stopwatch()
                stopWatch.Start()

                let! _ =
                    update {
                        for r in libraryRootsTable do
                            set { root with is_scanning = true }
                            includeColumn r.is_scanning
                            where (r.id = root.id)
                    }
                    |> conn.UpdateAsync<library_roots>

                // Get directories
                let! directories =
                    select {
                        for items in directoryItemsTable do
                            where (items.is_dir = true)
                    }
                    |> conn.SelectAsync<directory_items>
                // Make set of current dirs in db
                let directoryPathSet = directories |> Seq.map (_.path) |> Set.ofSeq

                // Get all dirs in library_root, then subtract it from current dirs
                let dirsToDelete =
                    Directory.EnumerateDirectories(root.path, "*", EnumerationOptions(RecurseSubdirectories = true))
                    |> Seq.map Path.GetFullPath
                    |> Set.ofSeq
                    |> Set.difference directoryPathSet
                    |> Set.toList

                logger.info (Log.setMessage $"To delete: %A{dirsToDelete}")

                // This will also cascade and delete any directory_items that are orphaned
                // As well as any relations.
                let! deletedDirs =
                    match dirsToDelete with
                    | [] -> Task.FromResult Seq.empty
                    | _ ->
                        delete {
                            for di in directoryItemsTable do
                                where (isIn di.path dirsToDelete)
                        }
                        |> conn.DeleteOutputAsync<directory_items>

                for d in deletedDirs do
                    logger.info (Log.setMessage $"Deleted folder and children: {d.path}")

                // Now find items that have been deleted
                // First get all items
                let! files =
                    select {
                        for items in directoryItemsTable do
                            where (items.is_dir = false)
                    }
                    |> conn.SelectAsync<directory_items>

                // Map to path
                let filesPathSet = files |> Seq.map (_.path) |> Set.ofSeq

                // Remove files on disk from db result
                let filesToDelete =
                    Directory.EnumerateFiles(root.path, "*", EnumerationOptions(RecurseSubdirectories = true))
                    |> Seq.map Path.GetFullPath
                    |> Set.ofSeq
                    |> Set.difference filesPathSet
                    |> Set.toList

                logger.info (Log.setMessage $"Files to delete: %A{filesToDelete}")

                let! deletedFiles =
                    match filesToDelete with
                    | [] -> Task.FromResult Seq.empty
                    | _ ->
                        delete {
                            for di in directoryItemsTable do
                                where (isIn di.path filesToDelete)
                        }
                        |> conn.DeleteOutputAsync<directory_items>

                for d in deletedFiles do
                    logger.info (Log.setMessage $"Deleted item: {d.path}")

                let! images =
                    select {
                        for ca in coverArtTable do
                            where (isNotNullValue ca.path)
                    }
                    |> conn.SelectAsync<cover_art>

                let imagePaths = images |> Seq.choose (_.path) |> Set.ofSeq

                // Now find images that don't exist
                let imagesToDelete =
                    seq {
                        Directory.EnumerateFiles(root.path, "*.jpeg", EnumerationOptions(RecurseSubdirectories = true))
                        Directory.EnumerateFiles(root.path, "*.jpg", EnumerationOptions(RecurseSubdirectories = true))
                        Directory.EnumerateFiles(root.path, "*.png", EnumerationOptions(RecurseSubdirectories = true))
                    }
                    |> Seq.concat
                    |> Seq.map Path.GetFullPath
                    |> Set.ofSeq
                    |> Set.difference imagePaths
                    |> Set.toList
                    |> List.map Some

                logger.info (Log.setMessage $"Images to delete: %A{imagesToDelete}")

                let! deletedImages =
                    match imagesToDelete with
                    | [] -> Task.FromResult Seq.empty
                    | _ ->
                        delete {
                            for ca in coverArtTable do
                                where (isIn ca.path imagesToDelete)
                        }
                        |> conn.DeleteOutputAsync<cover_art>

                for i in deletedImages do
                    logger.info (Log.setMessage $"Deleted cover art: {i.path}")

                let dirInfo = DirectoryInfo(root.path)
                // Now rescan for files that have changed
                let dirs: (DirectoryInfo * Guid option) list =
                    dirInfo.GetDirectories()
                    |> Array.sortBy (_.Name)
                    |> List.ofArray
                    |> List.map (fun d -> (d, None))

                traverseDirectories root.id dirInfo dirs None

                let updatedRoot =
                    { root with
                        initial_scan = Some DateTime.UtcNow
                        is_scanning = false }

                let! _ =
                    update {
                        for r in libraryRootsTable do
                            set updatedRoot
                            includeColumn r.initial_scan
                            includeColumn r.is_scanning
                            where (r.id = root.id)
                    }
                    |> conn.UpdateAsync<library_roots>


                stopWatch.Stop()
                logger.info (Log.setMessage $"Updated {root.path} in {stopWatch.ElapsedMilliseconds}ms")
                ()
    }
