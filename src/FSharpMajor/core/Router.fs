module FSharpMajor.Router

open Giraffe

open FSharpMajor.API.Scanning
open FSharpMajor.API.Error
open FSharpMajor.API.System
open FSharpMajor.API.User
open FSharpMajor.API.Users
open FSharpMajor.API.MusicFolders

let apiRouter: HttpHandler =
    choose
        [ route "/ping.view" >=> pingHandler
          route "/getLicense.view" >=> licenseHandler
          route "/getUser.view" >=> userHandler
          route "/getMusicFolders.view" >=> musicFoldersHandler
          route "/getUsers.view"
          >=> requiresRole "Admin" (setSubsonicCode ErrorEnum.Unauthorized >=> subsonicError)
          >=> usersHandler ]

let rootRouter: HttpHandler =
    choose
        [ GET >=> route "/update" >=> updateScanHandler
          GET
          >=> setXmlType
          >=> requiresAuthentication subsonicError
          >=> choose [ subRoute "/rest" apiRouter ]
          subsonicError ]
