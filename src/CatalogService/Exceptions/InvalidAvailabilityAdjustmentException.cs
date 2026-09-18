namespace CatalogService.Exceptions;

public class InvalidAvailabilityAdjustmentException()
    : Exception("Adjustment would push available copies outside the valid range");
