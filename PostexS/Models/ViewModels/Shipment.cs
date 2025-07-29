using PostexS.Models.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PostexS.Models.ViewModels
{
    public class Shipment
    {
        public string AWB { get; set; }
        public string ToConsigneeName { get; set; }
        public string ToAddress { get; set; }
        public string ToPhone { get; set; }
        public decimal COD { get; set; }
        public decimal CollectedValue { get; set; }
        public string StatusNameA { get; set; }
        public string StatusNameE { get; set; }
        public DateTime TransDate { get; set; }
        [JsonConverter(typeof(FlexibleDateTimeConverter))]
        public DateTime? DeliveryDate { get; set; }
        public string SpecialInstuctions { get; set; }
        public string CurrentBranch { get; set; }
        public decimal ShipmentFees { get; set; }
        public string RunnerName { get; set; }
        public string RunnerMobile { get; set; }
    }
    public class ApiResponse
    {
        public List<Shipment> Shipments { get; set; }
    }
    public class FlexibleDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                if (reader.TokenType == JsonTokenType.Null)
                    return null;

                if (reader.TokenType == JsonTokenType.String)
                {
                    var dateString = reader.GetString();

                    if (string.IsNullOrWhiteSpace(dateString))
                        return null;

                    if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                        return date;

                    if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                        return date;
                }

                return reader.GetDateTime();
            }
            catch
            {
                return null;
            }
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value?.ToString("yyyy-MM-dd"));
        }
    }
}
