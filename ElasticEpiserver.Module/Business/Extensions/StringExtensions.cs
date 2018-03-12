namespace ElasticEpiserver.Module.Business.Extensions
{
    public static class StringExtensions
    {
        public static string ToElasticPropertyName(this string propertyName)
        {
            // Example: SchemaCode will become schemaCode
            if (!string.IsNullOrWhiteSpace(propertyName) && propertyName.Length > 1)
            {
                return char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
            }

            return propertyName;
        }
    }
}