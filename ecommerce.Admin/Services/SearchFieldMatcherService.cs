using System;
using System.Collections.Generic;
using System.Linq;
using ecommerce.Domain.Shared.Dtos.Product;

namespace ecommerce.Admin.Services
{
    /// <summary>
    /// Service for detecting which Elasticsearch fields matched a search term
    /// Used by ML system to learn which field types lead to conversions
    /// </summary>
    public interface ISearchFieldMatcherService
    {
        FieldMatchResult DetectMatchedFields(string searchTerm, SellerProductViewModel product);
    }

    public class SearchFieldMatcherService : ISearchFieldMatcherService
    {
        public FieldMatchResult DetectMatchedFields(string searchTerm, SellerProductViewModel product)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || product == null)
            {
                return new FieldMatchResult();
            }

            var matchedFields = new List<string>();
            var fieldScores = new Dictionary<string, double>();
            var searchLower = searchTerm.Trim().ToLowerInvariant();

            // Check OemCode match
            if (product.OemCode != null && product.OemCode.Any(oem =>
                oem?.ToLowerInvariant().Contains(searchLower) == true))
            {
                matchedFields.Add("OemCode");
                var exactMatch = product.OemCode.Any(oem =>
                    string.Equals(oem, searchTerm, StringComparison.OrdinalIgnoreCase));
                fieldScores["OemCode"] = exactMatch ? 100.0 : 50.0;
            }

            // Check PartNumber match
            if (!string.IsNullOrEmpty(product.PartNumber) &&
                product.PartNumber.ToLowerInvariant().Contains(searchLower))
            {
                matchedFields.Add("PartNumber");
                var exactMatch = string.Equals(product.PartNumber, searchTerm,
                    StringComparison.OrdinalIgnoreCase);
                fieldScores["PartNumber"] = exactMatch ? 100.0 : 50.0;
            }

            // Check ProductName match
            if (!string.IsNullOrEmpty(product.ProductName) &&
                product.ProductName.ToLowerInvariant().Contains(searchLower))
            {
                matchedFields.Add("ProductName");
                var exactMatch = string.Equals(product.ProductName, searchTerm,
                    StringComparison.OrdinalIgnoreCase);
                fieldScores["ProductName"] = exactMatch ? 100.0 : 30.0;
            }

            // Check ManufacturerName match
            if (!string.IsNullOrEmpty(product.ManufacturerName) &&
                product.ManufacturerName.ToLowerInvariant().Contains(searchLower))
            {
                matchedFields.Add("ManufacturerName");
                fieldScores["ManufacturerName"] = 20.0;
            }

            // Check Brand name match
            if (product.Brand != null && !string.IsNullOrEmpty(product.Brand.Name) &&
                product.Brand.Name.ToLowerInvariant().Contains(searchLower))
            {
                matchedFields.Add("BrandName");
                fieldScores["BrandName"] = 15.0;
            }

            // Check DotPartName match
            if (!string.IsNullOrEmpty(product.DotPartName) &&
                product.DotPartName.ToLowerInvariant().Contains(searchLower))
            {
                matchedFields.Add("DotPartName");
                fieldScores["DotPartName"] = 25.0;
            }

            // Check BaseModelName match
            if (!string.IsNullOrEmpty(product.BaseModelName) &&
                product.BaseModelName.ToLowerInvariant().Contains(searchLower))
            {
                matchedFields.Add("BaseModelName");
                fieldScores["BaseModelName"] = 18.0;
            }

            // Determine primary match field (highest score)
            var primaryField = fieldScores.OrderByDescending(x => x.Value)
                                         .FirstOrDefault().Key;

            return new FieldMatchResult
            {
                MatchedFields = matchedFields,
                FieldScores = fieldScores,
                PrimaryMatchField = primaryField
            };
        }
    }

    public class FieldMatchResult
    {
        public List<string> MatchedFields { get; set; } = new();
        public Dictionary<string, double> FieldScores { get; set; } = new();
        public string? PrimaryMatchField { get; set; }
    }
}
