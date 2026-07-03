namespace Mkey.NakamaPoc
{
    /// <summary>
    /// Isolated Nakama POC settings — not wired to production ApiConfig.
    /// </summary>
    public static class NakamaPocSettings
    {
        public const string Tag = "[NakamaPoc]";

        public const string ServerHost = "127.0.0.1";
        public const int ServerPort = 7350;
        public const string ServerKey = "defaultkey";
        public const bool UseSsl = false;

        public const int OpcodeHello = 1;
        public const int OpcodePlayerJoined = 2;
        public const int OpcodePlayerLeft = 3;

        public const int MatchmakerMinPlayers = 2;
        public const int MatchmakerMaxPlayers = 2;

        public const float StepTimeoutSeconds = 45f;
    }
}
