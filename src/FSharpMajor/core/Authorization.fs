module FSharpMajor.Authorization

open System.Security.Claims

open System.Security.Principal
open Microsoft.AspNetCore.Authentication
open Microsoft.Extensions.Primitives

open Dapper.FSharp.PostgreSQL

open FSharpMajor.DatabaseService

open FSharpMajor.DatabaseTypes
open FSharpMajor.Encryption
open FSharpMajor.API.Error



type Queries =
    { U: StringValues option
      T: StringValues option
      S: StringValues option }


let getRoles user =
    let map =
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

    seq {
        for k, v in map do
            if v then
                yield k
    }

type IAuthenticationManager =
    abstract member Authenticate: username: string -> token: string -> salt: string -> Claim seq option

type SubsonicAuthenticationManager(dbContext: IDatabaseService) =
    interface IAuthenticationManager with
        member _.Authenticate username token salt =
            use conn = dbContext.OpenConnection()

            let userResults =
                select {
                    for u in usersTable do
                        where (u.username = username)
                }
                |> conn.SelectAsync<users>

            userResults.Result
            |> Seq.tryHead
            |> Option.map (fun user ->
                let decrypted = decryptPassword user.password

                match checkHashedPassword token salt decrypted with
                | false -> None
                | true ->
                    let roles = seq { for r in getRoles user -> Claim(ClaimTypes.Role, r) }

                    let claims =
                        seq {
                            yield! roles
                            yield Claim(ClaimTypes.Name, user.username)
                        }

                    Some(claims))
            |> Option.flatten

type BasicAuthenticationOptions() =
    class
        inherit AuthenticationSchemeOptions()
    end

type BasicAuthHandler(options, logger, encoder, clock, authManager: IAuthenticationManager, dbContext: IDatabaseService)
    =
    inherit AuthenticationHandler<BasicAuthenticationOptions>(options, logger, encoder, clock)
    member _.authManager = authManager

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
                let identity = ClaimsIdentity(claims, __.Scheme.Name)

                let roles =
                    seq {
                        for c in claims do
                            if c.Type = ClaimTypes.Role then
                                c.Value
                    }
                    |> Array.ofSeq

                let principal = GenericPrincipal(identity, roles)
                let ticket = AuthenticationTicket(principal, __.Scheme.Name)
                task { return AuthenticateResult.Success(ticket) }
            | None ->
                __.Context.Items["subsonicCode"] <- ErrorEnum.Credentials
                task { return AuthenticateResult.Fail("Unauthorized") }
        | false ->
            __.Context.Items["subsonicCode"] <- ErrorEnum.Params
            task { return AuthenticateResult.Fail("Unauthorized") }
