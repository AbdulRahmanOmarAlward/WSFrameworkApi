using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using WSFramework.Models;

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
        public IHttpActionResult GetAspNetUserRole(string id)
        {
            IList<AspNetUserRole> aspNetUserRole = db.AspNetUserRoles.Where(p => p.UserId.ToString() == id).ToList();
            if (aspNetUserRole == null)
            {
                return NotFound();
            }

            return Ok(aspNetUserRole);
        }

        // PUT: api/UserRoles/5
        [Authorize(Roles = "Admin")]
        [ResponseType(typeof(void))]
        public IHttpActionResult PutAspNetUserRole(string id, AspNetUserRole aspNetUserRole)
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
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AspNetUserRoleExists(id))
                {
                    return NotFound();
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
        public IHttpActionResult PostAspNetUserRole(AspNetUserRole aspNetUserRole)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.AspNetUserRoles.Add(aspNetUserRole);

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateException)
            {
                if (AspNetUserRoleExists(aspNetUserRole.UserId))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtRoute("api/UserRoles", new { id = aspNetUserRole.UserId }, aspNetUserRole);
        }

        // DELETE: api/UserRoles
        [Authorize(Roles = "Admin")]
        [ResponseType(typeof(AspNetUserRole))]
        public IHttpActionResult DeleteAspNetUserRole(AspNetUserRole aspNetUserRoleIn)
        {
            AspNetUserRole aspNetUserRole = db.AspNetUserRoles.Single(p => p.UserId == aspNetUserRoleIn.UserId && p.RoleId == aspNetUserRoleIn.RoleId);
            if (aspNetUserRole == null)
            {
                return NotFound();
            }

            db.AspNetUserRoles.Remove(aspNetUserRole);
            db.SaveChanges();

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

        private bool AspNetUserRoleExists(string id)
        {
            return db.AspNetUserRoles.Count(e => e.UserId == id) > 0;
        }
    }
}