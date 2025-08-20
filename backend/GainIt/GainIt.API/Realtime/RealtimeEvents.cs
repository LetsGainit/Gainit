namespace GainIt.API.Realtime
{
    public static class RealtimeEvents
    {
        public static class Projects
        {
            public const string JoinRequested = "projectJoinRequested";
            public const string JoinApproved = "projectJoinApproved";
            public const string JoinRejected = "projectJoinRejected";
            public const string JoinCancelled = "projectJoinCancelled";
        }
    }
}
