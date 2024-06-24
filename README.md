
# TeraCyte Home Assignment

## Overview

This application is designed to process images, adjust their brightness, and manage histograms. It includes a WPF frontend and integrates with an Azure Function App for sending histogram data to the cloud.

## Table of Contents
- [Prerequisites](#prerequisites)
- [Running the Application](#running-the-application)
- [Multithreading Implementation](#multithreading-implementation)
- [Sending Histogram Data to the Cloud](#sending-histogram-data-to-the-cloud)

## Prerequisites
- Visual Studio 2019 or later
- .NET Core 3.1 SDK or later
- OpenCVSharp4

## Running the Application

1. **Clone the repository:**
   ```sh
   git clone https://github.com/epeer1/TeraCyteHomeAssignment.git
   cd TeraCyteHomeAssignment
   ```

2. **Open the solution:**
   - Open `TeraCyteHomeAssignment.sln` in Visual Studio.

3. **Restore NuGet packages:**
   - In Visual Studio, right-click on the solution in the Solution Explorer and select `Restore NuGet Packages`.

4. **Build the solution:**
   - Press `Ctrl+Shift+B` to build the solution.

5. **Run the application:**
   - Press `F5` to run the application.

## Multithreading Implementation

### MVVM Design Pattern

The application follows the Model-View-ViewModel (MVVM) design pattern to separate the business logic from the UI. This design pattern ensures a clear separation of concerns, making the code more maintainable and testable.

### Background Tasks with Multithreading

To maintain a responsive UI, the application uses multithreading for long-running operations. Here's a high-level overview of how multithreading is implemented:

1. **Image Loading:**
   - Image loading is performed on a background thread to prevent the UI from freezing. The image is loaded in the background, and once the loading is complete, the result is invoked on the UI thread to update the display.

2. **Brightness Adjustment:**
   - Adjusting the brightness of an image is computationally intensive. This operation is offloaded to a background thread. Once the brightness adjustment is complete, the updated image is invoked on the UI thread to update the display.

3. **Histogram Calculation:**
   - Histogram calculation is also performed on a background thread. This ensures that the calculation does not block the UI thread. The histogram data is then invoked on the UI thread to update the histogram display.

4. **Command Handling:**
   - User commands (e.g., loading an image, adjusting brightness) are handled using asynchronous commands. This allows the application to remain responsive while processing the commands in the background.

## Sending Histogram Data to the Cloud

The application includes a feature to send histogram data to an Azure Function App. This is done automatically without any user intervention.

### Process Overview

1. **Generate Histogram:**
   - The application processes the image and generates a histogram. The histogram data is maintained within the application.

2. **Send Histogram Data:**
   - The histogram data is automatically sent to an Azure Function App. This function app is responsible for storing the histogram data in the cloud.

3. **Azure Function App:**
   - The Azure Function App processes the incoming histogram data and stores it. The URL for the Azure Function App is configured in the application.

### Azure Function URL

- The Azure Function App URL is: `https://terafunctionapppyhton.azurewebsites.net`
