// This code was generated by `SqlHydra.Npgsql` -- v2.0.0.0.
namespace FSharpMajor.DatabaseTypes

module ``public`` =
    [<CLIMutable>]
    type albums =
        { id: System.Guid
          name: string
          year: Option<int> }

    [<CLIMutable>]
    type albums_cover_art =
        { album_id: System.Guid
          cover_art_id: System.Guid }

    [<CLIMutable>]
    type albums_genres =
        { genre_id: System.Guid
          album_id: System.Guid }

    [<CLIMutable>]
    type albums_users =
        { album_id: System.Guid
          user_id: System.Guid
          starred: Option<System.DateTime>
          last_played: Option<System.DateTime>
          play_count: Option<int64>
          rating: Option<int> }

    [<CLIMutable>]
    type artists =
        { id: System.Guid
          name: string
          image_url: Option<string> }

    [<CLIMutable>]
    type artists_albums =
        { artist_id: System.Guid
          album_id: System.Guid }

    [<CLIMutable>]
    type artists_users =
        { artist_id: System.Guid
          user_id: System.Guid
          starred: Option<System.DateTime>
          last_played: Option<System.DateTime>
          rating: Option<int> }

    [<CLIMutable>]
    type cover_art =
        { id: System.Guid
          mime: string
          image: Option<byte []>
          path: Option<string>
          hash: string
          created: System.DateTime }

    [<CLIMutable>]
    type directory_items =
        { id: System.Guid
          parent_id: Option<System.Guid>
          name: Option<string>
          is_dir: bool
          track: Option<int>
          year: Option<int>
          size: Option<int64>
          content_type: Option<string>
          suffix: Option<string>
          duration: Option<int>
          bit_rate: Option<int>
          path: Option<string>
          is_video: Option<bool>
          disc_number: Option<int>
          created: System.DateTime
          ``type``: Option<string>
          album_from_path: bool
          artist_from_path: bool }

    [<CLIMutable>]
    type genres = { id: System.Guid; name: string }

    [<CLIMutable>]
    type items_albums =
        { item_id: System.Guid
          album_id: System.Guid }

    [<CLIMutable>]
    type items_artists =
        { item_id: System.Guid
          artist_id: System.Guid }

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
          scan_completed: Option<System.DateTime> }

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
