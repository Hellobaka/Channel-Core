namespace Channel_Core
{
    public static class Config
    {
        public static string AppID
        {
            get => ConfigHelper.ReadConfig("AppID").ToString();
            set => ConfigHelper.WriteConfig("AppID", value);
        }
        public static string AppKey
        {
            get => ConfigHelper.ReadConfig("AppKey").ToString();
            set => ConfigHelper.WriteConfig("AppKey", value);
        }
        public static string Token
        {
            get => ConfigHelper.ReadConfig("Token").ToString();
            set => ConfigHelper.WriteConfig("Token", value);
        }
    }
}
