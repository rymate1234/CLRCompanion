namespace CLRCompanion.Data
{
    public class UserPreferences
    {
        public ulong Id { get; set; }
        public UserMessagePreference UserMessagePreference { get; set; }
    }

    public enum UserMessagePreference 
    {
        None = 0,
        All = 1,
        Mentions = 2,
    }
}
    