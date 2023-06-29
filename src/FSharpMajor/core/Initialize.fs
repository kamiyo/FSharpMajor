module FSharpMajor.Initialize

open System.IO

open FSharpMajor.Database
open FSharpMajor.DatabaseTypes
open FSharpMajor.DatabaseTypesDefaults

open Dapper.FSharp.PostgreSQL
open FsConfig
open dotenv.net
open Encryption

type Config =
    { AdminUser: string
      AdminPassword: string }

DotEnv.Load(DotEnvOptions(envFilePaths = [| ".env" |], probeForEnv = true))

let makeOrUpdateAdmin () =
    let config =
        match EnvConfig.Get<Config>() with
        | Ok config -> config
        | Error error ->
            match error with
            | NotFound envVarName -> failwithf $"Environment variable {envVarName} not found"
            | BadValue(envVarName, value) -> failwithf $"Environment variable {envVarName} has invalid value {value}"
            | NotSupported msg -> failwith msg

    use conn = npgsqlSource.OpenConnection()

    let usersTable = table<users>

    let exists =
        select {
            for u in usersTable do
                where (u.username = config.AdminUser)
        }
        |> conn.SelectAsync<users>


    match exists.Result |> Seq.tryHead with
    | None ->
        let encryptedPass = config.AdminPassword |> encryptPassword

        insert {
            for u in usersTable do

                value
                    { defaultUser with
                        username = config.AdminUser
                        password = encryptedPass
                        admin_role = true }

                excludeColumn u.id
        }
        |> conn.InsertAsync
        |> ignore
    | Some user ->
        let storedPass = user.password |> decryptPassword

        if storedPass <> config.AdminPassword then
            let newPass = config.AdminPassword |> encryptPassword
            // Update with new password
            update {
                for u in usersTable do
                    set { user with password = newPass }
                    where (u.id = user.id)
            }
            |> conn.UpdateAsync
            |> ignore

type LibraryRoot = { LibraryRoots: string }

let makeLibraryRoots () =
    let config =
        match EnvConfig.Get<LibraryRoot>() with
        | Ok config -> config
        | Error error ->
            match error with
            | NotFound envVarName -> failwithf "Environment variable %s not found" envVarName
            | BadValue(envVarName, value) -> failwithf "Environment variable %s has invalid value %s" envVarName value
            | NotSupported msg -> failwith msg

    let roots = config.LibraryRoots.Split ',' |> Array.map (fun s -> s.Trim())

    use conn = npgsqlSource.OpenConnection()
    // For now, just insert the library roots without updating if changed or deleted
    let existsTask =
        select {
            for _roots in libraryRootsTable do
                selectAll
        }
        |> conn.SelectAsync<library_roots>

    let exists = existsTask.Result |> Seq.map (fun r -> r.path) |> Set.ofSeq

    let needed =
        roots
        |> Set.ofArray
        |> (fun s -> Set.difference s exists)
        |> Set.toList
        |> List.map (fun s ->
            { id = System.Guid.Empty
              name = s |> Path.GetDirectoryName |> Path.GetFileName
              path = s
              scan_completed = None
              is_scanning = false })

    match needed with
    | [] -> 0
    | _ ->
        let insertTask =
            insert {
                for r in libraryRootsTable do
                    values needed
                    excludeColumn r.id
            }
            |> conn.InsertAsync<library_roots>

        insertTask.Result
