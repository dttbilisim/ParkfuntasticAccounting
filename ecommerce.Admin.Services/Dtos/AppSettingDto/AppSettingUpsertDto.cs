using AutoMapper;
using ecommerce.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace ecommerce.Admin.Domain.Dtos.AppSettingDto
{
    [AutoMap(typeof(AppSettings), ReverseMap = true)]
    public class AppSettingUpsertDto
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Anahtar gereklidir")]
        [MaxLength(200)]
        public string Key { get; set; } = string.Empty;

        [Required(ErrorMessage = "Değer gereklidir")]
        public string Value { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
    }
}
