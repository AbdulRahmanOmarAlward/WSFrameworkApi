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
using WSFramework.Helpers;

namespace WSFramework.Controllers
{
    public class UsersController : ApiController, WSFController
    {
        private AuthContextEntitiesUsers db = new AuthContextEntitiesUsers();

        // GET: /Users
        [Authorize(Roles = "Admin")]
        public IQueryable<User> GetUsers()
        {
            return db.Users;
        }

        // GET: /Users/5
        [Authorize(Roles = "Admin")]
        [ResponseType(typeof(User))]
        public async Task<IHttpActionResult> GetUser(string id)
        {
            User user = await db.Users.FindAsync(id);
            if (user == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));

            return Ok(user);
        }

        // DELETE: /Users/5
        [Authorize(Roles = "Admin")]
        [ResponseType(typeof(User))]
        public async Task<IHttpActionResult> DeleteUser(string id)
        {
            User user = await db.Users.FindAsync(id);
            if (user == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));
            
            db.Users.Remove(user);
            await db.SaveChangesAsync();
            return Ok(user);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public HttpResponseMessage getHttpResponse(HttpStatusCode statusCode)
        {
            HttpResponseMessage resp = new HttpResponseMessage();
            resp.StatusCode = statusCode;
            string reasonPhrase;
            switch (statusCode)
            {
                case HttpStatusCode.NotFound:
                    reasonPhrase = "User ID not present in database.";
                    break;
                default:
                    reasonPhrase = "Contact service provider.";
                    break;
            }
            resp.ReasonPhrase = reasonPhrase;
            return resp;
        }
    }
}