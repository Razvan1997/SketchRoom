SketchRoom

SketchRoom is a collaborative drawing & white-boarding application built in C# WPF from scratch. It enables multiple users to work together on sketches, diagrams, and visual content in real time or asynchronously.

Key Features

Real-time and asynchronous sketch collaboration via modules like DrawingStateService, WhiteBoardModule, UsersInteractionsModule.

Supports multiple frameworks: WPF toolkit, backend services, database component.

Modular architecture with distinct layers:

SketchRoom.Database – for persistence layer

SketchRoom.Services – business logic & operations

SketchRoom.Toolkit.Wpf – front-end WPF toolkit & UI controls

WhiteBoard.Core & WhiteBoardModule – core drawing abstractions

Ready for deployment via MSIX package (SketchRoomMSIXPackage) and setup project (SketchRoomSetup).

Demo included (WalkthroughDemo.zip) to get started quickly.
