module FSharpMajor.API.Error

open Microsoft.AspNetCore.Http
open Giraffe
open Types

type ErrorEnum =
    | Generic = 0
    | Params = 10
    | ClientVersion = 20
    | ServerVersion = 30
    | Credentials = 40
    | LDAPToken = 41
    | Unauthorized = 50
    | Trial = 60
    | NotFound = 70

let setSubsonicCode (code: ErrorEnum) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        ctx.Items["subsonicCode"] <- code
        next ctx

let subsonicError: HttpHandler =
    fun (_: HttpFunc) (ctx: HttpContext) ->
        let serializer = ctx.GetXmlSerializer()
        let code: int = ctx.Items["subsonicCode"] :?> int

        let msg =
            match code with
            | 70 -> "The requested data was not found."
            | 40 -> "Wrong username or password."
            | 10 -> "Required parameter is missing."
            | _ -> "Unknown error."

        let failedResp =
            SubsonicResponse(
                SubsonicResponseAttributes(status = "failed"),
                children = XmlElements [| Error(ErrorAttributes(code = code, message = msg)) |]
            )
            |> serializer.Serialize

        setBody failedResp earlyReturn ctx
