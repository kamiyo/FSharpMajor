module FSharpMajor.Authorization

open System.Security.Claims
open System.Security.Principal
open System.Threading.Tasks

open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives

open Giraffe

open SqlHydra.Query.SelectBuilders

open FSharpMajor.API.Types
open FSharpMajor.Database
open FSharpMajor.DatabaseTypes
open FSharpMajor.DatabaseTypes.``public``
open FSharpMajor.Encryption

type Queries =
    { U: StringValues option
      T: StringValues option
      S: StringValues option }


let getRoles user =
    [ ("Admin", user.admin_role)
      ("Settings", user.settings_role)
      ("Download", user.download_role)
      ("Upload", user.upload_role)
      ("Playlist", user.playlist_role)
      ("CoverArt", user.cover_art_role)
      ("Podcast", user.podcast_role)
      ("Comment", user.comment_role)
      ("Stream", user.stream_role)
      ("Jukebox", user.jukebox_role)
      ("Share", user.share_role)
      ("VideoConversion", user.video_conversion_role) ]
    |> Map.ofList
    |> Map.filter (fun k v -> v)
    |> Map.keys

type IAuthenticationManager =
    abstract member Authenticate: username: string -> token: string -> salt: string -> Claim list option

type SubsonicAuthenticationManager() =
    interface IAuthenticationManager with
        member __.Authenticate username token salt =
            let userResults =
                selectTask HydraReader.Read (Create openContext) {
                    for u in users do
                        where (u.username = username)
                        select u
                }

            userResults.Result
            |> Seq.tryHead
            |> Option.map (fun user ->
                let decrypted = decryptPassword user.password

                match checkHashedPassword token salt decrypted with
                | false -> None
                | true ->
                    let roles = [ for r in getRoles user -> new Claim(ClaimTypes.Role, r) ]
                    let claims = new Claim(ClaimTypes.Name, user.username) :: roles
                    Some(claims))
            |> Option.flatten

type BasicAuthenticationOptions() =
    class
        inherit AuthenticationSchemeOptions()
    end

type BasicAuthHandler(options, logger, encoder, clock, authManager: IAuthenticationManager) =
    inherit AuthenticationHandler<BasicAuthenticationOptions>(options, logger, encoder, clock)
    member __.authManager = authManager

    override __.HandleAuthenticateAsync() =
        let request = __.Request
        let query = request.Query
        let mutable username = StringValues()
        let mutable token = StringValues()
        let mutable salt = StringValues()

        for q in query do
            printfn $"{q.Key}: {q.Value}"

        match
            query.TryGetValue("u", &username)
            && query.TryGetValue("t", &token)
            && query.TryGetValue("s", &salt)
        with
        | true ->
            match __.authManager.Authenticate username[0] token[0] salt[0] with
            | Some claims ->
                let identity = new ClaimsIdentity(claims, __.Scheme.Name)

                let roles =
                    claims |> List.tail |> List.map (fun claim -> claim.Value) |> Array.ofList

                let principal = new GenericPrincipal(identity, roles)
                let ticket = new AuthenticationTicket(principal, __.Scheme.Name)
                task { return AuthenticateResult.Success(ticket) }
            | None ->
                __.Context.Items["subsonicCode"] <- 40
                task { return AuthenticateResult.Fail("Unauthorized") }
        | false ->
            __.Context.Items["subsonicCode"] <- 10
            task { return AuthenticateResult.Fail("Unauthorized") }

let subsonicError: HttpHandler =
    fun (_: HttpFunc) (ctx: HttpContext) ->
        let serializer = ctx.GetXmlSerializer()
        let code: int = ctx.Items["subsonicCode"] :?> int

        let msg =
            match code with
            | 40 -> "Wrong username or password."
            | 10 -> "Required parameter is missing."
            | _ -> "Unknown error"

        let failedResp =
            SubsonicResponse(
                SubsonicResponseAttributes(status = "failed"),
                children = XmlElements [| Error(ErrorAttributes(code = code, message = msg)) |]
            )
            |> serializer.Serialize

        setBody failedResp (Some >> Task.FromResult) ctx
