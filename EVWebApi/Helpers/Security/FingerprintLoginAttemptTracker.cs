using System.Collections.Concurrent;

namespace EVWebApi.Helpers.Security
{
    public static class FingerprintLoginAttemptTracker
    {
        private static readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _attempts = new();

        private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

        private const int MaxAttempts = 15;
        private const int CaptchaThreshold = 7;

        public static void RegisterAttempt(string fingerprint)
        {
            var queue = _attempts.GetOrAdd(fingerprint, _ => new ConcurrentQueue<DateTime>());
            queue.Enqueue(DateTime.UtcNow);
            Cleanup(fingerprint);
        }

        public static bool IsCaptchaRequired(string fingerprint)
        {
            Cleanup(fingerprint);
            return _attempts.TryGetValue(fingerprint, out var q) && q.Count >= CaptchaThreshold;
        }

        public static bool IsLimitExceeded(string fingerprint)
        {
            Cleanup(fingerprint);
            return _attempts.TryGetValue(fingerprint, out var q) && q.Count > MaxAttempts;
        }
        public static void Reset(string fingerprint)
        {
            _attempts.TryRemove(fingerprint, out _);
        }
        private static void Cleanup(string fingerprint)
        {
            if (!_attempts.TryGetValue(fingerprint, out var queue))
                return;

            var now = DateTime.UtcNow;

            while (queue.TryPeek(out var time) && now - time > Window)
            {
                queue.TryDequeue(out _);
            }
        }
    }
}
