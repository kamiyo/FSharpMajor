module FSharpMajor.Scanner

open System
open System.IO
open FSharpMajor.FsLibLog

// let insertDirectories

let TraverseDirectories root =
    let rootDirInfo: DirectoryInfo = DirectoryInfo root
    let logger = LogProvider.getLoggerByFunc ()

    try
        if rootDirInfo.Exists then
            logger.info (Log.setMessage $"Directory {root} exists!")
            let allDirs = rootDirInfo.EnumerateDirectories()
            let mutable count = 0

            for dir in allDirs do
                count <- count + 1

    with e ->
        printfn $"Failed with error {e}"
