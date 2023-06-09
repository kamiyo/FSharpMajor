module FSharpMajor.DatabaseTypes.Defaults

open FSharpMajor.DatabaseTypes.``public``

// Default instances of database types
let defaultUser =
    { id = System.Guid.Empty
      username = ""
      password = ""
      scrobbling = true
      admin_role = false
      settings_role = false
      download_role = true
      upload_role = false
      playlist_role = true
      cover_art_role = true
      podcast_role = true
      comment_role = true
      stream_role = true
      jukebox_role = false
      share_role = true
      video_conversion_role = true
      max_bit_rate = None
      avatar_last_changed = None }
