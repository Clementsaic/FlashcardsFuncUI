namespace CounterApp

open System.Threading
open System.Xml.Linq
open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Media
open Avalonia.Platform.Storage
open Avalonia.Themes.Fluent
open Avalonia.FuncUI.Hosts
open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open KokoroSharp
open Microsoft.FSharp.Collections
open Microsoft.FSharp.Core
open Microsoft.ML.OnnxRuntime
open OpenTK.Audio.OpenAL

type Card =
    { Face: string
      Back: string
      FaceUp: bool
      Id: int
      LoadId: int }

module Main =

    let ttsOptions = new SessionOptions()
    ttsOptions.AppendExecutionProvider_CUDA()
    ttsOptions.LogSeverityLevel <- OrtLoggingLevel.ORT_LOGGING_LEVEL_INFO

    let ttsVoice = KokoroVoiceManager.GetVoice "ff_siwis"
    
    let kokoro = KokoroTTS.LoadModel("./kokoro.onnx", ttsOptions)

    kokoro.SpeakFast("Text to speech initialized!", ttsVoice) |> ignore

    let wavSynth =
        new KokoroSharp.Utilities.KokoroWavSynthesizer("./kokoro.onnx", ttsOptions)

    let emitSpeech (wav: byte[]) =
        async {
            let bufferAL = AL.GenBuffer()
            let sourceAL = AL.GenSource()

            AL.BufferData(bufferAL, ALFormat.Mono16, wav, 24000)
            AL.Source(sourceAL, ALSourcei.Buffer, bufferAL)
            AL.SourcePlay(sourceAL)

            while (enum<ALSourceState> (AL.GetSource(sourceAL, ALGetSourcei.SourceState)) = ALSourceState.Playing) do
                Thread.Sleep(250)

            AL.SourceStop(sourceAL)
            AL.DeleteSource(sourceAL)
            AL.DeleteBuffer(bufferAL)
        }


    let tts (text) =
        wavSynth.Synthesize(text, ttsVoice) |> emitSpeech |> Async.Start

    let view () =
        Component(fun ctx ->
            let state = ctx.useState []

            let mouseState = ctx.useState (Point(0.0, 0.0))

            let xmlFileType =
                FilePickerFileType(
                    "XML Document",
                    Patterns = [ "*.xml" ],
                    AppleUniformTypeIdentifiers = [ "public.xml" ],
                    MimeTypes = [ "application/xml" ]
                )

            let parseCard (card: XElement, id: int) =
                card.Elements()
                |> Seq.fold
                    (fun card element ->
                        match element with
                        | e when string e.Name = "face" -> { card with Face = e.Value }
                        | e when string e.Name = "back" -> { card with Back = e.Value }
                        | _ -> card)
                    { Face = ""
                      Back = ""
                      FaceUp = true
                      Id = id
                      LoadId = id }

            let parseCardSet (cardSet: XDocument) =
                ([], cardSet.Root.Elements())
                ||> Seq.fold (fun cards element ->
                    match element with
                    | e when string e.Name = "card" -> cards @ [ (parseCard (element, List.length cards)) ] // prob not ideal but good enough for now
                    | _ -> cards)

            let loadCardSet () =
                async {
                    let! docs =
                        (TopLevel.GetTopLevel ctx.control)
                            .StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents)
                        |> Async.AwaitTask

                    let options =
                        FilePickerOpenOptions(
                            Title = "Select Card Set",
                            SuggestedStartLocation = docs,
                            AllowMultiple = false,
                            FileTypeFilter = [ xmlFileType ]
                        )

                    let! files =
                        (TopLevel.GetTopLevel ctx.control).StorageProvider.OpenFilePickerAsync(options)
                        |> Async.AwaitTask
                    
                    if Seq.length files = 0 then () else
                        let! stream = files[0].OpenReadAsync() |> Async.AwaitTask
                        let cardsXML = XDocument.Load(stream)
                        let cards = parseCardSet cardsXML
                        state.Set(cards)
                }

            let serializeCardSet () =
                XDocument(
                    XElement(
                        "cards",
                        (state.Current, [])
                        ||> Seq.foldBack (fun card cardsXML ->
                            XElement("card", [ XElement("face", card.Face); XElement("back", card.Back) ])
                            :: cardsXML)
                    )
                )


            let saveCardSet () =
                async {
                    let cardsXML = serializeCardSet ()

                    let! docs =
                        (TopLevel.GetTopLevel ctx.control)
                            .StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents)
                        |> Async.AwaitTask

                    let options =
                        FilePickerSaveOptions(
                            Title = "Save Card Set",
                            SuggestedStartLocation = docs,
                            ShowOverwritePrompt = true,
                            FileTypeChoices = [ xmlFileType ]
                        )

                    let! file =
                        (TopLevel.GetTopLevel ctx.control).StorageProvider.SaveFilePickerAsync(options)
                        |> Async.AwaitTask
                    
                    match file with
                    | null -> ()
                    | output ->
                        let! outStream = output.OpenWriteAsync() |> Async.AwaitTask
                        cardsXML.Save(outStream)
                }

            let emptyCard =
                { Face = ""
                  Back = ""
                  FaceUp = true
                  Id = -1
                  LoadId = -1 }

            let flipCard (cards: Card List, id: int) =
                if cards[id].FaceUp then tts cards[id].Back else ()

                List.foldBack
                    (fun card cardsUpdated ->
                        { card with
                            FaceUp = (if card.Id = id then not card.FaceUp else card.FaceUp) }
                        :: cardsUpdated)
                    cards
                    []

            let deleteCard (cards: Card List, id: int) =
                List.foldBack
                    (fun card cardsUpdated ->
                        if card.Id = id then
                            cardsUpdated
                        else
                            { card with
                                Id = card.Id - if card.Id > id then 1 else 0 }
                            :: cardsUpdated)
                    cards
                    []

            let swapCards (cards: Card List, id1: int, id2: int) =
                List.foldBack
                    (fun card cardsUpdated ->
                        match card.Id with
                        | id when id = id1 -> cards[id2]
                        | id when id = id2 -> cards[id1]
                        | _ -> card
                        :: cardsUpdated)
                    cards
                    []

            let setAllCards (cards: Card List, faceUp: bool) =
                List.foldBack (fun card cardsUpdated -> { card with FaceUp = faceUp } :: cardsUpdated) cards []

            let shuffleCards (cards: Card List) =
                (List.randomShuffle cards)
                |> List.fold
                    (fun cardsUpdated card ->
                        cardsUpdated
                        @ [ { card with
                                Id = List.length cardsUpdated } ])
                    []

            let cardButtons (cards: Card List) : Types.IView List =
                (List.foldBack
                    (fun card views ->
                        Button.create
                            [ Button.width 250
                              Button.height 150
                              Button.margin 20
                              Button.clipToBounds false
                              Button.verticalContentAlignment VerticalAlignment.Center
                              Button.horizontalContentAlignment HorizontalAlignment.Center
                              Button.borderThickness 5
                              Button.borderBrush (
                                  if card.FaceUp then
                                      Color.FromRgb(0x7Fuy, 0x7Fuy, 0xBFuy)
                                  else
                                      Color.FromRgb(0xBFuy, 0x7Fuy, 0x7Fuy)
                              )
                              Button.cornerRadius 25
                              Button.background (
                                  if card.FaceUp then
                                      Color.FromRgb(0x7Fuy, 0x7Fuy, 0xFFuy)
                                  else
                                      Color.FromRgb(0xFFuy, 0x7Fuy, 0x7Fuy)
                              )
                              Button.content (
                                  TextBlock.create
                                      [ TextBlock.textWrapping TextWrapping.Wrap
                                        TextBlock.fontSize 24
                                        TextBlock.textAlignment TextAlignment.Center
                                        TextBlock.text (if card.FaceUp then card.Face else card.Back) ]
                              )
                              Button.onClick (fun _ -> state.Set(flipCard (state.Current, card.Id)))
                              Button.onPointerPressed (fun args -> mouseState.Set(args.GetPosition ctx.control))
                              Button.onPointerMoved (fun args ->
                                  let pos = args.GetPosition ctx.control

                                  let offset =
                                      pos
                                      - (if args.Properties.IsLeftButtonPressed then
                                             mouseState.Current
                                         else
                                             pos)

                                  (args.Source :?> Visual).RenderTransform <- TranslateTransform(offset.X, offset.Y)

                                  args.Handled <- true)
                              Button.onPointerReleased (fun args ->
                                  (args.Source :?> Visual).RenderTransform <- TranslateTransform(0.0, 0.0))
                              Button.onContextRequested (fun args ->
                                  state.Set(deleteCard (state.Current, card.Id))
                                  args.Handled <- true) ]
                        :: views)
                    cards
                    [])
                @ [ Button.create
                        [ Button.width 250
                          Button.height 150
                          Button.margin 20
                          Button.verticalContentAlignment VerticalAlignment.Center
                          Button.horizontalContentAlignment HorizontalAlignment.Center
                          Button.borderThickness 5
                          Button.borderBrush (Color.FromRgb(0x7Fuy, 0x7Fuy, 0x7Fuy))
                          Button.cornerRadius 25
                          Button.background (Color.FromRgb(0x3Fuy, 0x3Fuy, 0x3Fuy))
                          Button.content "+"
                          Button.onClick (fun _ ->
                              state.Set(
                                  state.Current
                                  @ [ { emptyCard with
                                          Id = List.length state.Current } ]
                              )) ] ]

            let stateString (cards: Card List) =
                List.foldBack (fun card ids -> $"{card.LoadId}.{if card.FaceUp then 0 else 1}" :: ids) cards []
                |> String.concat "-"

            // printfn "%s" (stateString state.Current)

            let cardView (cards: Card List) =
                Component.create (
                    stateString cards,
                    fun ctx -> WrapPanel.create [
                        WrapPanel.background (Color.FromRgb (0x2buy, 0x2buy, 0x2buy))
                        WrapPanel.children (cardButtons state.Current)
                    ]
                )

            DockPanel.create
                [ DockPanel.children
                      [ Menu.create
                            [ Menu.dock Dock.Top
                              Menu.background (Color.FromRgb (0x20uy, 0x20uy, 0x20uy))
                              Menu.viewItems
                                  [ MenuItem.create
                                        [ MenuItem.header "Load"
                                          MenuItem.onClick (fun _ -> loadCardSet () |> Async.StartImmediate) ]
                                    MenuItem.create
                                        [ MenuItem.header "Save"
                                          MenuItem.onClick (fun _ -> saveCardSet () |> Async.StartImmediate) ]
                                    MenuItem.create
                                        [ MenuItem.header "FaceUp"
                                          MenuItem.onClick (fun _ -> state.Set(setAllCards (state.Current, true))) ]
                                    MenuItem.create
                                        [ MenuItem.header "FaceDown"
                                          MenuItem.onClick (fun _ -> state.Set(setAllCards (state.Current, false))) ]
                                    MenuItem.create
                                        [ MenuItem.header "Shuffle"
                                          MenuItem.onClick (fun _ ->
                                              state.Set(setAllCards (shuffleCards state.Current, true))) ] ] ]
                        ContentControl.create [ ContentControl.content (cardView state.Current) ] ] ])

type MainWindow() =
    inherit HostWindow()

    do
        base.Title <- "FuncCards"
        base.Content <- Main.view ()

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Add(FluentTheme())
        this.RequestedThemeVariant <- Styling.ThemeVariant.Dark

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime -> desktopLifetime.MainWindow <- MainWindow()
        | _ -> ()

module Program =

    [<EntryPoint>]
    let main (args: string[]) =
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(args)
