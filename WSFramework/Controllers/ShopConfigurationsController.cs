using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using WSFramework.Helpers;
using WSFramework.Models;

namespace WSFramework.Controllers
{
    public class ShopConfigurationsController : ApiController
    {
        private WSFrameworkDBEntitiesFull db = new WSFrameworkDBEntitiesFull();

        // GET: /ShopConfigurations
        public IQueryable<ShopConfiguration> GetShopConfigurations()
        {
            return db.ShopConfigurations;
        }

        // GET: /ShopConfigurations/5
        [ResponseType(typeof(ShopConfiguration))]
        public async Task<IHttpActionResult> GetShopConfiguration(long id)
        {
            ShopConfiguration shopConfiguration = await db.ShopConfigurations.FindAsync(id);
            if (shopConfiguration == null)
            {
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));
            }

            return Ok(shopConfiguration);
        }

        // GET: ShopConfigurations/Own/
        [Authorize(Roles = "Admin, User")]
        [Route("ShopConfigurations/Own")]
        [ResponseType(typeof(Shop))]
        public async Task<IHttpActionResult> GetOwnShopConfig()
        {
            CurrentIdentity identity = getIdentity();

            Shop shop = await db.Shops.FirstOrDefaultAsync(p => p.UserId == identity.userId);
            if (shop == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));

            ShopConfiguration shopConfiguration = await db.ShopConfigurations.FindAsync(shop.Id);
            if (shopConfiguration == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));
            return Ok(shopConfiguration);
        }

        // PUT: /ShopConfigurations/5
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutShopConfiguration(long id, ShopConfiguration shopConfiguration)
        {
            CurrentIdentity identity = getIdentity();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (identity.role == "Admin")
            {

            }
            else
            {
                Shop shop = await db.Shops.FirstOrDefaultAsync(p => p.UserId == identity.userId);
                if (shop == null)
                    return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));

                if (shop.Id != shopConfiguration.ShopId)
                    return ResponseMessage(getHttpResponse(HttpStatusCode.Unauthorized));
            }

            if (shopConfiguration.LayoutId < 0)
                return ResponseMessage(getHttpResponse(HttpStatusCode.BadRequest));

            Shop shopCurrent = await db.Shops.FindAsync(id);
            if (shopCurrent == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));

            if (identity.userId == shopCurrent.UserId || identity.role == "Admin")
            {
                db.Entry(shopConfiguration).State = EntityState.Modified;

                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShopConfigurationExists(id))
                    {
                        return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));
                    }
                    else
                    {
                        throw;
                    }
                }

                return StatusCode(HttpStatusCode.NoContent);
            }
            return ResponseMessage(getHttpResponse(HttpStatusCode.Unauthorized));
        }

        // POST: /ShopConfigurations
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(ShopConfiguration))]
        public async Task<IHttpActionResult> PostShopConfiguration(ShopConfiguration shopConfiguration)
        {
            CurrentIdentity identity = getIdentity();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (identity.role == "Admin")
            {

            }
            else
            {
                Shop shop = await db.Shops.FirstOrDefaultAsync(p => p.UserId == identity.userId);
                if (shop == null)
                    return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));

                if (shop.Id != shopConfiguration.ShopId)
                    return ResponseMessage(getHttpResponse(HttpStatusCode.Unauthorized));
            }

            if (shopConfiguration.LayoutId < 0)
                return ResponseMessage(getHttpResponse(HttpStatusCode.BadRequest));

            db.ShopConfigurations.Add(shopConfiguration);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (ShopConfigurationExists(shopConfiguration.ShopId))
                {
                    return ResponseMessage(getHttpResponse(HttpStatusCode.Conflict));
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtRoute("WSApi", new { id = shopConfiguration.ShopId }, shopConfiguration);
        }

        // DELETE: /ShopConfigurations/5
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(ShopConfiguration))]
        public async Task<IHttpActionResult> DeleteShopConfiguration(long id)
        {
            CurrentIdentity identity = getIdentity();

            if (identity.role == "Admin")
            {

            }
            else
            {
                Shop shop = await db.Shops.FirstOrDefaultAsync(p => p.UserId == identity.userId);
                if (shop == null)
                    return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));

                if (shop.Id != id)
                    return ResponseMessage(getHttpResponse(HttpStatusCode.Unauthorized));
            }

            ShopConfiguration shopConfiguration = await db.ShopConfigurations.FindAsync(id);
            if (shopConfiguration == null)
            {
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));
            }

            db.ShopConfigurations.Remove(shopConfiguration);
            await db.SaveChangesAsync();

            return Ok(shopConfiguration);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool ShopConfigurationExists(long id)
        {
            return db.ShopConfigurations.Count(e => e.ShopId == id) > 0;
        }

        private CurrentIdentity getIdentity()
        {
            var identity = (ClaimsIdentity)User.Identity;
            IEnumerable<Claim> claims = identity.Claims;
            string userId = claims.FirstOrDefault(p => p.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            string role = claims.FirstOrDefault(p => p.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Value;
            CurrentIdentity identityOut = new CurrentIdentity();
            identityOut.userId = userId;
            identityOut.role = role;
            return identityOut;
        }

        public HttpResponseMessage getHttpResponse(HttpStatusCode statusCode)
        {
            HttpResponseMessage resp = new HttpResponseMessage();
            resp.StatusCode = statusCode;
            string reasonPhrase;
            switch (statusCode)
            {
                case HttpStatusCode.NotFound:
                    reasonPhrase = "Shop ID not present in database.";
                    break;
                case HttpStatusCode.Conflict:
                    reasonPhrase = "Shop already has a configuration.";
                    break;
                case HttpStatusCode.BadRequest:
                    reasonPhrase = "Not a valid layoutID.";
                    break;
                case HttpStatusCode.Unauthorized:
                    reasonPhrase = "You are not authorized to modify this data.";
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