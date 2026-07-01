# Smart Fleet Management Hub - Extensions Repository

This repository hosts pro-developer extensions for the **Smart Fleet Management Hub**, showcasing custom frontend and backend logic integrated with Microsoft Dynamics 365 and Power Platform (Dataverse).

---

## Directory Structure

```
├── README.md
├── .gitignore
├── src/
│   ├── client/
│   │   └── WebResources/
│   │       └── js/
│   │           └── cr_vehicle_form.js       # Frontend Web Resource (form-scripting)
│   └── server/
│       └── Plugins/
│           ├── SmartFleetPlugins.csproj      # .NET Framework 4.6.2 Project definition
│           └── PostAssetLogCreate.cs         # Backend assembly implementing IPlugin
```

---

## Features

### 1. Frontend: Vehicle Form Logic ([cr_vehicle_form.js](file:///Users/aibrahim/Documents/GitHub/Smart%20Fleet%20and%20Asset%20Management/src/client/WebResources/js/cr_vehicle_form.js))
Targeted at the Model-Driven Form for **Vehicle** (`cr_vehicle`).
* **OnLoad & OnChange Handlers**: Dynamically listens to updates on the Status (`cr_status`) field.
* **Conditional Enforcement**:
  * If a vehicle's status is set to **In Repair** (`861230001`):
    * Disables the **Assigned Driver** (`cr_driverassignedid`) field.
    * Programmatically clears the field value to avoid assigning a driver to a vehicle currently undergoing repairs.
    * Displays a warning banner at the top of the form using `formContext.ui.setFormNotification`.
  * If the status is any other value:
    * Unlocks the **Assigned Driver** field.
    * Clears the repair warning banner.

### 2. Backend: Mileage Aggregation & Maintenance Alert ([PostAssetLogCreate.cs](file:///Users/aibrahim/Documents/GitHub/Smart%20Fleet%20and%20Asset%20Management/src/server/Plugins/PostAssetLogCreate.cs))
Targeted at the **Asset Log** (`cr_assetlog`) table on **Post-Operation Create** (Stage 40).
* **Automated Aggregation**:
  * Extracts the newly logged trip mileage (`cr_mileageincurred`) and references the parent vehicle (`cr_vehicleid`).
  * Queries the parent vehicle's current total mileage from Dataverse.
  * Calculates the new total and updates the parent record.
* **Maintenance Threshold**:
  * Checks if the new total mileage crosses the **5,000-mile** threshold.
  * Toggles the vehicle's "Needs Maintenance" (`cr_needs_maintenance`) flag to **True** if the threshold is met or exceeded.

---

## Prerequisites
* **Power Apps Environment** with custom entities enabled.
* **Microsoft.Xrm.Tooling.PluginRegistration** (Plugin Registration Tool) to deploy assemblies.
* **.NET SDK** installed locally. Because this targets `.NET Framework 4.6.2` (via Mono reference assemblies on macOS/Linux), a local `dotnet build` will successfully restore and compile dependencies using the modern SDK-style project layout.

---

## Dataverse Schema Configuration

Ensure the following tables and fields are created in your Dataverse solution before deploying the code.

### 1. Vehicle Table (`cr_vehicle`)
* **Display Name**: Vehicle
* **Plural Name**: Vehicles
* **Columns**:
  | Display Name | Schema Name | Data Type | Additional Settings / Values |
  | :--- | :--- | :--- | :--- |
  | **Vehicle Name** | `cr_name` | Single line of text | *Primary Column (Required)* |
  | **Status** | `cr_status` | Choice | Value: `861230000` (Active)<br>Value: `861230001` (In Repair) |
  | **Assigned Driver** | `cr_driverassignedid` | Lookup | Target Entity: `systemuser` (User) |
  | **Total Mileage** | `cr_totalmileage` | Whole Number | Minimum: 0, Default: 0 |
  | **Needs Maintenance** | `cr_needs_maintenance` | Yes/No (Boolean) | Default: No (`false`) |

### 2. Asset Log Table (`cr_assetlog`)
* **Display Name**: Asset Log
* **Plural Name**: Asset Logs
* **Columns**:
  | Display Name | Schema Name | Data Type | Additional Settings |
  | :--- | :--- | :--- | :--- |
  | **Log Name** | `cr_name` | Single line of text | *Primary Column (Required)* |
  | **Vehicle** | `cr_vehicleid` | Lookup | *Required*. Target Entity: `cr_vehicle` |
  | **Mileage Incurred** | `cr_mileageincurred` | Whole Number | Minimum: 0, *Required* |

---

## Frontend Web Resource Setup & Configuration

Follow these steps to deploy and configure the client-side scripting on your Vehicle form:

### Step 1: Upload the Web Resource
1. Sign in to the [Power Apps Maker Portal](https://make.powerapps.com/).
2. Select your development **Solution**.
3. Click **New** -> **More** -> **Web Resource**.
4. Configure the Web Resource details:
   - **Display Name**: Vehicle Form Script
   - **Name**: `cr_vehicle_form.js`
   - **Type**: Script (JavaScript)
   - **File**: Upload the [cr_vehicle_form.js](file:///Users/aibrahim/Documents/GitHub/Smart%20Fleet%20and%20Asset%20Management/src/client/WebResources/js/cr_vehicle_form.js) file from this repository.
5. Click **Save** and then click **Publish**.

### Step 2: Configure Form Event Handlers
1. In your solution, open the **Vehicle** table and edit the **Main** form.
2. In the Form properties panel on the right, locate the **Events** tab.
3. Under **Form Libraries**, click **+ Add Library** and select the Web Resource `cr_vehicle_form.js` you just published.
4. Under **On Load**, click **+ Event Handler** and configure the listener:
   - **Event Type**: On Load
   - **Library**: `cr_vehicle_form.js`
   - **Function**: `SmartFleet.VehicleForm.onLoad`
   - **Pass execution context as first parameter**: **Checked** (Mandatory)
5. Save and Publish the Form.
   > [!NOTE]
   > You **do not** need to register a separate `OnChange` handler in the UI for the Status field. The script automatically attaches the `OnChange` listener programmatically during form load via the `statusAttribute.addOnChange` API.

---

## Backend Plugin Setup & Configuration

Follow these steps to build and register the backend assembly to handle automatic mileage aggregation:

### Step 1: Build the Assembly
Open a terminal in the project directory and build the assembly:
```bash
dotnet restore src/server/Plugins/SmartFleetPlugins.csproj
dotnet build src/server/Plugins/SmartFleetPlugins.csproj -c Release
```
This generates the assembly output at:
`src/server/Plugins/bin/Release/net462/SmartFleetPlugins.dll`

### Step 2: Register Assembly using Plugin Registration Tool (PRT)
1. Open the **Plugin Registration Tool** and click **Create New Connection** to authenticate with your Dataverse environment.
2. Click **Register** -> **Register New Assembly**.
3. Browse to the output file `SmartFleetPlugins.dll` generated in Step 1.
4. Ensure **Sandbox** isolation mode is selected, and choose **Database** for the storage location.
5. Click **Register Selected Plugins**.

### Step 3: Register the Plugin Step
1. In the PRT treeview, expand the registered assembly to locate `SmartFleetPlugins.PostAssetLogCreate`.
2. Right-click the class name and select **Register New Step**.
3. Configure the Step parameters:
   - **Message**: `Create`
   - **Primary Entity**: `cr_assetlog`
   - **Event Pipeline Stage of Execution**: `PostOperation`
   - **Execution Mode**: `Synchronous` (Or `Asynchronous` for background computation)
   - **Deployment**: `Server`
4. Click **Register New Step**.

---

## Testing & Verifying the Application

### 1. Verifying the Form Logic (Frontend)
1. Navigate to your Model-Driven App containing the **Vehicles** table.
2. Create a new Vehicle record or open an existing one.
3. Look at the **Assigned Driver** field; it should be editable.
4. Change the **Status** field to **In Repair**.
5. **Observe**:
   - The **Assigned Driver** field instantly locks (becomes read-only).
   - Any value in the **Assigned Driver** field is automatically cleared.
   - A yellow warning banner appears at the top of the form stating: *"This vehicle is currently In Repair. You cannot assign a driver..."*
6. Change the **Status** field back to **Active** (or another option).
7. **Observe**:
   - The **Assigned Driver** field becomes unlocked/editable again.
   - The warning banner vanishes.

### 2. Verifying the Mileage Aggregation Plugin (Backend)
1. Create a Vehicle record named `"Truck 1"` with **Total Mileage** initialized to `1000`. Keep **Needs Maintenance** set to `No`.
2. Go to **Asset Logs** and create a new record:
   - **Log Name**: `"Trip 101"`
   - **Vehicle**: Select `"Truck 1"`
   - **Mileage Incurred**: `2500`
3. Save the Asset Log.
4. Navigate back to `"Truck 1"`. Refresh the page if needed.
5. **Observe**: The **Total Mileage** should now read `3500` (1000 base + 2500 incurred). The **Needs Maintenance** flag should still be `No` (since total mileage is under 5000).
6. Create a second Asset Log record:
   - **Log Name**: `"Trip 102"`
   - **Vehicle**: Select `"Truck 1"`
   - **Mileage Incurred**: `1600`
7. Save the Asset Log.
8. Navigate back to `"Truck 1"`.
9. **Observe**: The **Total Mileage** should now read `5100` (3500 base + 1600 incurred). Because the aggregated value crosses the 5,000-mile threshold, the **Needs Maintenance** field should be toggled to **Yes** (`true`).

---

## Troubleshooting & Debugging

### Debugging the Frontend Script
1. Press `F12` inside the browser to open Developer Tools.
2. Navigate to the **Sources** tab, find `cr_vehicle_form.js` under the Page assets or via search (`Cmd + P` or `Ctrl + P`).
3. Set breakpoints inside `onLoad`, `onStatusChange`, and `handleStatusChange` to track execution flow.
4. View console outputs. If any errors occur, they will be logged via `console.error` and `console.warn` statements built into the handlers.

### Debugging the Backend Plugin
1. Install the **Plugin Profiler** in the Plugin Registration Tool.
2. Select the `PostAssetLogCreate` step and click **Profile** to enable debugging capture.
3. Trigger the plugin by creating a new Asset Log record. Download the generated profile file when prompted by Dataverse.
4. Attach your IDE debugger to the PRT process, choose the Profile file, and debug the execution line-by-line using your local source code.
5. Review the execution logs in the Dataverse **Plugin Trace Log** table to inspect tracing output generated via `ITracingService.Trace()`.
