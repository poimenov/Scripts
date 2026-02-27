#if INTERACTIVE

#r "nuget: Avalonia"
#r "nuget: Avalonia.Desktop"
#r "nuget: Avalonia.Themes.Fluent"
#r "nuget: Avalonia.FuncUI"
#r "nuget: FSharp.Data"
#r "nuget: LibVLCSharp"
#r "nuget: FluentIcons.Avalonia"

#endif

//https://funcui.avaloniaui.net/

open System
open System.Collections.ObjectModel
open System.IO
open Avalonia
open Avalonia.Controls
open Avalonia.Data
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Hosts
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media.Imaging
open FluentIcons.Avalonia
open FSharp.Data
open FSharp.Data.JsonExtensions
open LibVLCSharp.Shared

[<AutoOpen>]
module SymbolIcon =
    open Avalonia.FuncUI.Types
    open Avalonia.FuncUI.Builder
    open FluentIcons.Common
    open FluentIcons.Avalonia

    let create (attrs: IAttr<SymbolIcon> list) : IView<SymbolIcon> = ViewBuilder.Create<SymbolIcon> attrs

    type SymbolIcon with
        static member symbol<'t when 't :> SymbolIcon>(value: Symbol) : IAttr<'t> =
            AttrBuilder<'t>.CreateProperty<Symbol>(SymbolIcon.SymbolProperty, value, ValueNone)

type Track(id, title, artistId, artist, duration, downloadUrl, href) =
    member val Id = id with get, set
    member val Title = title with get, set
    member val Artist = artist with get, set
    member val ArtistId = artistId with get, set
    member val Duration = duration with get, set
    member val DownloadUrl = downloadUrl with get, set
    member val Href = false with get, set

[<AbstractClass; Sealed>]
type Views =
    static member main() =
        Component(fun ctx ->
            let data = ctx.useState (ObservableCollection<Track>([]))
            let selectedItem = ctx.useState<Option<Track>> None
            let searchButtonEnabled = ctx.useState true
            let downloadButtonEnabled = ctx.useState true
            let playEnabled = ctx.useState true
            let isPlaying = ctx.useState false
            let searchText = ctx.useState ""
            let libVlc = ctx.useState (new LibVLC())

            let getPlayer =
                let _player = new MediaPlayer(libVlc.Current)

                _player.EndReached.Add(fun _ ->
                    isPlaying.Set false
                    _player.Media.Dispose()
                    _player.Media <- null)

                _player

            let player = ctx.useState getPlayer
            let baseUri = new Uri "https://m.z3.fm"

            let getAsyncSearch keyword =
                async {
                    let uri = new Uri(baseUri, "mp3/search")
                    let query = [ "keywords", keyword ]

                    let headers =
                        [ HttpRequestHeaders.Accept "application/json"
                          "x-requested-with", "XMLHttpRequest" ]

                    let! text =
                        Http.AsyncRequestString(uri.AbsoluteUri, httpMethod = "GET", query = query, headers = headers)

                    return text
                }

            let getTrack jsonVal =
                let id = jsonVal?id.AsInteger()
                let title = jsonVal?title.AsString()
                let artistId = jsonVal?artist_id.AsInteger()
                let artist = jsonVal?artist.AsString()
                let t = TimeSpan.FromSeconds(float (jsonVal?duration.AsInteger()))
                let duration = String.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds)
                let href = jsonVal?href.AsString()
                let downloadUrl = "/download/" + id.ToString()
                Track(id, title, artistId, artist, duration, downloadUrl, href)

            let doSearch =
                async {
                    searchButtonEnabled.Set false
                    if isPlaying.Current then
                        player.Current.Stop()
                        isPlaying.Set false                    

                    try
                        let! text = getAsyncSearch searchText.Current
                        let arrJson = JsonValue.Parse(text).AsArray()
                        let tracks = arrJson |> Seq.map getTrack |> Seq.toList |> ObservableCollection
                        data.Set tracks
                    with ex ->
                        printfn "%A" ex

                    searchButtonEnabled.Set(true)
                }

            let doDownload =
                async {
                    match selectedItem.Current with
                    | None -> ()
                    | Some track ->
                        let uri = new Uri(baseUri, track.DownloadUrl)
                        downloadButtonEnabled.Set(false)

                        try
                            let! response = Http.AsyncRequest uri.AbsoluteUri
                            //response.Headers |> Seq.iter (fun x -> printfn "%A" x)

                            match response.Body with
                            | Binary bytes ->
                                File.WriteAllBytes(
                                    Path.Combine(
                                        Environment.GetFolderPath Environment.SpecialFolder.MyMusic,
                                        response.Headers.["Content-Disposition"].Split("filename=").[1].Split(",").[0]
                                    ),
                                    bytes
                                )
                            | Text(_) -> ignore ()
                        with ex ->
                            printfn "%A" ex

                        downloadButtonEnabled.Set true
                }

            let play =
                async {
                    match selectedItem.Current with
                    | None -> ()
                    | Some track ->
                        let uri = new Uri(baseUri, track.DownloadUrl)
                        playEnabled.Set false

                        try
                            let! inputStream = Http.AsyncRequestStream uri.AbsoluteUri
                            let memoryStream = new MemoryStream()
                            inputStream.ResponseStream.CopyTo memoryStream
                            player.Current.Media <- new Media(libVlc.Current, new StreamMediaInput(memoryStream))
                            isPlaying.Set(player.Current.Play())
                        with ex ->
                            printfn "%A" ex

                        playEnabled.Set true
                }

            let playStop =
                async {
                    if isPlaying.Current then
                        player.Current.Stop()
                        isPlaying.Set false
                    else
                        play |> Async.Start
                }

            DockPanel.create
                [ DockPanel.children
                      [ Grid.create
                            [ Grid.dock Dock.Top
                              Grid.margin 4
                              Grid.columnDefinitions "*, Auto, Auto, Auto"
                              Grid.rowDefinitions "Auto, *"
                              Grid.children
                                  [ TextBox.create
                                        [ Grid.column 0
                                          Grid.row 0
                                          TextBox.margin 4
                                          TextBox.watermark "Search music"
                                          TextBox.horizontalAlignment HorizontalAlignment.Stretch
                                          TextBox.onKeyDown (fun e ->
                                              if e.Key = Key.Enter then
                                                  Async.StartImmediate doSearch)
                                          TextBox.onTextChanged (fun e -> searchText.Set e) ]
                                    Button.create
                                        [ Grid.column 1;Grid.row 0
                                          Button.content  (
                                                    SymbolIcon.create
                                                  [ SymbolIcon.width 24
                                                    SymbolIcon.height 24
                                                    SymbolIcon.symbol FluentIcons.Common.Symbol.Search ]
                                          )
                                          Button.isEnabled (
                                              searchButtonEnabled.Current
                                              && not (String.IsNullOrWhiteSpace searchText.Current)
                                          )
                                          Button.horizontalContentAlignment HorizontalAlignment.Center
                                          Button.onClick (fun _ -> Async.StartImmediate doSearch) ]
                                    Button.create
                                        [ Grid.column 2;Grid.row 0
                                          Button.content (
                                              SymbolIcon.create
                                                  [ SymbolIcon.width 24
                                                    SymbolIcon.height 24
                                                    SymbolIcon.symbol
                                                        (if isPlaying.Current then
                                                            FluentIcons.Common.Symbol.Stop
                                                         else
                                                            FluentIcons.Common.Symbol.Play) ]
                                          )
                                          Button.isEnabled (selectedItem.Current.IsSome && playEnabled.Current)
                                          Button.horizontalContentAlignment HorizontalAlignment.Center
                                          Button.onClick (fun _ -> Async.StartImmediate playStop) ]
                                    Button.create
                                        [ Grid.column 3;Grid.row 0
                                          Button.content (
                                              SymbolIcon.create
                                                  [ SymbolIcon.width 24
                                                    SymbolIcon.height 24
                                                    SymbolIcon.symbol FluentIcons.Common.Symbol.ArrowDownload ]
                                          )
                                          Button.isEnabled (
                                              selectedItem.Current.IsSome && downloadButtonEnabled.Current
                                          )
                                          Button.horizontalContentAlignment HorizontalAlignment.Center
                                          Button.onClick (fun _ -> Async.StartImmediate doDownload) ] ] ]
                        DataGrid.create
                            [ DataGrid.dock Dock.Top
                              Grid.row 1
                              Grid.columnSpan 4
                              DataGrid.isReadOnly true
                              DataGrid.items data.Current
                              DataGrid.onSelectedItemChanged (fun item ->
                                  (match box item with
                                   | null -> None
                                   | :? Track as i -> Some i
                                   | _ -> failwith "Something went horribly wrong!")
                                  |> selectedItem.Set)

                              DataGrid.columns
                                  [ DataGridTextColumn.create
                                        [ DataGridTextColumn.header "Artist"
                                          DataGridTextColumn.width (DataGridLength 200.0)
                                          DataGridTextColumn.binding (Binding "Artist") ]
                                    DataGridTextColumn.create
                                        [ DataGridTextColumn.header "Title"
                                          DataGridTextColumn.width (DataGridLength(490.0, DataGridLengthUnitType.Star))
                                          DataGridTextColumn.binding (Binding "Title") ]
                                    DataGridTextColumn.create
                                        [ DataGridTextColumn.header "Duration"
                                          DataGridTextColumn.width (DataGridLength 100.0)
                                          DataGridTextColumn.binding (Binding "Duration") ] ] ] ] ])

type MainWindow() as this =
    inherit HostWindow()

    do
        base.Title <- "z3.fm - Music Search/Download"
        base.Width <- 800.0
        base.Height <- 500.0
        base.Icon <- new WindowIcon(new Bitmap(Path.Combine(__SOURCE_DIRECTORY__, "img/Fsharp_logo.png")))
        this.Content <- Views.main ()


type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Add(Themes.Fluent.FluentTheme())
        this.RequestedThemeVariant <- Styling.ThemeVariant.Dark
        this.Styles.Load "avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml"

    override this.OnFrameworkInitializationCompleted() =

        match this.ApplicationLifetime with
        | :? ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime as desktop ->
            desktop.MainWindow <- MainWindow()
            printfn "App running..."
        | _ -> ()

let app =
    AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .StartWithClassicDesktopLifetime([||])
