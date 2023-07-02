module FSharpMajor.Utils.Environment

open dotenv.net
open FsConfig
open Microsoft.Extensions.Configuration


DotEnv.Load(DotEnvOptions(envFilePaths = [| ".env" |]))

let config = ConfigurationBuilder().AddEnvironmentVariables().Build()
let appConfig = AppConfig(config)

type BuildType = { AspnetcoreEnvironment: string }

let buildType =
    match EnvConfig.Get<BuildType>() with
    | Ok config -> config
    | Error _ -> { AspnetcoreEnvironment = "Production" }

let aspnetcoreEnvironment = buildType.AspnetcoreEnvironment
