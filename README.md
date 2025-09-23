# EyeLog with C# Readme

EyeLog is a simple C# program that tracks eye movements using the Tobii Interaction SDK. It allows you to define custom areas of interest on the screen and logs when the user's gaze enters, exits, or stays within these areas. Additionally, it can detect and log clicks based on the user's gaze staying within a defined area for a certain duration.

## Prerequisites

Before running EyeLog, make sure you have the following prerequisites installed:

1. **Tobii Interaction SDK**: EyeLog relies on the Tobii Interaction SDK to access eye-tracking data. Ensure you have the SDK installed on your system.

2. **C# Development Environment**: You need a C# development environment to compile and run the code. Visual Studio is recommended, but you can use any C# IDE of your choice.

3. **Visual C++ Redistributable (x64)**: Required for Tobii native libraries.  
   Download and install from: [Microsoft Visual C++ Redistributable](https://aka.ms/vs/17/release/vc_redist.x64.exe).

## Getting Started

1. Clone or download the EyeLog repository to your local machine.

2. Open the project in your C# development environment.

3. Build and run the project.

## Usage

EyeLog listens for user input and gaze data, allowing you to interact with it via the command line. Here are the available options and commands:

### Command-Line Flags

- **`--raw`**  
  When this flag is provided, EyeLog will output raw gaze data directly to the console in the format:  
```
X,Y,Timestamp
```
This is useful if you want unprocessed eye-tracking data.

- **`--out=<filename>`**  
Use this flag to write output logs to a file in addition to the console. For example:  
```
EyeLog.exe --out=gaze_log.txt
```
This will create (or append to) a file named `gaze_log.txt` containing the log data.

### Interactive Commands

- **Define Areas of Interest**:  
You can define custom areas of interest on the screen by inputting their coordinates and dimensions in the following format:  
```
x1,y1,width1,height1;x2,y2,width2,height2;...
```
Example:  
```
100,100,200,200;300,300,150,150;
```

- **Change Click Timeout**:  
You can adjust the click detection timeout by typing `timeout:<milliseconds>`.  
Example:  
```
timeout:1500
```

- **Exit the Program**:  
Exit at any time by using your system's exit command (e.g., pressing `Ctrl+C`).

## How It Works

EyeLog runs two main threads:

1. **InputCycle Thread**: Listens for user input via the command line. You can use it to define areas of interest and adjust the click timeout.

2. **EyeCycle Thread**: Continuously monitors the user's gaze data and tracks when it enters, exits, or stays within the defined areas of interest. It also logs click events based on the specified timeout.

## Output

EyeLog outputs information to the console (and optionally a file if `--out` is specified):

- **Raw Mode (`--raw`)**:  
Continuous gaze coordinates in `X,Y,Timestamp` format.

- **Enter**: When the user's gaze enters an area of interest, it logs the area's index.
- **Exit**: When the user's gaze exits an area of interest, it logs an "exit" message.
- **Click**: When the user's gaze stays within an area of interest for the specified click timeout, it logs the area's index and the number of clicks.

## Customize and Extend

Feel free to customize and extend EyeLog according to your needs. You can add more functionality or integrate it with other systems and applications.

## Troubleshooting

If you encounter issues:
- Make sure Tobii Experience (or Tobii Interaction SDK services) is running.
- Verify you installed the [Visual C++ Redistributable (x64)](https://aka.ms/vs/17/release/vc_redist.x64.exe).
- Confirm your project is built for **x64** (to match Tobii SDK libraries).
- Refer to the Tobii Interaction SDK documentation or seek assistance from the Tobii support community.

## License

This project is provided under the [MIT License](LICENSE.md).

---

Enjoy using EyeLog to track and log eye movements and interactions! If you find it helpful, consider contributing to the project or sharing your enhancements with the community.
