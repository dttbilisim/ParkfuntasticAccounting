using ecommerce.Core.Entities.Admin;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Core.Entities
{
    public class RoleMenu : IEntity<int>
    {
        public int Id { get; set; }

        public int RoleId { get; set; }

        public int MenuId { get; set; }

        public bool CanView { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }

        #region [ Navigation Properties ]

        [ForeignKey("RoleId")]
        public virtual ApplicationRole Role { get; set; }

        [ForeignKey("MenuId")]
        public virtual Menu Menu { get; set; }

        #endregion        
    }
}

