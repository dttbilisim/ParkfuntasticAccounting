using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.ProductImageDto
{
    [AutoMap(typeof(ProductImage))]
    public class ProductImageListDto
    {
        public int Id { get; set; }
        public int Order { get; set; }
        public string FileGuid { get; set; }
        public string FileName { get; set; }
        public string Root { get; set; }

        [Ignore]
        public byte[] Base64Str
        {
            get
            {
                //byte[] imageArray = System.IO.File.ReadAllBytes(Root);
                //string base64ImageRepresentation = Convert.ToBase64String(imageArray);
                //return $"data:image/jpeg;base64,{base64ImageRepresentation}";
                if (File.Exists(Root))
                    return System.IO.File.ReadAllBytes(Root);
                else
                    return new byte[0];
            }
        }

        [Ignore]
        public string StatusStr
        {
            get
            {
                switch ((int)Status)
                {
                    case 0: return "Pasif";
                    case 1: return "Aktif";
                    case 99: return "Silinmiş";
                    default: return "Belirsiz";
                };
            }
        }

        public EntityStatus Status { get; set; }
    }
}
