After spending approximately 0.5 microseconds searching for flashcard tools online I found that several of them asked me to pay money in order to use them. This was so preposterous that I did not read any further to see whether or not it was actually the cards that were locked behind a paywall or if it was extra functionality; I immediately began working on my own virtual solution for flashcards.

The only GUI software I'd made any attempt to use properly before was [Avalonia](https://github.com/AvaloniaUI/Avalonia), more specifically [FuncUI](https://github.com/fsprojects/Avalonia.FuncUI), which provides a way to program UI stuff in F#. The problem here is that I do not really know F#. So now in my efforts to learn French I am also learning F#, all while I probably could have just read a bit more about those online services and used the flashcards there.

## Features
- Cards with two faces, click to flip
- Shuffle button randomly orders cards
- Save and load card sets as XML files
- Cards can make use of text to speech when flipped
  - Supported languages for TTS include English, Spanish, French, Hindi, Italian, Japanese, Brazilian Portuguese, and Chinese
  - I've only tested the English and French TTS, doesn't seem to get every word correct so definitely be careful with this feature!
  - There's definitely some oddness with my limited testing on Japanese, probably an issue with the tokenizer if I had to guess

## Prerequisites
- [CUDA Toolkit](https://developer.nvidia.com/cuda-toolkit)
- [cuDNN](https://developer.nvidia.com/cudnn)
- Make sure your system can see the libraries provided by the two prereqs above
- I think you also need a CUDA compatible GPU? Which actually is really bad lol, I should probably make the GPU accelerated stuff opt-in.
- I think NuGet probably handles everything else, idk though cause I don't use .net stuff much

## Cool libraries I used
- [FuncUI](https://github.com/fsprojects/Avalonia.FuncUI): Allows me to use [Avalonia](https://github.com/AvaloniaUI/Avalonia) in a slightly more F#ish manner
- [KokoroSharp](https://github.com/Lyrcaxis/KokoroSharp): Lets me use [Kokoro-82M](https://huggingface.co/hexgrad/Kokoro-82M) for TTS in .NET, pretty fun stuff.

## XML Schema
The XML Schema can be viewed [here](https://clementsaic.github.io/xml/flashcards.xsd). Since I haven't implemented an 'edit' function yet (which means the 'add card' feature I did implement is pretty useless, lol), to make card sets you'll need to write them up in xml. Fortunately, this is pretty easy. Here is an example of a valid card set XML file:

```xml
﻿<?xml version="1.0" encoding="utf-8"?>
<cards>
  <card>
    <face>This text will appear on the front of the card!</face>
    <back>This text will appear when the user flips the card to reveal the back!</back>
  </card>
  <card>
    <face>To use the text to speech, you will need to add pronunciation data!</face>
    <back>Specify TTS language using an ISO 639-1 language code!</back>
    <pronunciation>
      <face lang="en">To use different text for TTS, add the text to speak here!</face>
      <back lang="en-gb">You can run the TTS with multiple phrases.; Each extra phrase is preceded by a semicolon.</back>
    </pronunciation>
  </card>
  <card>
    <face>You can send the text from the face of the card to the TTS by simply not specifying TTS speech</face>
    </back>You do still need to specify language though!</back>
    <pronunciation>
      <face lang="en-us"/>
      <!--By not providing pronunciation for the back of the card, no TTS will occur when flipping to back.
          This works the same way with excluding pronunciation for the front of the card.-->
    </pronunciation>
  </card>
</cards>
```

## Final notes
I have only tested this on a single computer running Ubuntu 24.04, I *think* because of the .NET-ness it should run on other OSs as well, but although I could test that hypothesis I don't really have the time or motivation for it. If you encounter any OS-related problems though do let me know and I will try to fix it.

Also if you have any suggestions feel free to open up an issue with the suggestion, I like those. Even the ones that say "ur code is bad fix it", as long as they tell me why.

kthxbye
