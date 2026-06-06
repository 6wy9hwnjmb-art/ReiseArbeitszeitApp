namespace ReiseArbeitszeitApp.Models;

public class YearReport
{
    public int Year { get; set; }
    public int WorkDayCount { get; set; }
    public int TravelDayCount { get; set; }
    public TimeSpan TotalActualWorkTime { get; set; }
    public TimeSpan TotalTargetWorkTime { get; set; }
    public TimeSpan TotalOvertime => TotalActualWorkTime - TotalTargetWorkTime;
    public TimeSpan TotalTravelWorkTime { get; set; }
}
