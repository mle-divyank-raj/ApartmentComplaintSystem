using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ACLS.Persistence.Converters;

/// <summary>
/// EF Core value converter for List{string} ↔ nvarchar JSON string.
/// Used for StaffMember.Skills and Complaint.RequiredSkills.
/// Stored format: JSON array e.g. ["Plumbing","Electrical","HVAC"]
/// Null DB value maps to an empty list — never null in the domain model.
/// </summary>
public sealed class JsonStringListConverter : ValueConverter<List<string>, string>
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public JsonStringListConverter()
        : base(
            list => JsonSerializer.Serialize(list, JsonOptions),
            json => string.IsNullOrWhiteSpace(json)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>())
    {
    }
}
