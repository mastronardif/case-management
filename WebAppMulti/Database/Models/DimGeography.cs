using System;
using System.Collections.Generic;
using WebAppMulti.Database.Models;

namespace WebAppMulti.Database.Models;

public partial class DimGeography
{
    public int GeographyKey { get; set; }

    public string? City { get; set; }

    public string? StateProvinceCode { get; set; }

    public string? StateProvinceName { get; set; }

    public string? CountryRegionCode { get; set; }

    public string? EnglishCountryRegionName { get; set; }

    public string? SpanishCountryRegionName { get; set; }

    public string? FrenchCountryRegionName { get; set; }

    public string? PostalCode { get; set; }

    public int? SalesTerritoryKey { get; set; }

    public string? IpAddressLocator { get; set; }

    public virtual ICollection<DimCustomer> DimCustomers { get; set; } = new List<DimCustomer>();

    //public virtual ICollection<DimReseller> DimResellers { get; set; } = new List<DimReseller>();

    //public virtual DimSalesTerritory? SalesTerritoryKeyNavigation { get; set; }
}
