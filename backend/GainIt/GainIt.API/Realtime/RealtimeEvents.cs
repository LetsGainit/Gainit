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
        public static class Tasks
        {
            public const string TaskCreated = "taskCreated";
            public const string TaskUnblocked = "taskUnblocked";
            public const string TaskCompleted = "taskCompleted";
            public const string MilestoneCompleted = "milestoneCompleted";
        }
        
        public static class Forum
        {
            public const string PostReplied = "postReplied";
            public const string PostLiked = "postLiked";
            public const string ReplyLiked = "replyLiked";
        }
    }
}
