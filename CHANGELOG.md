# CHANGELOG

Using [SemVer](https://semver.org/)

The gist of it is **[major.minor.patch]**:

- **Major**: API breaking changes, behaviour might change
- **Minor**: API upgrades, backwards compatible, behaviour unchanged
- **Patch**: API unchanged, fixed functionality

## [4.2.0] - 2025-04-10

### Changed

- Made the `logger` parameter optional in the extension method
  ```public static IContainerBuilder RegisterStoryControllerDependencies(this IContainerBuilder builder, ...)```
    - it will try to resolve the needed `Serilog.ILogger` instance from the lifetime scope
    - it will throw when none is found
- added an overload -> allows passing in `MessagePipeOptions` for when the message broker is already defined
    - ```csharp
      RegisterStoryControllerDependencies(this IContainerBuilder     builder,
                                          InkStoryAsset              inkStoryAsset,
                                          MessagePipeOptions         messagePipeOptions,
                                          ILogger                    logger                   = null,
                                          CommandProcessingOptions   commandProcessingOptions = default,
                                          AudioLibrary               audioLibrary             = null,
                                          ImageLibrary               imageLibrary             = null,
                                          Action<MessagePipeOptions> configure                = null)
      ```

## [4.1.0] - 2025-04-10

### Added

- Support for custom image commands in ink text
    - see README for more info
- made command processing optional

### Changed

- Updated basic sample

## [4.0.0] - 2025-04-03

### Added

- Support for custom audio commands in ink text
    - see README for more info
- Log settings for this package

### Changed

- `LineType` -> `CommandType`
- I should stop flip-flopping between these names lol
- adjusted sample (which will break if you don't have the FMOD example project)
- LogLevel for text and choice logging elevated to `Information`

## [3.0.0] - 2025-03-29

### Removed

- Old logging setup

### Added

- Serilog for logging

## [2.0.0] - 2025-03-22

### Removed

- Unity.Logging dependency

### Added

- Image commands

### Changed

- `CommandType` -> `LineType`
- `RegisterStoryControllerDependencies` has better configuration options now
- `ContinueMessage.Maximally = true` now only sends the text when encountering a command and buffers it; it will be published on the next continue message
- TextCommand -> `>%`
- ImageCommand -> `>:`
- OtherCommand -> `>>`

## [1.0.3] - 2025-03-20

### Changed

- dropped the '\n' for commands

## [1.0.2] - 2025-03-19

### Changed

- added extension for registering dependencies so making your own lifetime scope is easier

## [1.0.1] - 2025-03-19

### Changed

~~- fixed package dependency path~~

- nvm, rolled it back

## [1.0.0] - 2025-03-18

- Initial release of Ink Integration package

### Added

- Story controller for parsing and processing Ink stories
- Message-based architecture using MessagePipe
- Support for special commands (Audio, Animation, Scene, UI)
- Integration with VContainer for dependency injection
- Message types for text, choices, commands, and story flow control
- ScriptableObject wrapper for Ink story JSON files