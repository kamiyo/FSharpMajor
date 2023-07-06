module FSharpMajor.API.Indexes

open System
open System.Text.RegularExpressions
open Giraffe
open Microsoft.AspNetCore.Http
open FSharpMajor.DatabaseService
open FSharpMajor.DatabaseTypes
open FSharpMajor.API.Types
open Dapper

let ignoredArticles =
    @"the a an el la los las le les un una une unos des unas die der das ein eine il i lo gli"

let ignoredArticlesRegexed =
    "^(?:(?:" + ignoredArticles.Replace(" ", "|") + ") |l')?(.*)$"

let articleRegex =
    Regex(
        ignoredArticlesRegexed,
        RegexOptions.Compiled
        ||| RegexOptions.IgnoreCase
        ||| RegexOptions.CultureInvariant
    )

let stringComparer (left: string) (right: string) =
    let leftMatch, rightMatch = articleRegex.Match left, articleRegex.Match right
    let leftStr, rightStr = leftMatch.Groups[1].Value, rightMatch.Groups[1].Value
    leftStr.CompareTo rightStr

let getFirstLetter (s: string) =
    let m = articleRegex.Match s
    m.Groups[1].Value.[0].ToString()

let stringOfMillisecondsToDateTime (s: string) =
    (DateTimeOffset.FromUnixTimeMilliseconds(Int64.Parse s)).UtcDateTime

let indexesHandler: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        use conn = ctx.GetDatabaseQueryContext().OpenConnection()
        let username = ctx.User.Identity.Name

        let artistsResult =
            match ctx.TryGetQueryStringValue "musicFolderId", ctx.TryGetQueryStringValue "ifModifiedSince" with
            | None, None ->
                let query =
                    $"WITH avg_rating_for_user AS \
                        (SELECT artist_id, AVG(rating) AS avg_rating \
                             FROM artists_users \
                             GROUP BY artist_id) \
                        SELECT a.*, au.starred, au.last_played, au.rating, ar.avg_rating \
                        FROM artists AS a \
                        LEFT JOIN artists_users AS au ON (au.artist_id = a.id) \
                        LEFT JOIN avg_rating_for_user AS ar ON (ar.artist_id = a.id) \
                        LEFT JOIN users u ON (au.user_id = u.id) \
                        WHERE u.username = @Username OR u.username IS NULL"

                conn.Query<artist_with_extensions>(query, struct {| Username = username |})
            | None, Some lastModified ->
                let query =
                    $"WITH avg_rating_for_user AS \
                        (SELECT artist_id, AVG(rating) AS avg_rating \
                             FROM artists_users \
                             GROUP BY artist_id) \
                        SELECT a.*, au.starred, au.last_played, au.rating, ar.avg_rating \
                        FROM artists AS a \
                        LEFT JOIN artists_users AS au ON (au.artist_id = a.id) \
                        LEFT JOIN avg_rating_for_user AS ar ON (ar.artist_id = a.id) \
                        LEFT JOIN users u ON (au.user_id = u.id) \
                        INNER JOIN items_artists AS ia ON (a.id = ia.artist_id) \
                        INNER JOIN directory_items AS di ON (ia.item_id = di.id) \
                        WHERE (u.username = @Username OR u.username IS NULL) \
                            AND di.created > @LastModified"

                conn.Query<artist_with_extensions>(
                    query,
                    struct {| Username = username
                              LastModified = stringOfMillisecondsToDateTime lastModified |}
                )
            | Some folderId, None ->
                let query =
                    $"WITH avg_rating_for_user AS \
                        (SELECT artist_id, AVG(rating) AS avg_rating \
                             FROM artists_users \
                             GROUP BY artist_id) \
                        SELECT a.*, au.starred, au.last_played, au.rating, ar.avg_rating \
                        FROM artists AS a \
                        LEFT JOIN artists_users AS au ON (au.artist_id = a.id) \
                        LEFT JOIN avg_rating_for_user AS ar ON (ar.artist_id = a.id) \
                        LEFT JOIN users u ON (au.user_id = u.id) \
                        INNER JOIN items_artists AS ia ON (a.id = ia.artist_id) \
                        INNER JOIN directory_items AS di ON (ia.item_id = di.id) \
                        WHERE (u.username = @Username OR u.username IS NULL) \
                            AND di.music_folder_id = @MusicFolderId"

                conn.Query<artist_with_extensions>(
                    query,
                    struct {| Username = username
                              MusicFolderId = Guid.Parse folderId |}
                )
            | Some folderId, Some lastModified ->
                let query =
                    $"WITH avg_rating_for_user AS \
                        (SELECT artist_id, AVG(rating) AS avg_rating \
                             FROM artists_users \
                             GROUP BY artist_id) \
                        SELECT a.*, au.starred, au.last_played, au.rating, ar.avg_rating \
                        FROM artists AS a \
                        LEFT JOIN artists_users AS au ON (au.artist_id = a.id) \
                        LEFT JOIN avg_rating_for_user AS ar ON (ar.artist_id = a.id) \
                        LEFT JOIN users u ON (au.user_id = u.id) \
                        INNER JOIN items_artists AS ia ON (a.id = ia.artist_id) \
                        INNER JOIN directory_items AS di ON (ia.item_id = di.id) \
                        WHERE (u.username = @Username OR u.username IS NULL) \
                            AND di.created > @LastModified \
                            AND di.music_folder_id = @MusicFolderId"

                conn.Query<artist_with_extensions>(
                    query,
                    struct {| Username = username
                              MusicFolderId = Guid.Parse folderId
                              LastModified = stringOfMillisecondsToDateTime lastModified |}
                )

        let sortedArtists =
            artistsResult
            |> Array.ofSeq
            |> Array.sortWith (fun l r -> stringComparer l.name r.name)
            |> Array.groupBy (fun a -> getFirstLetter a.name)

        let serializer = ctx.GetXmlSerializer()

        let indexes =
            sortedArtists
            |> Array.map (fun (k, a) ->
                let artistArray =
                    a
                    |> Array.map (fun aa ->
                        Artist(
                            ArtistAttributes(
                                aa.id.ToString(),
                                aa.name,
                                aa.image_url,
                                aa.starred |> Option.map (fun dt -> dt.ToIsoString()),
                                aa.user_rating,
                                aa.average_rating
                            )
                        ))

                Index(IndexAttributes(k), artistArray))

        let body =
            SubsonicResponse(children = XmlElements [| Indexes(IndexesAttributes(ignoredArticles) = libraryFolders) |])
            |> serializer.Serialize

        setBody body next ctx
