# Ink Integration for Unity

A Unity package that integrates Ink narrative scripting language and FMOD with Unity, using MessagePipe to step through Ink story JSON files.

## Overview

This package integrates Ink (the narrative scripting language by Inkle) and FMOD with Unity, using MessagePipe for message-based communication. It allows developers to
incorporate interactive storytelling into Unity projects with a decoupled architecture.

## Features

- Load and parse Ink story JSON files
- Step through narrative content with a message-based architecture
- Handle choices and branching narratives
- Process special commands for audio, animation, UI, and scene management
- Separation of concerns using dependency injection with VContainer

## Installation

### Tested Unity Versions

- Unity 6000.1

### Via Unity Package Manager

1. Open your Unity project and install [dependencies](#dependencies)
2. Navigate to Window > Package Manager
3. Click the "+" button and select "Add package from git URL..."
4. Enter `https://github.com/InfiniteCanvas/Ink-Integration.git`
5. Click "Add"

## Dependencies

This package requires the following dependencies:

These need to be manually installed before adding the package:

- Serilog
    - Install via `NuGet for Unity` : `https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity`
    - Install Serilog using `NuGet for Unity`
- Ink Unity Integration: `com.inkle.ink-unity-integration`
    - Install via `Ink Unity Integration`: `https://github.com/inkle/ink-unity-integration.git#upm`
- FMOD 2.03
    - get from [their website](https://www.fmod.com/download#fmodforunity) or from the unity asset store
- Odin Inspector
    - since we're using it, you'll have to buy it on the asset store or this package will probably throw errors

These will be automatically installed:

- MessagePipe: `https://github.com/Cysharp/MessagePipe.git?path=src/MessagePipe.Unity/Assets/Plugins/MessagePipe`
- MessagePipe.VContainer: `https://github.com/Cysharp/MessagePipe.git?path=src/MessagePipe.Unity/Assets/Plugins/MessagePipe.VContainer`
- UniTask: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
- VContainer: `https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer`
- Pooling Utility: `https://github.com/InfiniteCanvas/Pooling-Utility.git`
- Utilities for Unity: `https://github.com/InfiniteCanvas/Utilities-For-Unity.git`
- Serilog Integration: `https://github.com/InfiniteCanvas/Serilog-Integration.git`

## Usage

### Basic Setup

1. Create an Ink story asset:
    - Right-click in your Project window
    - Select Create > Infinite Canvas > Ink Story Asset
    - Assign your compiled Ink JSON file to the InkStoryJson field

2. Create your StoryLifetimeScope:
    - Create a StoryLifetimeScope class that inherits from LifetimeScope
    - add a serialized field for `InkStoryAsset` to your LifetimeScope
    - inject dependencies of `StoryController`
    - Example:
        ```csharp
        public InkStoryAsset       InkStoryAsset;
        public AudioLibrary        AudioLibrary;
        public ImageLibrary        ImageLibrary;
        public LogSettingOverrides LogSettings;
  
        var logger = new LoggerConfiguration().OverrideLogLevels(LogSettings)
                                              .WriteTo.Unity()
                                              .CreateLogger();


        _ = builder.RegisterStoryControllerDependencies(InkStoryAsset,
                                                        logger,
                                                        new CommandProcessingOptions(true, true),
                                                        AudioLibrary,
                                                        ImageLibrary,
                                                        options => options.HandlingSubscribeDisposedPolicy = HandlingSubscribeDisposedPolicy.Ignore);
        ```

3. Set up the StoryLifetimeScope:
    - Create an empty GameObject in your scene
    - Add the StoryLifetimeScope component
    - Assign your Ink Story Asset to the component

Read [VContainer](https://github.com/hadashiA/VContainer) docs for more information on the LifetimeScope.

### Stepping Through a Story

The package uses a message-based system to control story flow:

```csharp
// To continue the story
_continuePublisher.Publish(false); // true to continue until a choice or command

// To select a choice
_choiceSelectedPublisher.Publish(choiceIndex);
```

### Handling Story Events

Subscribe to the message types to respond to story events:

```csharp
// Subscribe to text messages
_textSubscriber.Subscribe(text => {
    Debug.Log($"Story text: {text}");
});

// Subscribe to choices
_choiceSubscriber.Subscribe(choiceMessage => {
    foreach (var choice in choiceMessage.Choices) {
        Debug.Log($"Choice: {choice.text}");
    }
});

// Subscribe to commands
_commandSubscriber.Subscribe(command => {
    switch (command.LineType) {
        case LineType.Audio:
            // Handle audio command
            break;
        case LineType.Animation:
            // Handle animation command
            break;
        // etc.
    }
});
```

## Special Commands

The system recognizes special command syntax in your Ink files:

- `>!` - Audio commands
    - Defaults:
        - position = `0,0,0`
        - oneshot = true
    - Use like this: `>!EventName>other:params>other2:params1;params2`
    - delimit parameters with `>` and within those, delimit multiple values with `;`
    - `p:x;y;z` -> plays this audio at position Vector3(x,y,z); components must be valid floats
        - needs at least `x` and `y`
        - specifying only `x` and `y` will set z to `0`
    - `pn:name:value` -> name is the event's parameter name, value must be a valid float
    - `pnl:name:label` -> name is the event's parameter name, label is the parameter's label
    - `a:Play/Stop/Toggle/Remove` -> to play a track, toggle pause/resume, remove a track, stop a track
    - when `a:play` is used, this event will be a tracked instance instead of a one shot (use it for music or atmo sfx)
    - `>!->a:stop` is a special command and will stop all tracked instances
        - tracked instances are not automatically removed when the instance is finished playing (for now)
- `>@` - Animation commands
- `>~` - Scene commands
- `>$` - UI commands
- `>:` - Image commands
    - Defaults:
        - position = `0,0,0`
        - scale = `1,1,1`
        - location = `worldspace`
    - Use like this: `>:NameSpace;Pose>l:w>p:-6;1>s:1;1.1`
        - Used without specifying the pose like this `>:NameSpace` will set the pose to `default`
    - Using another command with the same namespace will replace existing sprites with that namespace
    - delimit parameters with `>` and within those, delimit multiple values with `;`
    - `p:x;y;z` -> puts the sprite at position Vector3(x,y,z); components must be valid floats
        - specifying only `x` and `y` will set z to `0`
        - needs at least `x` and `y`
    - `s:x;y;z` -> puts the sprite at position Vector3(x,y,z); components must be valid floats
        - specifying only `x` and `y` will set z to `1`
        - needs at least `x` and `y`
    - `l:s` - sets image to screen space ui
    - `l:w` - sets image to world space
- `>%` - Text commands
- `>>` - Other commands

Don't forget to send a `ContinueMessage` after processing a command - it doesn't do so automatically, even if it's not a normal text line.

Example in Ink:

```
>~Welcome
>@wave
>!Welcome
>:Greeter;wave
Hello!
>:Greeter
Normal story text.
>%shake
>%default
Use `Other commands` for things like auto saves, like this:
>>Checkpoint
```

## Architecture

The package follows an architecture using dependency injection:

- `StoryLifetimeScope`: Container for dependency injection
- `StoryController`: Main controller that handles story progression
- `InkStoryAsset`: ScriptableObject wrapper for Ink story JSON
- Message types:
    - `TextMessage`: Story text content
    - `ChoiceMessage`: Available choices
    - `ChoiceSelectedMessage`: Player's choice selection
    - `CommandMessage`: Special commands for game integration
    - `ContinueMessage`: Signal to continue the story
    - `EndMessage`: Signal that the story has ended
    - `SaveMessage`: Saves current story state to specified path
    - `LoadMessage`: Loads story state at specified path

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- [Inkle](https://www.inklestudios.com/) for creating the Ink narrative scripting language and its [Unity integration](https://github.com/inkle/ink-unity-integration)
- [MessagePipe](https://github.com/Cysharp/MessagePipe) for the messaging system
- [VContainer](https://github.com/hadashiA/VContainer) for dependency injection