module FSharpMajor.Initialize

open FSharpMajor.Database
open FSharpMajor.DatabaseTypes
open FSharpMajor.DatabaseTypes.``public``

open FsConfig
open SqlHydra.Query.SelectBuilders
open SqlHydra.Query.InsertBuilders
open DatabaseTypes.Defaults
open dotenv.net
open Encryption

type Config =
    { AdminUser: string
      AdminPassword: string }

DotEnv.Load(DotEnvOptions(envFilePaths = [| ".env" |], probeForEnv = true))

let makeAdminIfNotExists () =
    let config =
        match EnvConfig.Get<Config>() with
        | Ok config -> config
        | Error error ->
            match error with
            | NotFound envVarName -> failwithf "Environment variable %s not found" envVarName
            | BadValue(envVarName, value) -> failwithf "Environment variable %s has invalid value %s" envVarName value
            | NotSupported msg -> failwith msg

    let exists =
        selectTask HydraReader.Read (Create openContext) {
            for u in users do
                where (u.username = config.AdminUser)
                count
        }

    let encryptedPass = config.AdminPassword |> encryptPassword

    let created =
        match exists.Result with
        | 0 ->
            let inserted =
                insertTask (Create openContext) {
                    for u in users do
                        entity
                            { defaultUser with
                                users.username = config.AdminUser
                                users.password = encryptedPass
                                users.admin_role = true }

                        getId u.id
                }

            Some inserted.Result
        | _ -> None

    created
