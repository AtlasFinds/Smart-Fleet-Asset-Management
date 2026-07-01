using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace SmartFleetPlugins
{
    /// <summary>
    /// Plugin to aggregate mileage on the parent Vehicle record when a new Asset Log is created.
    /// Triggers on Post-Operation Create of cr_assetlog.
    /// </summary>
    public class PostAssetLogCreate : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            // Obtain execution context and services
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Verify stage is Post-Operation (40) and message is Create
            if (context.Stage != 40 || !string.Equals(context.MessageName, "Create", StringComparison.OrdinalIgnoreCase))
            {
                tracingService.Trace("PostAssetLogCreate: Invalid execution stage or message. Expected Stage 40 (Post-Operation) and Message 'Create'.");
                return;
            }

            // Verify Target is present and is Entity
            if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity))
            {
                tracingService.Trace("PostAssetLogCreate: Target input parameter is missing or is not an Entity instance.");
                return;
            }

            Entity targetEntity = (Entity)context.InputParameters["Target"];

            // Verify Target logical name is cr_assetlog
            if (!string.Equals(targetEntity.LogicalName, "cr_assetlog", StringComparison.OrdinalIgnoreCase))
            {
                tracingService.Trace($"PostAssetLogCreate: Target entity '{targetEntity.LogicalName}' is not 'cr_assetlog'. Exiting.");
                return;
            }

            tracingService.Trace("PostAssetLogCreate: Plugin execution started.");

            // 1. Retrieve trip mileage ('cr_mileageincurred')
            int mileageIncurred = 0;
            if (targetEntity.Contains("cr_mileageincurred"))
            {
                mileageIncurred = targetEntity.GetAttributeValue<int>("cr_mileageincurred");
                tracingService.Trace($"PostAssetLogCreate: Extracted mileage incurred: {mileageIncurred}.");
            }
            else
            {
                tracingService.Trace("PostAssetLogCreate: Target entity does not contain 'cr_mileageincurred'. Defaulting to 0.");
            }

            // 2. Look up parent vehicle reference ('cr_vehicleid')
            if (!targetEntity.Contains("cr_vehicleid") || !(targetEntity["cr_vehicleid"] is EntityReference))
            {
                tracingService.Trace("PostAssetLogCreate: 'cr_vehicleid' is missing or not a valid lookup. Cannot update parent vehicle.");
                return;
            }

            EntityReference vehicleRef = targetEntity.GetAttributeValue<EntityReference>("cr_vehicleid");
            tracingService.Trace($"PostAssetLogCreate: Parent vehicle entity: {vehicleRef.LogicalName}, ID: {vehicleRef.Id}.");

            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                // 3. Query the parent vehicle's current total mileage and maintenance flag
                ColumnSet vehicleColumns = new ColumnSet("cr_totalmileage", "cr_needs_maintenance");
                Entity vehicle = service.Retrieve(vehicleRef.LogicalName, vehicleRef.Id, vehicleColumns);

                int currentTotalMileage = 0;
                if (vehicle.Contains("cr_totalmileage"))
                {
                    currentTotalMileage = vehicle.GetAttributeValue<int>("cr_totalmileage");
                }
                tracingService.Trace($"PostAssetLogCreate: Parent vehicle current total mileage: {currentTotalMileage}.");

                // 4. Aggregate the new total mileage
                int newTotalMileage = currentTotalMileage + mileageIncurred;
                tracingService.Trace($"PostAssetLogCreate: Aggregated new total mileage: {newTotalMileage}.");

                // Prepare vehicle update entity
                Entity vehicleToUpdate = new Entity(vehicleRef.LogicalName, vehicleRef.Id);
                vehicleToUpdate["cr_totalmileage"] = newTotalMileage;

                // 5. Check if it crosses the 5000-mile threshold to toggle cr_needs_maintenance to true
                if (newTotalMileage >= 5000)
                {
                    bool currentMaintenanceFlag = vehicle.Contains("cr_needs_maintenance") && vehicle.GetAttributeValue<bool>("cr_needs_maintenance");
                    if (!currentMaintenanceFlag)
                    {
                        vehicleToUpdate["cr_needs_maintenance"] = true;
                        tracingService.Trace("PostAssetLogCreate: New mileage crosses 5000-mile threshold. Flagging vehicle for maintenance.");
                    }
                }

                // 6. Update parent vehicle record
                service.Update(vehicleToUpdate);
                tracingService.Trace("PostAssetLogCreate: Successfully updated parent vehicle record.");
            }
            catch (Exception ex)
            {
                tracingService.Trace($"PostAssetLogCreate: Exception encountered: {ex.Message}");
                throw new InvalidPluginExecutionException($"An error occurred in PostAssetLogCreate: {ex.Message}", ex);
            }
        }
    }
}
