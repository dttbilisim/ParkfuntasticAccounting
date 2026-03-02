using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Interfaces;

namespace ecommerce.Core.Entities
{
	public class MembershipActivation:IEntity<int>
	{
		public int Id { get; set; }
        public int MembershipId { get; set; }
        [ForeignKey("MembershipId")]
        public Membership Membership { get; set; } = null!;
        public string Token { get; set; } = null!;
		public DateTime ExpireDate { get; set; }
	}
}

