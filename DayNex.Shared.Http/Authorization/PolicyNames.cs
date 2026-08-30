namespace DayNex.Shared.Http.Authorization
{
    public static class PolicyNames
    {
        public const string RequireAdmin = "RequireAdmin";
        public const string RequireSuperAdmin = "RequireSuperAdmin";
        public const string RequirePremium = "RequirePremium";
    }

    /// <summary>Claim type constants used in the JWT issued by Entra External ID / IdentityService.</summary>
    public static class DayNexClaimTypes
    {
        public const string Role = "role";
        public const string SubscriptionTier = "subscription_tier";
        public const string SubscriptionStatus = "subscription_status";
        public const string UserId = "sub";
    }

}
