namespace WebAppMulti.Database.Dtos
{
    public class CustomerWithGeoDto
    {
        public int CustomerKey { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? EmailAddress { get; set; }
        public string? Phone { get; set; }

        // Geography info
        public string? City { get; set; }
        public string? StateProvinceCode { get; set; }
        public string? CountryRegionCode { get; set; }
    }

}
