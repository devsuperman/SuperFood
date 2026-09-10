namespace SuperFood.Contracts.Restaurants;

public record OperatingHourDto(DayOfWeek DayOfWeek, TimeOnly OpenTime, TimeOnly CloseTime, bool IsClosed);

public record UpdateOperatingHoursRequest(List<OperatingHourDto> Hours);
