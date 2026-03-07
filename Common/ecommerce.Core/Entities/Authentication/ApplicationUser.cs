using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Accounting;

namespace ecommerce.Core.Entities.Authentication {

    public class ApplicationUser :  IdentityUser<int> {
        [NotMapped]
        public string Password { get; set; } = "";
        [NotMapped]
        public string ConfirmPassword { get; set; } = "";
        public string ? FirstName { get; set; } = null!;
        public string? MiddleName { get; set; }
        public string ? LastName { get; set; } = null!;
        public DateTime? RegisterDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime? BirthDate { get; set; }
        
   
        

        [NotMapped]
        public string FullName
        {
            get { return FirstName + " " + LastName; }
        }
        public List<ApplicationRole> Roles { get; set; } = new();
        public List<UserMenu> UserMenus { get; set; } = new();
        public int? CityId { get; set; }
        public int? TownId { get; set; }
        public int? MembershipId { get; set; }
        [ForeignKey("MembershipId")]
        public Membership? Membership { get; set; }
        public int? CompanyId { get; set; }
        public int? SalesPersonId { get; set; }
        [ForeignKey("SalesPersonId")]
        public SalesPerson? SalesPerson { get; set; }
        
        public int? CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        /// <summary>Üst kurye (ana kurye) — alt kullanıcı ise bu alan set edilir.</summary>
        public int? ParentCourierId { get; set; }
        [ForeignKey("ParentCourierId")]
        public virtual ApplicationUser? ParentCourier { get; set; }

        // Multi-address support for ApplicationUser
        public virtual List<UserAddress> UserAddresses { get; set; } = new();

        // Kullanıcının kayıtlı araçları
        public virtual List<UserCars> UserCars { get; set; } = new();

        // Kullanıcının push bildirim token'ları
        public virtual List<UserPushToken> PushTokens { get; set; } = new();

        public string? ResetEmailToken { get; set; }
        public DateTime? ResetEmailTokenExpireDate { get; set; }
        public string? FileDocumenturl { get; set; }

        /// <summary>PcPos kullanıcısı mı? (Bu kullanıcı PcPos kasalarına erişebilir)</summary>
        public bool IsPcPosUser { get; set; }
        /// <summary>PcPos transfer: Şirket kodu</summary>
        [MaxLength(50)]
        public string? CompanyCode { get; set; }
        /// <summary>PcPos transfer: Kasa ID'leri (virgülle ayrılmış)</summary>
        [MaxLength(500)]
        public string? CaseIds { get; set; }
        /// <summary>PcPos transfer: Düzenleme yetkisi</summary>
        public bool IsEdit { get; set; }
        /// <summary>PcPos transfer: Kullanıcı tipi (2 = POS kullanıcısı)</summary>
        public int? UserType { get; set; }
        /// <summary>PcPos transfer: Kullanıcı aktif mi</summary>
        public bool IsActive { get; set; } = true;
    }
}
