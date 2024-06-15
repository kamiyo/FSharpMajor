module FSharpMajor.Utils.Counter

open System
open System.IO
open FSharpMajor.FsLibLog
open System.Timers

let mutable filesScanned = 0
let mutable directoriesScanned = 0
let mutable lastFilesScanned = 0
let mutable lastDirectoriesScanned = 0

type Message =
    | Reset
    | IncrementFileCount of int
    | IncrementDirectoryCount of int

let mutable counter =
    MailboxProcessor<Message>.Start(fun inbox ->
        let rec loop () =
            async {
                let! msg = inbox.Receive()

                match msg with
                | Reset ->
                    filesScanned <- 0
                    directoriesScanned <- 0
                | IncrementFileCount n -> filesScanned <- filesScanned + n
                | IncrementDirectoryCount n -> directoriesScanned <- directoriesScanned + n

                return! loop ()
            }

        loop ())

let timerHandler _source (_e: ElapsedEventArgs) =
    let logger = LogProvider.getLoggerByFunc ()

    if lastDirectoriesScanned <> directoriesScanned || lastFilesScanned <> filesScanned then
        logger.info (Log.setMessage $"Directories scanned: {directoriesScanned} | Files scanned: {filesScanned}")
        lastDirectoriesScanned <- directoriesScanned
        lastFilesScanned <- filesScanned

let logTimer = new Timer(1000)
logTimer.Elapsed.AddHandler timerHandler
logTimer.AutoReset <- true

let startScanLogger () =
    counter.Post(Reset)
    logTimer.Enabled <- true

let stopScanLogger () = logTimer.Enabled <- false
