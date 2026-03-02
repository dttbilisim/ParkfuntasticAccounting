using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.ProductImageDto
{
    [AutoMap(typeof(ProductImage),ReverseMap =true)]
    public class ProductImageUpsertDto
    {
        public int? Id { get; set; }
        public int ProductId { get; set; }
        public int Order { get; set; }
        public string FileGuid { get; set; }
        public string FileName { get; set; }
        public string Root { get; set; }
        public EntityStatus Status { get; set; }

        [Ignore]
        public byte[] Base64Str
        {
            get
            {
                if (File.Exists(Root))
                    return System.IO.File.ReadAllBytes(Root);
                else
                    return new byte[0];
            }
        }

        [Ignore]
        public bool StatusBool { get; set; }
    }
}
