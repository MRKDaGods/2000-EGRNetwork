namespace MRK.System
{
    public class Time
    {
        private static readonly DateTime _startTime;

        static Time()
        {
            _startTime = DateTime.Now;
        }

        public static TimeSpan Relative
        {
            get { return DateTime.Now - _startTime; }
        }

        public static float RelativeTimeSeconds
        {
            get { return (float)Relative.TotalSeconds; }
        }
    }
}
