namespace EVWebApi.Enums
{
    public class HROnboardingConfirmationTypes
    {
    }

    public static class HrConfirmationStatuses
    {
        public const string Pending = "pending";
        public const string Confirmed = "confirmed";
        public const string Processed = "processed";
        public const string Failed = "failed";
    }

    public static class HrConfirmationRowStatuses
    {
        public const string Matched = "matched";
        public const string CandidateNotFound = "candidate_not_found";
        public const string DuplicateMatch = "duplicate_match";
        public const string ValidationFailed = "validation_failed";
        public const string AlreadyConfirmed = "already_confirmed";
        public const string Converted = "converted";
    }
}
