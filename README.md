## **1. Software Description**

This is an **Automatic License Plate Recognition (ALPR)** system that:

  - Captures video from **IP cameras** (via RTSP).
  - Processes frames in real-time using the **[DTK LPR library](https://www.dtksoft.com/lprsdk)**.
  - Stores data in a **database** and images in **Azure Blob Storage**.
  - Operates in **multithread**, allowing simultaneous processing of multiple cameras.


> PS. Why use DTK Software and not Rekor, Plate Recnogize, etc.? Cost!
> It has quality equivalent to all the others at a lower cost. However,
> not everything is perfect, it is not as fast or optimized for GPU,
> Docker, etc.


**Key features:**
✔ Real-time plate recognition
✔ Support for multiple cameras
✔ Persistence in database and cloud
✔ Asynchronous queue for batch processing

-----

## **2. How to Run the Project**

### **Prerequisites**

  - **.NET 9** (or compatible version)
  - **DTK LPR License** (required to use the recognition library)
  - **IP cameras** configured with RTSP access

### **Initial Setup**

1.  **Modify `DataService.cs`** to include the camera URLs:

    ```csharp
    public static List<string> FuncaoRetornaListadeUrls()
    {
        return new List<string>
        {
            "rtsp://admin:senha@192.168.1.100/Streaming/Channels/101/",
            "rtsp://admin:senha@192.168.1.101/Streaming/Channels/101/"
            // Add more cameras as needed
        };
    }
    ```

    *(In the future, this should be replaced by a database query.)*

2.  **Configure the DTK LPR license** (refer to the official documentation).

3.  **Run the project:**

    ```bash
    dotnet run
    ```

-----

## **3. Project Architecture**

### **Main Components**

| Component             | Function                                                    |
| --------------------- | ----------------------------------------------------------- |
| **`CameraProcessor`** | Processes the stream from each camera and detects plates    |
| **`PlateQueueService`** | Thread-safe queue (`ConcurrentQueue`) for detected plates |
| **`PersistenceWorker`** | Processes the queue in batches and persists to DB + Azure |
| **`DataService`** | Simulates database insertion                                |
| **`AzureService`** | Uploads images to Azure Blob Storage                      |

### **Data Flow**

```mermaid
sequenceDiagram
    CameraProcessor->>PlateQueueService: Enqueues detected plate
    PlateQueueService->>PersistenceWorker: Processes in batches
    PersistenceWorker->>DataService: Inserts into database
    PersistenceWorker->>AzureService: Uploads image (plate + full scene)
```

### **Threading and Concurrency**

  - **Each camera runs in a separate thread** (via `Task`).
  - **`PersistenceWorker` processes the queue in batches** (avoids DB overload).
  - **`ConcurrentQueue` ensures thread safety**.

### **Dependency Injection (DI) and Clean Architecture**

  - The project **does not use DI natively**, but it is structured to facilitate migration.
  - Clear separation between:
      - **Capture Layer** (`CameraProcessor`)
      - **Processing Layer** (`PlateQueueService` + `PersistenceWorker`)
      - **Persistence Layer** (`DataService` + `AzureService`)

-----

## **4. Fixes in the DTK LPR Library**

Modifications were made to the original library to improve security and performance:

### **Replacement of `CopyMemory` with `MoveMemory`**

In the `DTKLPR5.cs` file:

```csharp
[Obsolete("Use MoveMemory for better compatibility")] 
[DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
public static extern void CopyMemory(IntPtr dest, IntPtr src, int count);

// New recommended implementation:
[DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory", SetLastError = false)]
internal static extern void MoveMemory(IntPtr dest, IntPtr src, IntPtr byteCount); // Suporte a 64-bit
```

### **Updated `CreateBitmapFromBuffer` Method**

Now uses `MoveMemory` to avoid issues on 64-bit systems.

-----

## **Next Steps (Roadmap)**

  - [ ] Migrate camera configuration to **database**.
  - [ ] Implement **structured logging** (Serilog + Seq).
  - [ ] Add **monitoring** (Prometheus + Grafana).
  - [ ] Add **Docker** support for simplified deployment.

## **Branchs**
there are 2 branches
  - simple_example - simpler branch with the basics to work. (but still a decoupled project)
  - stage - the best of the best

-----

## **Contribution**

If you want to contribute, open a **PR** or report **issues** in the repository.

-----

**License**: MIT.

📌 **Note**: This project is still under development. Test locally before using in production.

🔧 **Happy Coding\!** 🚗📸
