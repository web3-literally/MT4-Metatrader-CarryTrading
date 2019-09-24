using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using coderush.Data;
using coderush.Models;
using coderush.Models.AccountViewModels;
using coderush.Models.SyncfusionViewModels;
using coderush.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using coderush.Models.ManageViewModels;

namespace coderush.Controllers.Api
{
    [Authorize]
    [Produces("application/json")]
    [Route("api/Membership")]
    public class MembershipController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MembershipController(ApplicationDbContext context,
                        UserManager<ApplicationUser> userManager,
                        RoleManager<IdentityRole> roleManager)
        {
            _context = context;
        }

        // GET: api/Membership
        [HttpGet]
        public IActionResult GetMembership()
        {
            List<Membership> Items = new List<Membership>();
            Items = _context.Membership.ToList();
            int Count = Items.Count();
            return Ok(new { Items, Count });
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Insert([FromBody]CrudViewModel<Membership> payload)
        {
            Membership register = payload.value;
            _context.Membership.Add(register);
            await _context.SaveChangesAsync();
            
            return Ok(register);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Remove([FromBody]CrudViewModel<Membership> payload)
        {
            Membership removeItem = _context.Membership.SingleOrDefault(x => x.MembershipId.Equals(payload.key));
            if (removeItem != null)
            {
                _context.Remove(removeItem);
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpGet("[action]/{id}")]
        public IActionResult GetByMembershipId([FromRoute]string id)
        {
            Membership Item = _context.Membership.SingleOrDefault(x => x.MembershipId.Equals(id));
            List<Membership> Items = new List<Membership>();
            if (Item != null)
            {
                Items.Add(Item);
            }
            int Count = Items.Count();
            return Ok(new { Items, Count });
        }
    }
}
