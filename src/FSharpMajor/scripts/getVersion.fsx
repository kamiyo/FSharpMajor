open System.Xml
open System.IO

let doc = new XmlDocument()
doc.Load("./FSharpMajor.fsproj")
let version = doc.GetElementsByTagName("Version")
printfn $"{version[0].InnerXml}"
