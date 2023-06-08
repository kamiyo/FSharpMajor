module DatabaseTypes.Defaults

open DatabaseTypes.``public``

let defaultUser =
    { id = System.Guid.Empty
      username = ""
      password = ""
      scrobbling = None
      admin_role = None
      settings_role = None
      download_role = None
      upload_role = None
      playlist_role = None
      cover_art_role = None
      podcast_role = None
      comment_role = None
      stream_role = None
      jukebox_role = None
      share_role = None
      video_conversion_role = None
      max_bit_rate = None
      avatar_last_changed = None }
