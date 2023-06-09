module FSharpMajor.Encryption

open System
open System.Text
open System.IO
open System.Security.Cryptography

open FSharp.Data

// Fallback if no encryption key is set in env
[<Literal>]
let defaultEncryptionKey = "V0/IwzP39PQhmYh9H/yBXw=="

// Get a literal of the encryption key at compile-time
// Do not change once program is initially built.
[<Literal>]
let encryptionKey =
    LiteralProviders.Env<"ENCRYPTION_KEY", defaultEncryptionKey>.Value

// Prepare MD5, maybe saves overhead?
let md5 = MD5.Create()

// Prepare AES, maybe saves overhead?
let aesAlg = Aes.Create()
aesAlg.Key <- Convert.FromBase64String(encryptionKey)

let decodeMessage (iv: byte array) (b64msg: byte array) =
    aesAlg.IV <- iv
    let decryptor = aesAlg.CreateDecryptor()
    use msDecrypt = new MemoryStream(b64msg)
    use csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)
    use srDecrypt = new StreamReader(csDecrypt)
    srDecrypt.ReadToEnd()

let encodeMessage (msg: string) =
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

let checkHashedPassword (hashed: string) (salt: string) (storedPassDecrypted: string) =
    let toCheck =
        storedPassDecrypted + salt
        |> UTF8Encoding.UTF8.GetBytes
        |> md5.ComputeHash
        |> Convert.ToHexString

    System.String.Equals(hashed, toCheck, StringComparison.CurrentCultureIgnoreCase)
