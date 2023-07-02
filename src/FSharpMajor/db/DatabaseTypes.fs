module FSharpMajor.DatabaseTypes

open System
open System.Globalization

open Dapper.FSharp.PostgreSQL


type IQueryField =
    abstract QueryField: string * string

type IJoinTable =
    abstract QueryIdNames: string * string
    abstract QueryIdValues: Guid * Guid

[<CLIMutable; CustomEquality; CustomComparison>]
type albums =
    { id: System.Guid
      name: string
      year: Option<int>
      from_path: bool }

    override __.Equals(obj: obj) =
        match obj with
        | :? albums as other -> __.name.Equals other.name
        | _ -> invalidArg (nameof obj) "Object is not an album"

    override __.GetHashCode() = __.name.GetHashCode()

    interface IQueryField with
        member __.QueryField = nameof __.name, __.name

    interface IComparable with
        member __.CompareTo(obj: obj) =
            match obj with
            | null -> 1
            | :? albums as other -> __.name.CompareTo other.name
            | _ -> invalidArg (nameof obj) "Object is not an artist."

[<CLIMutable>]
type albums_cover_art =
    { album_id: Guid
      cover_art_id: Guid }

    interface IJoinTable with
        member __.QueryIdNames = nameof __.album_id, nameof __.cover_art_id
        member __.QueryIdValues = __.album_id, __.cover_art_id

[<CLIMutable>]
type albums_genres =
    { genre_id: System.Guid
      album_id: System.Guid }

    interface IJoinTable with
        member __.QueryIdNames = nameof __.genre_id, nameof __.album_id
        member __.QueryIdValues = __.genre_id, __.album_id

[<CLIMutable>]
type albums_users =
    { album_id: System.Guid
      user_id: System.Guid
      starred: Option<System.DateTime>
      last_played: Option<System.DateTime>
      play_count: Option<int64>
      rating: Option<int> }

[<CLIMutable; CustomComparison; CustomEquality>]
type artists =
    { id: System.Guid
      name: string
      image_url: Option<string>
      from_path: bool }

    override __.Equals(obj: obj) =
        match obj with
        | :? artists as other -> __.name.Equals other.name
        | _ -> invalidArg (nameof obj) "Object is not an artist."

    override __.GetHashCode() = __.name.GetHashCode()

    interface IQueryField with
        member __.QueryField = nameof __.name, __.name

    interface IComparable with
        member __.CompareTo(obj: obj) =
            match obj with
            | null -> 1
            | :? artists as other -> __.name.CompareTo other.name
            | _ -> invalidArg (nameof obj) "Object is not an artist."

[<CLIMutable>]
type artists_albums =
    { artist_id: System.Guid
      album_id: System.Guid }

    interface IJoinTable with
        member __.QueryIdNames = nameof __.artist_id, nameof __.album_id
        member __.QueryIdValues = __.artist_id, __.album_id

[<CLIMutable>]
type artists_users =
    { artist_id: System.Guid
      user_id: System.Guid
      starred: Option<System.DateTime>
      last_played: Option<System.DateTime>
      rating: Option<int> }

[<CLIMutable; CustomComparison; CustomEquality>]
type cover_art =
    { id: System.Guid
      mime: string
      image: Option<byte[]>
      path: Option<string>
      hash: string
      created: System.DateTime }

    override __.Equals(obj: obj) =
        match obj with
        | :? cover_art as other -> __.hash.Equals other.hash
        | _ -> invalidArg (nameof obj) "Object is not an cover_art"

    override __.GetHashCode() =
        Int32.Parse(__.hash, NumberStyles.HexNumber)

    interface IQueryField with
        member __.QueryField = nameof __.hash, __.hash

    interface IComparable with
        member __.CompareTo(obj: obj) =
            match obj with
            | null -> 1
            | :? cover_art as other -> __.hash.CompareTo other.hash
            | _ -> invalidArg (nameof obj) "Object is not an cover_art"

[<CLIMutable; CustomEquality; NoComparison>]
type directory_items =
    { id: System.Guid
      parent_id: Option<System.Guid>
      music_folder_id: System.Guid
      name: Option<string>
      is_dir: bool
      track: Option<int>
      year: Option<int>
      size: Option<int64>
      content_type: Option<string>
      suffix: Option<string>
      duration: Option<int>
      bit_rate: Option<int>
      path: string
      is_video: Option<bool>
      disc_number: Option<int>
      created: System.DateTime
      ``type``: Option<string> }

    override __.Equals(obj: obj) =
        match obj with
        | :? directory_items as other ->
            __.name = other.name
            && __.track = other.track
            && __.year = other.year
            && __.size = other.size
            && __.content_type = other.content_type
            && __.duration = other.duration
            && __.bit_rate = other.bit_rate
            && __.disc_number = other.disc_number
            && __.``type`` = other.``type``
        | _ -> invalidArg (nameof obj) "Object is not directory_items"

    override __.GetHashCode() = __.id.GetHashCode()

[<CLIMutable; CustomEquality; CustomComparison>]
type genres =
    { id: System.Guid
      name: string }

    override __.Equals(obj: obj) =
        match obj with
        | :? genres as other -> __.name.Equals other.name
        | _ -> invalidArg (nameof obj) "Object is not an genre"

    override __.GetHashCode() = __.name.GetHashCode()

    interface IQueryField with
        member __.QueryField = nameof __.name, __.name

    interface IComparable with
        member __.CompareTo(obj: obj) =
            match obj with
            | null -> 1
            | :? genres as other -> __.name.CompareTo other.name
            | _ -> invalidArg (nameof obj) "Object is not an genre."

[<CLIMutable>]
type items_albums =
    { item_id: System.Guid
      album_id: System.Guid }

    interface IJoinTable with
        member __.QueryIdNames = nameof __.item_id, nameof __.album_id
        member __.QueryIdValues = __.item_id, __.album_id

[<CLIMutable>]
type items_artists =
    { item_id: System.Guid
      artist_id: System.Guid }

    interface IJoinTable with
        member __.QueryIdNames = nameof __.item_id, nameof __.artist_id
        member __.QueryIdValues = __.item_id, __.artist_id

[<CLIMutable>]
type items_users =
    { item_id: System.Guid
      user_id: System.Guid
      starred: Option<System.DateTime>
      last_played: Option<System.DateTime>
      bookmark_pos: Option<int64>
      play_count: Option<int64>
      rating: Option<int> }

[<CLIMutable>]
type library_roots =
    { id: System.Guid
      name: string
      path: string
      initial_scan: Option<DateTime>
      is_scanning: bool }

[<CLIMutable>]
type schema_migrations = { version: string }

[<CLIMutable>]
type users =
    { id: System.Guid
      username: string
      password: string
      scrobbling: bool
      admin_role: bool
      settings_role: bool
      download_role: bool
      upload_role: bool
      playlist_role: bool
      cover_art_role: bool
      podcast_role: bool
      comment_role: bool
      stream_role: bool
      jukebox_role: bool
      share_role: bool
      video_conversion_role: bool
      max_bit_rate: Option<int>
      avatar_last_changed: Option<System.DateTime> }


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
