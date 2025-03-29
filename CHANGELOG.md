# CHANGELOG

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