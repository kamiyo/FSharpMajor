module FSharpMajor.Utils.Mime

open System
open Microsoft.AspNetCore.StaticFiles

let MIMEProvider = FileExtensionContentTypeProvider()
MIMEProvider.Mappings.Add(".flac", "audio/flac")

let getMimeType (file: string) =
    let mutable mimeType = ""

    match MIMEProvider.TryGetContentType(file, &mimeType) with
    | false -> "application/octet-stream"
    | true -> mimeType

let imageMimeTypes = [ "jpeg"; "jpg"; "png" ]

let isImage (mimeType: string) =
    let exists =
        imageMimeTypes
        |> List.exists (fun t -> mimeType.Contains(t, StringComparison.InvariantCultureIgnoreCase))

    match exists with
    | true -> true, mimeType.Replace("taglib", "image")
    | false -> false, mimeType

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
