module FSharpMajor.Tests.Encryption

open System
open Xunit
open FSharpMajor.Encryption
open FsUnitTyped.TopLevelOperators
open Xunit.Abstractions
open System.Security.Cryptography
open System.Text

type EncryptionTest(output: ITestOutputHelper) =

    [<Fact>]
    member _.``Encryption and decryption are symmetric``() =
        let message = "This is a test"

        let decrypted = message |> encryptPassword |> decryptPassword

        decrypted |> shouldEqual message

    [<Fact>]
    member _.``Check md5 hashing with salt``() =
        let storedPass = "2340532hfdjsj"
        let salt = "24gjk45jl4kgj2j5kl4kg5j54"
        let md5 = MD5.Create()

        let hashed =
            storedPass + salt
            |> UTF8Encoding.UTF8.GetBytes
            |> md5.ComputeHash
            |> Convert.ToHexString

        checkHashedPassword hashed salt storedPass |> shouldEqual true

    member private _.output: ITestOutputHelper = output
