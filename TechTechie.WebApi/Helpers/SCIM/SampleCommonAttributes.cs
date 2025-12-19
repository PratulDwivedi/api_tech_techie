namespace TechTechie.WebApi.Helpers.SCIM
{
    public static class SampleCommonAttributes
    {
        public static AttributeScheme IdentiFierAttributeScheme
        {
            get
            {
                AttributeScheme idScheme = new AttributeScheme("id", AttributeDataType.@string, false)
                {
                    Description = SampleConstants.DescriptionIdentifier
                };
                return idScheme;
            }
        }
    }
}
