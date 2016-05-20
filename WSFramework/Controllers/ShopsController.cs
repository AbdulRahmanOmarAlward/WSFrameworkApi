using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using WSFramework.Models;
using WSFramework.Providers;

namespace WSFramework.Controllers
{
    public class ShopIn
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string DescriptionFull { get; set; }
    }

    public class ShopUpdate
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string DescriptionFull { get; set; }
    }

    public class CurrIdentity
    {
        public string userId { get; set; }
        public string role { get; set; }
    }

    public class ShopsController : ApiController
    {
        private WSFrameworkDBEntitiesFull db = new WSFrameworkDBEntitiesFull();

        // GET: api/Shops
        public IQueryable<Shop> GetShops()
        {
            return db.Shops;
        }

        // GET: api/Shops/5
        [ResponseType(typeof(Shop))]
        public async Task<IHttpActionResult> GetShop(long id)
        {
            Shop shop = await db.Shops.FindAsync(id);
            if (shop == null)
            {
                return ResponseMessage(HttpResponseGenerator.getHttpResponse(HttpStatusCode.NotFound, "Shop ID not present in database."));
            }

            return Ok(shop);
        }

        // PUT: api/Shops/5
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutShop(long id, ShopUpdate shopIn)
        {
            CurrIdentity identity = getIdentity();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Shop shopCurrent = await db.Shops.FindAsync(id);
            if (shopCurrent == null)
            {
                return ResponseMessage(HttpResponseGenerator.getHttpResponse(HttpStatusCode.NotFound, "Shop ID not present in database."));
            }

            if (identity.userId == shopCurrent.UserId || identity.role == "Admin")
            {
                if (shopCurrent.Title != shopIn.Title)
                    if (ShopTitleExists(shopIn.Title))
                    {
                        return ResponseMessage(HttpResponseGenerator.getHttpResponse(HttpStatusCode.Conflict, "Shop title already taken."));
                    }
                        
                shopCurrent.Title = shopIn.Title;
                shopCurrent.Description = shopIn.Description;
                shopCurrent.DescriptionFull = shopIn.DescriptionFull;
                shopCurrent.UpdatedAt = DateTime.Now;
                db.Entry(shopCurrent).State = EntityState.Modified;

                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShopExists(id))
                    {
                        return ResponseMessage(HttpResponseGenerator.getHttpResponse(HttpStatusCode.NotFound, "Shop ID not present in database."));
                    }
                    else
                    {
                        throw;
                    }
                }

                return StatusCode(HttpStatusCode.NoContent);
            }

            return ResponseMessage(HttpResponseGenerator.getHttpResponse(HttpStatusCode.Forbidden, "You are not authorized to modify this data."));
        }

        // POST: api/Shops
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(Shop))]
        public async Task<IHttpActionResult> PostShop(ShopIn shop)
        {
            if (ShopTitleExists(shop.Title))
            {
                return ResponseMessage(HttpResponseGenerator.getHttpResponse(HttpStatusCode.Conflict, "Shop title already taken."));
            }

            CurrIdentity identity = getIdentity();
            Shop newShop = new Shop();
            newShop.UserId = identity.userId;
            newShop.Title = shop.Title;
            newShop.Description = shop.Description;
            newShop.DescriptionFull = shop.DescriptionFull;
            newShop.Views = 0;
            newShop.IsActive = 0;
            DateTime now = DateTime.Now;
            newShop.CreatedAt = now;
            newShop.UpdatedAt = now;

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Shops.Add(newShop);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (ShopExists(newShop.Id))
                {
                    return ResponseMessage(HttpResponseGenerator.getHttpResponse(HttpStatusCode.Conflict, "Shop ID already in use."));
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtRoute("DefaultApi", new { id = newShop.Id }, newShop);
        }

        // DELETE: api/Shops/5
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(Shop))]
        public async Task<IHttpActionResult> DeleteShop(long id)
        {
            CurrIdentity identity = getIdentity();
            Shop shop = await db.Shops.FindAsync(id);

            if (shop == null)
            {
                return ResponseMessage(HttpResponseGenerator.getHttpResponse(HttpStatusCode.NotFound, "Shop ID not present in database."));
            }

            if (identity.userId == shop.UserId || identity.role == "Admin")
            {
                db.Shops.Remove(shop);
                await db.SaveChangesAsync();

                return Ok(shop);
            }
            return ResponseMessage(HttpResponseGenerator.getHttpResponse(HttpStatusCode.Forbidden, "You are not authorized to modify this data."));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool ShopExists(long id)
        {
            return db.Shops.Count(e => e.Id == id) > 0;
        }

        private bool ShopTitleExists(string title)
        {
            return db.Shops.Count(e => e.Title == title) > 0;
        }

        private CurrIdentity getIdentity()
        {
            var identity = (ClaimsIdentity)User.Identity;
            IEnumerable<Claim> claims = identity.Claims;
            string userId = claims.FirstOrDefault(p => p.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            string role = claims.FirstOrDefault(p => p.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Value;
            CurrIdentity identityOut = new CurrIdentity();
            identityOut.userId = userId;
            identityOut.role = role;
            return identityOut;
        }
    }
}