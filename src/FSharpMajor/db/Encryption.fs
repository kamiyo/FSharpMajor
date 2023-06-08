module Encryption

open FSharp.Data
open System
open System.Text
open System.IO
open System.Security.Cryptography
open System.Security.Cryptography
open System.Text

[<Literal>]
let defaultEncryptionKey = "V0/IwzP39PQhmYh9H/yBXw=="

[<Literal>]
let encryptionKey =
    LiteralProviders.Env<"ENCRYPTION_KEY", defaultEncryptionKey>.Value

// Prepare MD5
let md5 = MD5.Create()

let decodeMessage (iv: byte array) (b64msg: byte array) =
    use aesAlg = Aes.Create()
    aesAlg.Key <- Convert.FromBase64String(encryptionKey)
    aesAlg.IV <- iv
    let decryptor = aesAlg.CreateDecryptor()
    use msDecrypt = new MemoryStream(b64msg)
    use csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)
    use srDecrypt = new StreamReader(csDecrypt)
    srDecrypt.ReadToEnd()

let encodeMessage (msg: string) =
    use aesAlg = Aes.Create()
    aesAlg.Key <- Convert.FromBase64String(encryptionKey)
    aesAlg.GenerateIV()
    let encryptor = aesAlg.CreateEncryptor()
    use msEncrypt = new MemoryStream()

    do
        use csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)
        use swEncrypt = new StreamWriter(csEncrypt)

        //Write all data to the stream.
        swEncrypt.Write(msg)

    // Return the encrypted bytes from the memory stream.
    Array.append aesAlg.IV (msEncrypt.ToArray())

let encryptPassword (password: string) =
    let result = password |> encodeMessage
    Convert.ToBase64String result

let decryptPassword (encrypted: string) =
    let (iv, cipher) = encrypted |> Convert.FromBase64String |> Array.splitAt 16
    cipher |> decodeMessage iv

// storedPass should be decrypted
let checkHashedPassword (hashed: string, salt: string, storedPass: string) =
    let toCheck =
        storedPass + salt
        |> UTF8Encoding.UTF8.GetBytes
        |> md5.ComputeHash
        |> Convert.ToHexString

    System.String.Equals(hashed, toCheck, StringComparison.CurrentCultureIgnoreCase)
