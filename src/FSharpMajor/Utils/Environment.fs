module FSharpMajor.Utils.Environment

open dotenv.net
open FsConfig
open Microsoft.Extensions.Configuration
open dotenv.net


DotEnv.Load(DotEnvOptions(envFilePaths = [| ".env" |]))

let config = ConfigurationBuilder().AddEnvironmentVariables().Build()
let appConfig = AppConfig(config)
