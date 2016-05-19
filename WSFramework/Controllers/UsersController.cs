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
    public class UsersController : ApiController
    {
        private AuthContextEntities1 db = new AuthContextEntities1();

        // GET: api/Users
        [Authorize(Roles = "Admin")]
        [ResponseType(typeof(User))]
        public IQueryable<User> GetUsers()
        {
            return db.Users;
        }
        
        // GET: api/Users/5
        [Authorize(Roles = "Admin")]
        [ResponseType(typeof(User))]
        public IHttpActionResult GetUser(string id)
        {
            User user = db.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        // DELETE: api/Users/5
        [Authorize(Roles = "Admin")]
        [ResponseType(typeof(User))]
        public IHttpActionResult DeleteUser(string id)
        {
            User user = db.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            db.Users.Remove(user);
            db.SaveChanges();
            return Ok(user);
        }
    }
}