/**
 * @namespace SmartFleet
 */
var SmartFleet = window.SmartFleet || {};
SmartFleet.VehicleForm = SmartFleet.VehicleForm || {};

(function (namespace) {
    "use strict";

    // Constants
    var STATUS_IN_REPAIR = 861230001;
    var NOTIFICATION_ID = "cr_vehicle_in_repair_warning";
    var FIELD_STATUS = "cr_status";
    var FIELD_DRIVER = "cr_driverassignedid";

    /**
     * Handles the Form OnLoad event.
     * @param {ExecutionContext} executionContext - The execution context from the Model-Driven Form.
     */
    namespace.onLoad = function (executionContext) {
        if (!executionContext) {
            console.error("SmartFleet.VehicleForm.onLoad: Execution context is null or undefined.");
            return;
        }

        var formContext = executionContext.getFormContext();
        
        // Register the OnChange event handler for the status field
        var statusAttribute = formContext.getAttribute(FIELD_STATUS);
        if (statusAttribute) {
            statusAttribute.addOnChange(namespace.onStatusChange);
        } else {
            console.warn("SmartFleet.VehicleForm.onLoad: Field '" + FIELD_STATUS + "' not found on the form.");
        }

        // Run logic once on load to establish correct initial form state
        namespace.handleStatusChange(formContext);
    };

    /**
     * Handles the Status Field OnChange event.
     * @param {ExecutionContext} executionContext - The execution context from the Model-Driven Form.
     */
    namespace.onStatusChange = function (executionContext) {
        if (!executionContext) {
            console.error("SmartFleet.VehicleForm.onStatusChange: Execution context is null or undefined.");
            return;
        }

        var formContext = executionContext.getFormContext();
        namespace.handleStatusChange(formContext);
    };

    /**
     * Core business logic to update UI state based on Vehicle Status.
     * @param {FormContext} formContext - The form context from the client API.
     */
    namespace.handleStatusChange = function (formContext) {
        if (!formContext) {
            return;
        }

        var statusAttribute = formContext.getAttribute(FIELD_STATUS);
        var driverControl = formContext.getControl(FIELD_DRIVER);
        var driverAttribute = formContext.getAttribute(FIELD_DRIVER);

        if (!statusAttribute) {
            return;
        }

        var statusValue = statusAttribute.getValue();

        if (statusValue === STATUS_IN_REPAIR) {
            // 1. Clear the assigned driver field first to avoid invalid associations
            if (driverAttribute) {
                driverAttribute.setValue(null);
            }

            // 2. Programmatically disable/lock the assigned driver field
            if (driverControl) {
                driverControl.setDisabled(true);
            }

            // 3. Display an on-screen warning notification banner
            formContext.ui.setFormNotification(
                "This vehicle is currently In Repair. You cannot assign a driver until maintenance is complete.",
                "WARNING",
                NOTIFICATION_ID
            );
        } else {
            // 1. Enable/unlock the assigned driver field
            if (driverControl) {
                driverControl.setDisabled(false);
            }

            // 2. Clear the warning notification banner
            formContext.ui.clearFormNotification(NOTIFICATION_ID);
        }
    };

})(SmartFleet.VehicleForm);
