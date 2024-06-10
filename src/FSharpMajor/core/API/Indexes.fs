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

let ignoredArticlesOr'd = ignoredArticles.Replace(" ", "|")

let ignoredArticlesRegexed =
    "^(?:(?:" + ignoredArticlesOr'd + ") |l')?(.*)$"
    
let cjk =
    @"\p{IsHangulJamo}|"+
    @"\p{IsCJKRadicalsSupplement}|"+
    @"\p{IsCJKSymbolsandPunctuation}|"+
    @"\p{IsEnclosedCJKLettersandMonths}|"+
    @"\p{IsCJKCompatibility}|"+
    @"\p{IsCJKUnifiedIdeographsExtensionA}|"+
    @"\p{IsCJKUnifiedIdeographs}|"+
    @"\p{IsHangulSyllables}|"+
    @"\p{IsCJKCompatibilityForms}"
    
let cjkRegex = Regex(cjk)
let numRegex = Regex(@"[0-9!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]")

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
    m.Groups[1].Value[0].ToString()

let stringOfMillisecondsToDateTime (s: string) =
    DateTimeOffset.FromUnixTimeMilliseconds(Int64.Parse s).UtcDateTime
    
let baseQuery =
    $"WITH
        avg_rating_for_user AS (SELECT artist_id, AVG(rating) AS average_rating \
                                FROM artists_users \
                                GROUP BY artist_id), \
        album_counts AS (SELECT artist_id, COUNT(artist_id) AS album_count \
                         FROM artists_albums \
                         GROUP BY artist_id) \
        SELECT DISTINCT \
            a.*, \
            au.starred, au.last_played, au.rating as user_rating, \
            ar.average_rating, \
            ac.album_count as album_count \
        FROM artists AS a \
        LEFT JOIN artists_users AS au ON (au.artist_id = a.id) \
        LEFT JOIN avg_rating_for_user AS ar ON (ar.artist_id = a.id) \
        LEFT JOIN users AS u ON (au.user_id = u.id) \
        LEFT JOIN album_counts AS ac ON (ac.artist_id = a.id) \
        INNER JOIN items_artists AS ia ON a.id = ia.artist_id \
        INNER JOIN directory_items AS di ON ia.item_id = di.id \
        WHERE (u.username = @Username OR u.username IS NULL) \
        "

let indexesHandler: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        use conn = ctx.GetDatabaseQueryContext().OpenConnection()
        let username = ctx.User.Identity.Name

        let modifiedSince =
            match ctx.TryGetQueryStringValue "ifModifiedSince" with
            | Some(m) -> m |> int64
            | None -> 0L

        let artistsResult =
            match ctx.TryGetQueryStringValue "musicFolderId", ctx.TryGetQueryStringValue "ifModifiedSince" with
            | None, None ->
                let query = baseQuery
                conn.Query<artist_with_extensions>(query, struct {| Username = username |})
                
            | None, Some lastModified ->
                let query = baseQuery + $"AND di.created > @LastModified"
                conn.Query<artist_with_extensions>(
                    query,
                    struct {| Username = username
                              LastModified = stringOfMillisecondsToDateTime lastModified |}
                )
            | Some folderId, None ->
                let query = baseQuery + $"AND di.music_folder_id = @MusicFolderId"

                conn.Query<artist_with_extensions>(
                    query,
                    struct {| Username = username
                              MusicFolderId = Guid.Parse folderId |}
                )
                
            | Some folderId, Some lastModified ->
                let query = baseQuery
                            + $"AND di.created > @LastModified \
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
            |> Array.groupBy (fun a ->
                match getFirstLetter a.name with
                | c when numRegex.IsMatch(c) -> "#"
                | c when cjkRegex.IsMatch(c) -> "中日韓"
                | c -> c)

        let serializer = ctx.GetXmlSerializer()

        let indexes =
            sortedArtists
            |> Array.map (fun (k, a) ->
                let artistArray =
                    a
                    |> Array.map (fun aa ->
                        Artist(
                            ArtistAttributes(
                                id = aa.id.ToString(),
                                name = aa.name,
                                artistImageUrl = aa.image_url,
                                starred = (aa.starred |> Option.map (_.ToIsoString())),
                                userRating = aa.user_rating,
                                averageRating = (aa.average_rating |> Option.map float),
                                albumCount = (aa.album_count |> Option.map int)
                            )
                        ))

                Index(IndexAttributes(k), artistArray))

        let body =
            SubsonicResponse(
                children =
                    XmlElements [|
                        Indexes(IndexesAttributes(modifiedSince.ToString(), ignoredArticles), indexes)
                    |]
            )
            |> serializer.Serialize

        setBody body next ctx
