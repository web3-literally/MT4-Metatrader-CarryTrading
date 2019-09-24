using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace coderush.Models.AccountViewModels
{
    public class MemberRoleViewModel
    {
        public int CounterId { get; set; }
        public string MembershipId { get; set; }
        public string RoleName { get; set; }
        public bool IsHaveAccess { get; set; }
    }
}
