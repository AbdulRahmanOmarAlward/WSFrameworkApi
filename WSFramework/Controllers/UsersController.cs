using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using WSFramework.Models;
using WSFramework.Providers;

namespace WSFramework.Controllers
{
    public class UsersController : ApiController
    {
        private AuthContextEntitiesUsers db = new AuthContextEntitiesUsers();

        // GET: api/Users
        [Authorize(Roles = "Admin")]
        public IQueryable<User> GetUsers()
        {
            return db.Users;
        }

        // GET: api/Users/5
        [Authorize(Roles = "Admin")]
        [ResponseType(typeof(User))]
        public async Task<IHttpActionResult> GetUser(string id)
        {
            User user = await db.Users.FindAsync(id);
            if (user == null)
            {
                return ResponseMessage(HttpResponseGenerator.getHttpResponse(HttpStatusCode.NotFound, "User ID not present in database."));
            }

            return Ok(user);
        }

        // DELETE: api/Users/5
        [Authorize(Roles = "Admin")]
        [ResponseType(typeof(User))]
        public async Task<IHttpActionResult> DeleteUser(string id)
        {
            User user = await db.Users.FindAsync(id);
            if (user == null)
            {
                return ResponseMessage(HttpResponseGenerator.getHttpResponse(HttpStatusCode.NotFound, "User ID not present in database."));
            }

            db.Users.Remove(user);
            await db.SaveChangesAsync();

            return Ok(user);
        }
    }
}