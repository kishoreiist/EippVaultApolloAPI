namespace EVWebApi.Helpers
{
    public class PreviousWeekDateHelper
    {
        public static (DateTime start, DateTime end) GetPreviousWeekRange()
        {
            var today = DateTime.Today;

            // This week's Monday
            int diffToMonday = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var thisWeeksMonday = today.AddDays(-diffToMonday);

            // Previous week's Monday
            var lastWeeksMonday = thisWeeksMonday.AddDays(-7);

            // Previous week's Sunday = Monday + 6 days
            var lastWeeksSunday = lastWeeksMonday.AddDays(6);

            return (lastWeeksMonday, lastWeeksSunday);
        }

    }
}
