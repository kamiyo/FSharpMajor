module API.User

open Giraffe
open Microsoft.AspNetCore.Http

open API.Types

let setXmlType : HttpHandler =
    setHttpHeader "Content-Type" "application/xml"