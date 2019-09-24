using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace coderush.Models
{
    public class MembershipRole
    {
        public string Id { get; set; }
        public string MembershipId { get; set; }
        public string RoleName { get; set; }
    }
}
