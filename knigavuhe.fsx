#r "nuget: FSharp.Data"

open System
open System.IO
open System.Text.RegularExpressions
open FSharp.Data

let sourceDirectory = __SOURCE_DIRECTORY__

let pattern =
    @"var\s+player\s*=\s*new\s+BookPlayer\s*\(\s*\d+\s*,\s*(?<jsonArray>\[\s*\{.*?\}\s*(?:,\s*\{.*?\}\s*)*\])\s*(?:,\s*[^)]+)*\s*\)\s*;"

type FilesProvider =
    JsonProvider<
        """ [{"id":828379,"title":"_01","url":"https:\/\/s1.knigavuhe.org\/1\/audio\/14642\/-01.mp3","player_data":{"title":"Библия. Ветхий Завет","cover":"https:\/\/s5.knigavuhe.org\/1\/covers\/14642\/1-3.jpg?2","cover_type":"image\/jpeg","authors":null,"readers":"Дмитрий Оргин","series":""},"error":0,"duration":300,"duration_float":300.38}]""",
        SampleIsList=true
     >

type FileDescription(id: int, title: string, url: string) =
    member this.Id = id
    member this.Title = title
    member this.Url = url


let downloadFileAsync (file: FileDescription) (outputFolder: string) =
    async {
        try
            let! response = Http.AsyncRequestStream(file.Url, httpMethod = "GET")

            match response.StatusCode with
            | 200 ->
                let uri = Uri response.ResponseUrl

                let outputPath =
                    Path.Combine(outputFolder, $"{file.Id}{Path.GetFileName uri.LocalPath}")

                use fileStream = File.Create outputPath
                do! response.ResponseStream.CopyToAsync fileStream |> Async.AwaitTask
            | _ -> printfn "Ошибка при скачивании %s: %d" file.Url response.StatusCode

        with ex ->
            printfn "%A" ex
    }

let downloadFilesAsync (files: seq<FileDescription>) (outputFolder: string) =
    async {
        return!
            files
            |> Seq.map (fun file -> downloadFileAsync file outputFolder)
            |> fun download -> Async.Parallel(download, 5)
    }

let getHtmlAsync (url: string) =
    async { return! Http.AsyncRequestString(url, httpMethod = "GET") }

let extractAudioLinks (html: string) =
    let rMatch = Regex.Match(html, pattern, RegexOptions.IgnoreCase)

    if not rMatch.Success then
        printfn "Не удалось найти JSON-массив с аудиофайлами."
        None
    else
        let jsonArray = rMatch.Groups.["jsonArray"].Value.Trim()

        FilesProvider.ParseList(jsonArray)
        |> Seq.map (fun item -> FileDescription(item.Id, item.Title, item.Url))
        |> Seq.distinct
        |> Some


let downloadAudiobook (bookUrl: string) (outputDir: string) =
    async {
        Directory.CreateDirectory(outputDir) |> ignore
        let! html = getHtmlAsync bookUrl
        let links = extractAudioLinks html

        match links with
        | None -> printfn "Не удалось извлечь аудиофайлы."
        | Some links ->
            printfn "Найдено файлов: %d" (Seq.length links)
            links |> Seq.iter (fun file -> printfn "Файл: %s" file.Url)
            let! _ = downloadFilesAsync links outputDir
            printfn "Готово!"
    }

//Здесь задать нужную ссылку:
let bookUrl = "https://knigavuhe.org/book/shljagery"//"https://knigavuhe.org/book/biblija" // замените на нужную ссылку
let outputDir = Path.Combine(sourceDirectory, bookUrl.Split('/') |> Array.last)
downloadAudiobook bookUrl outputDir |> Async.RunSynchronously
// Использовать: dotnet fsi knigavuhe.fsx
