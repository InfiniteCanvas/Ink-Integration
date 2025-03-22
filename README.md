# Ink Integration for Unity

A Unity package that integrates Ink narrative scripting language with Unity, using MessagePipe to step through Ink story JSON files.

## Overview

This package integrates Ink (the narrative scripting language by Inkle) with Unity, using MessagePipe for message-based communication. It allows developers to
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

1. Open your Unity project
2. Navigate to Window > Package Manager
3. Click the "+" button and select "Add package from git URL..."
4. Enter `https://github.com/InfiniteCanvas/Ink-Integration.git`
5. Click "Add"

## Dependencies

This package requires the following dependencies which will be automatically installed:

- Ink Unity Integration: `com.inkle.ink-unity-integration`
- MessagePipe: `com.cysharp.messagepipe`
- MessagePipe.VContainer: `com.cysharp.messagepipe.vcontainer`
- UniTask: `com.cysharp.unitask`
- VContainer: `jp.hadashikick.vcontainer`
- Pooling Utility: `io.infinitecanvas.poolingutility`

Sometimes `Ink Unity Integration` doesn't install correctly as dependency and this package installation will abort.
Simply install it before installing this package, by following the same steps above for this url: `https://github.com/inkle/ink-unity-integration.git#upm`

## Usage

### Basic Setup

1. Create an Ink story asset:
    - Right-click in your Project window
    - Select Create > Infinite Canvas > Ink Story Asset
    - Assign your compiled Ink JSON file to the InkStoryJson field

2. Create your StoryLifetimeScope:
    - Create a StoryLifetimeScope class that inherits from LifetimeScope
    - add a serialized field to your LifetimeScope
    - inject dependencies of `StoryController`
    - Example:
        ```csharp
        public InkStoryAsset InkStoryAsset;

        protected override void Configure(IContainerBuilder builder){
        _ = builder.RegisterStoryControllerDependencies(InkStoryAsset, 
                                                       new StoryControllerLogSettings(StoryControllerLogSettings.LogLevel.Debug, (_, s) => Debug.Log(s)),
                                                       options =>
                                                       {
                                                            options.HandlingSubscribeDisposedPolicy = HandlingSubscribeDisposedPolicy.Ignore;
                                                       });
        }
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
- `>@` - Animation commands
- `>~` - Scene commands
- `>$` - UI commands
- `>%` - Image commands
- `>:` - Text commands
- `>>` - Other commands

Don't forget to send a `ContinueMessage` after processing a command - it doesn't do so automatically, even if it's not a normal text line.

Example in Ink:

```
>~Welcome
>%Greeter
>@wave
>!welcome.ogg
>$Welcome
Normal story text.
>:shake
>:default
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