using AutoMapper;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.RoleMenuDto
{
    [AutoMap(typeof(RoleMenu))]
    public class RoleMenuListDto
    {

        public int? Id { get; set; }
        
        public int RoleId { get; set; }

        public int MenuId { get; set; }

        public bool CanView { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        
        public string MenuName { get; set; }

    }
}
