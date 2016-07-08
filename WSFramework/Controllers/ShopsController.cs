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
using WSFramework.Helpers;
using WSFramework.Controllers;
using System.Net.Http;

namespace WSFramework.Controllers
{
    public class ShopIn
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string DescriptionFull { get; set; }
    }

    class ShopProducts
    {
        public IList<ProductOut> Products { get; set; }
    }

    public class ShopUpdate
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string DescriptionFull { get; set; }
        public int IsActive { get; set; }
    }

    public class ShopsController : ApiController, WSFController
    {
        private WSFrameworkDBEntitiesFull db = new WSFrameworkDBEntitiesFull();

        // GET: /Shops
        public IQueryable<Shop> GetShops()
        {
            CurrentIdentity identity = getIdentity();
            if(identity.role == "Admin")
            {
                return db.Shops;
            }
            else
            {
                return db.Shops.Where(p => p.IsActive == 1);
            }
        }

        // GET: /Shops/5
        [Route("Shops/{id}/")]
        [ResponseType(typeof(Shop))]
        public async Task<IHttpActionResult> GetShop(long id)
        {
            Shop shop = await db.Shops.FindAsync(id);
            if (shop == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));

            return Ok(shop);
        }

        // GET: /Shops/Own
        [Route("Shops/Own/")]
        [ResponseType(typeof(Shop))]
        public async Task<IHttpActionResult> GetOwnShop()
        {
            CurrentIdentity identity = getIdentity();
            Shop shop = await db.Shops.FirstOrDefaultAsync(p => p.UserId == identity.userId);
            if (shop == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));

            return Ok(shop);
        }

        // GET: /Shops/5/products
        [Route("Shops/{id}/products")]
        [ResponseType(typeof(ShopProducts))]
        public async Task<IHttpActionResult> GetShopProducts(long id)
        {
            Shop shop = await db.Shops.FindAsync(id);
            if (shop == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));

            ShopProducts shopOut = new ShopProducts();
            shopOut.Products = new List<ProductOut>();
            IList<Product> productsInShop = new List<Product>();
            productsInShop = await db.Products.Where(p => p.ShopId == id).ToListAsync();

            foreach (var product in productsInShop)
            {
                shopOut.Products.Add(await GetProductsForShop(product.Id));
            }

            await IncrementView(id);

            return Ok(shopOut);
        }

        // GET: /Shops/Own/Products
        [Route("Shops/Own/Products")]
        [ResponseType(typeof(ShopProducts))]
        public async Task<IHttpActionResult> GetOwnShopProducts()
        {
            CurrentIdentity identity = getIdentity();
            Shop shop = await db.Shops.FirstOrDefaultAsync(p => p.UserId == identity.userId);
            if (shop == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));

            ShopProducts shopOut = new ShopProducts();
            shopOut.Products = new List<ProductOut>();
            IList<Product> productsInShop = new List<Product>();
            productsInShop = await db.Products.Where(p => p.ShopId == shop.Id).ToListAsync();

            foreach (var product in productsInShop)
            {
                shopOut.Products.Add(await GetProductsForShop(product.Id));
            }

            return Ok(shopOut);
        }

        // PUT: /Shops/5
        [Route("Shops/{id}/")]
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutShop(long id, ShopUpdate shopIn)
        {
            CurrentIdentity identity = getIdentity();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            if(shopIn.IsActive != 0)
                if(shopIn.IsActive != 1)
                    return ResponseMessage(getHttpResponse(HttpStatusCode.BadRequest));

            Shop shopCurrent = await db.Shops.FindAsync(id);
            if (shopCurrent == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));
            

            if (identity.userId == shopCurrent.UserId || identity.role == "Admin")
            {
                if (shopCurrent.Title != shopIn.Title)
                    if (ShopTitleExists(shopIn.Title))
                        return ResponseMessage(getHttpResponse(HttpStatusCode.Conflict));
                    
                shopCurrent.Title = (shopIn.Title != null) ? shopIn.Title : shopCurrent.Title;
                shopCurrent.Description = (shopIn.Description != null) ? shopIn.Description : shopCurrent.Description;
                shopCurrent.DescriptionFull = (shopIn.DescriptionFull != null) ? shopIn.DescriptionFull : shopCurrent.DescriptionFull;
                shopCurrent.UpdatedAt = DateTime.Now;
                shopCurrent.IsActive = shopIn.IsActive;
                db.Entry(shopCurrent).State = EntityState.Modified;

                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShopExists(id))
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

        // POST: /Shops
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(Shop))]
        public async Task<IHttpActionResult> PostShop(ShopIn shop)
        {
            if (ShopTitleExists(shop.Title))
                return ResponseMessage(getHttpResponse(HttpStatusCode.Conflict));

            CurrentIdentity identity = getIdentity();

            if((await db.Shops.CountAsync(p => p.UserId == identity.userId)) > 0)
            {
                return ResponseMessage(getHttpResponse(HttpStatusCode.Forbidden));
            }

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
                return BadRequest(ModelState);
            
            db.Shops.Add(newShop);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (ShopExists(newShop.Id))
                {
                    return ResponseMessage(getHttpResponse(HttpStatusCode.Conflict));
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtRoute("WSApi", new { id = newShop.Id }, newShop);
        }

        // DELETE: /Shops/5
        [Route("Shops/{id}/")]
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(Shop))]
        public async Task<IHttpActionResult> DeleteShop(long id)
        {
            CurrentIdentity identity = getIdentity();
            Shop shop = await db.Shops.FindAsync(id);

            if (shop == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));
            

            if (identity.userId == shop.UserId || identity.role == "Admin")
            {
                db.Shops.Remove(shop);
                await db.SaveChangesAsync();

                return Ok(shop);
            }
            return ResponseMessage(getHttpResponse(HttpStatusCode.Unauthorized));
        }

        private async Task IncrementView(long id)
        {
            Shop ShopCurrent = await db.Shops.FindAsync(id);
            ShopCurrent.Views = ShopCurrent.Views + 1;
            db.Entry(ShopCurrent).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
            }
        }

        private async Task<ProductOut> GetProductsForShop(long id)
        {
            Product product = await db.Products.FindAsync(id);

            IList<Image> images = await db.Images.Where(p => p.ProductId == id).ToListAsync();
            IList<ProductsToCategory> productToCategories = await db.ProductsToCategories.Where(p => p.ProductId == id).ToListAsync();
            IList<Category> categories = new List<Category>();

            foreach (var categoryEach in productToCategories)
            {
                categories.Add(await db.Categories.FirstOrDefaultAsync(p => p.Id == categoryEach.CategoryId));
            }

            ProductOut productOut = new ProductOut();
            productOut.Id = product.Id;
            productOut.Title = product.Title;
            productOut.Description = product.Description;
            productOut.DescriptionFull = product.DescriptionFull;
            productOut.Views = product.Views;
            productOut.IsActive = product.IsActive;
            productOut.CreatedAt = product.CreatedAt;
            productOut.UpdatedAt = product.UpdatedAt;
            productOut.ShopId = product.ShopId;
            productOut.Stock = product.Stock;
            productOut.Price = product.Price;
            if (images != null)
            {
                productOut.Images = images;
            }
            if (categories != null)
            {
                productOut.Categories = categories;
            }

            return await Task.FromResult(productOut);
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
                    reasonPhrase = "Shop title already taken.";
                    break;
                case HttpStatusCode.Forbidden:
                    reasonPhrase = "User ID already has a shop.";
                    break;
                case HttpStatusCode.BadRequest:
                    reasonPhrase = "isActive can only be 0 or 1. 0 is inactive. 1 is active.";
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