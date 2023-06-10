module FSharpMajor.TypeMappers

open FSharpMajor.DatabaseTypes.``public``
open FSharpMajor.API.Types

let mapUserToAttributes (user: users) =
    UserAttributes(
        user.username,
        None,
        user.scrobbling,
        user.max_bit_rate,
        user.admin_role,
        user.settings_role,
        user.download_role,
        user.upload_role,
        user.playlist_role,
        user.cover_art_role,
        user.comment_role,
        user.podcast_role,
        user.stream_role,
        user.jukebox_role,
        user.share_role,
        user.video_conversion_role,
        user.avatar_last_changed
    )
