using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using WSFramework.Models;
using WSFramework.Providers;

namespace WSFramework.Controllers
{
    public class UserRolesController : ApiController
    {
        private AuthContextEntities2 db = new AuthContextEntities2();

        // GET: api/UserRoles
        [Authorize(Roles = "Admin")]
        public IQueryable<AspNetUserRole> GetAspNetUserRoles()
        {
            return db.AspNetUserRoles;
        }

        // GET: api/UserRoles/5
        [Authorize(Roles = "Admin")]
        [ResponseType(typeof(AspNetUserRole))]
        public async Task<IHttpActionResult> GetAspNetUserRole(string id)
        {
            IList<AspNetUserRole> aspNetUserRole = await db.AspNetUserRoles.Where(p => p.UserId.ToString() == id).ToListAsync();
            if (aspNetUserRole == null)
            {
                return ResponseMessage(HttpResponseGenerator.getHttpResponse(HttpStatusCode.NotFound, "UserRole not present in database."));
            }

            return Ok(aspNetUserRole);
        }

        // PUT: api/UserRoles/5
        [Authorize(Roles = "Admin")]
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutAspNetUserRole(string id, AspNetUserRole aspNetUserRole)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != aspNetUserRole.UserId)
            {
                return BadRequest();
            }

            db.Entry(aspNetUserRole).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AspNetUserRoleExists(aspNetUserRole))
                {
                    return ResponseMessage(HttpResponseGenerator.getHttpResponse(HttpStatusCode.NotFound, "UserRole not present in database."));
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/UserRoles
        [Authorize(Roles = "Admin")]
        [ResponseType(typeof(AspNetUserRole))]
        public async Task<IHttpActionResult> PostAspNetUserRole(AspNetUserRole aspNetUserRole)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.AspNetUserRoles.Add(aspNetUserRole);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (AspNetUserRoleExists(aspNetUserRole))
                {
                    return ResponseMessage(HttpResponseGenerator.getHttpResponse(HttpStatusCode.Conflict, "UserRole already present in database."));
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtRoute("DefaultApi", new { id = aspNetUserRole.UserId }, aspNetUserRole);
        }

        // DELETE: api/UserRoles/5
        [Authorize(Roles = "Admin")]
        [ResponseType(typeof(AspNetUserRole))]
        public async Task<IHttpActionResult> DeleteAspNetUserRole(AspNetUserRole aspNetUserRoleIn)
        {
            AspNetUserRole aspNetUserRole = await db.AspNetUserRoles.SingleAsync(p => p.UserId == aspNetUserRoleIn.UserId && p.RoleId == aspNetUserRoleIn.RoleId);
            if (aspNetUserRole == null)
            {
                return ResponseMessage(HttpResponseGenerator.getHttpResponse(HttpStatusCode.NotFound, "UserRole not present in database."));
            }

            db.AspNetUserRoles.Remove(aspNetUserRole);
            await db.SaveChangesAsync();

            return Ok(aspNetUserRole);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool AspNetUserRoleExists(AspNetUserRole userRole)
        {
            return db.AspNetUserRoles.Count(e => e.UserId == userRole.UserId && e.RoleId == userRole.RoleId) > 0;
        }
    }
}