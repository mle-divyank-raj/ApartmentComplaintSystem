namespace ACLS.SharedKernel;

/// <summary>
/// Precondition guard clauses used in domain entity factory methods and value objects.
/// All methods throw immediately on violation — fail loudly, never silently.
/// </summary>
public static class Guard
{
    public static class Against
    {
        /// <summary>Throws ArgumentNullException if value is null.</summary>
        public static T Null<T>(T? value, string parameterName) where T : class
        {
            if (value is null)
                throw new ArgumentNullException(parameterName, $"{parameterName} must not be null.");
            return value;
        }

        /// <summary>Throws ArgumentException if value is null, empty, or whitespace.</summary>
        public static string NullOrWhiteSpace(string? value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(
                    $"{parameterName} must not be null or whitespace.", parameterName);
            return value;
        }

        /// <summary>Throws ArgumentOutOfRangeException if value is zero or negative.</summary>
        public static int NegativeOrZero(int value, string parameterName)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(
                    parameterName, value,
                    $"{parameterName} must be a positive integer greater than zero.");
            return value;
        }

        /// <summary>Throws ArgumentOutOfRangeException if value is negative.</summary>
        public static int Negative(int value, string parameterName)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(
                    parameterName, value,
                    $"{parameterName} must not be negative.");
            return value;
        }

        /// <summary>Throws ArgumentException if value exceeds the specified maximum length.</summary>
        public static string ExceedsMaxLength(string? value, int maxLength, string parameterName)
        {
            if ((value?.Length ?? 0) > maxLength)
                throw new ArgumentException(
                    $"{parameterName} must not exceed {maxLength} characters.",
                    parameterName);
            return value ?? string.Empty;
        }

        /// <summary>Throws ArgumentOutOfRangeException if value is outside the inclusive range [min, max].</summary>
        public static int OutOfRange(int value, int min, int max, string parameterName)
        {
            if (value < min || value > max)
                throw new ArgumentOutOfRangeException(
                    parameterName, value,
                    $"{parameterName} must be between {min} and {max} inclusive.");
            return value;
        }

        /// <summary>Throws ArgumentException if the DateTime is not UTC.</summary>
        public static DateTime NotUtc(DateTime value, string parameterName)
        {
            if (value.Kind != DateTimeKind.Utc)
                throw new ArgumentException(
                    $"{parameterName} must be a UTC DateTime. Use DateTime.UtcNow, not DateTime.Now.",
                    parameterName);
            return value;
        }
    }
}
