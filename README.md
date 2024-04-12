# MovieSweep

## Overview

`MovieSweep` is a utility for organizing and cleaning up movie download folders. It simplifies the management of your movie collection by renaming folders and sorting files.

## Features

- **Folder Renaming**: Standardizes movie folder names to a "Movie Title (Year)" format for consistency and ease of browsing.

  - Example: `The.Great.Escape.1963.1080p.BluRay.x264` is renamed to `The Great Escape (1963)`.

- **File Cleanup**: Relocates extraneous files like text documents and images to a `.MovieSweepBackup` directory to declutter the main movie folders.

  - Example: `RARBG.com.txt` would be moved to `.MovieSweepBackup/The Great Escape (1963)/`.

- **Sample Folder Management**: Preserves the directory structure for 'Sample' folders by moving them intact to the backup directory.

  - Example: `Sample/sample-video.mkv` within the `The Great Escape (1963)` folder would be backed up to `.MovieSweepBackup/The Great Escape (1963)/Sample/sample-video.mkv`.

- **Centralized Backup**: Non-essential files are securely backed up to a single `.MovieSweepBackup` folder within the application's directory.
- **Logging**: Actions and errors are logged to `success.log` and `error.log` files respectively, allowing for transparent tracking of the sweep process.
- **Command-Line Arguments**:

  - `-v`: Verbose mode, which provides detailed operation logs in the console.
  - `-a`: Auto mode, which bypasses confirmation prompts for folder renaming and file backup.

## Usage

To use `MovieSweep`, place the `MovieSweep.exe` executable in the root directory of your movie collection and run it with the desired command-line arguments.

Example usage:

cssCopy code

`MovieSweep.exe -v -a`

## Requirements

- .NET 6.0 Runtime
- Compatible with Windows operating systems

## Contributing

If you would like to contribute to `MovieSweep`, please review the contribution guidelines first. Your input is valued!

## License

`MovieSweep` is freely distributed under the MIT License.
