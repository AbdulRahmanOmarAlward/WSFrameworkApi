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

namespace WSFramework.Controllers
{
    public class ProductIn
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string DescriptionFull { get; set; }
        public long ShopId { get; set; }
    }
    public class ProductUpdate
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string DescriptionFull { get; set; }
        public int IsActive { get; set; }
    }
    public class ProductsController : ApiController
    {
        private WSFrameworkDBEntitiesFull db = new WSFrameworkDBEntitiesFull();

        // GET: api/Products
        public IQueryable<Product> GetProducts()
        {
            return db.Products;
        }

        // GET: api/Products/5
        [ResponseType(typeof(Product))]
        public async Task<IHttpActionResult> GetProduct(long id)
        {
            Product product = await db.Products.FindAsync(id);
            if (product == null)
            {
                return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.NotFound, "Product ID not present in database."));
            }

            return Ok(product);
        }
        //TODO: this is where u left off
        // PUT: api/Products/5
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutProduct(long id, Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != product.Id)
            {
                return BadRequest();
            }

            db.Entry(product).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.NotFound, "Product ID not present in database."));
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Products
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(Product))]
        public async Task<IHttpActionResult> PostProduct(ProductIn productIn)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            IdentityHelper identity = getIdentity();
            Shop shopToCheck = await db.Shops.FindAsync(productIn.ShopId);
            Product productsToCheck = await db.Products.FindAsync(productIn.ShopId);

            if (shopToCheck.UserId != identity.userId)
                if(identity.role != "Admin")
                    return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.Forbidden, "Not owner of provided shop ID."));

            if(ProductTitleExistsInShop(productIn.Title, productIn.ShopId))
                return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.Conflict, "Shop already has a product with that title."));

            Product newProduct = new Product();
            newProduct.Title = productIn.Title;
            newProduct.Description = productIn.Description;
            newProduct.DescriptionFull = productIn.DescriptionFull;
            newProduct.Views = 0;
            newProduct.IsActive = 0;
            DateTime now = DateTime.Now;
            newProduct.CreatedAt = now;
            newProduct.UpdatedAt = now;
            newProduct.ShopId = productIn.ShopId;

            db.Products.Add(newProduct);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (ProductExists(newProduct.Id))
                {
                    return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.Conflict, "Product ID already present in database."));
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtRoute("DefaultApi", new { id = newProduct.Id }, newProduct);
        }

        // DELETE: api/Products/5
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(Product))]
        public async Task<IHttpActionResult> DeleteProduct(long id)
        {
            IdentityHelper identity = getIdentity();

            Product product = await db.Products.FindAsync(id);
            if (product == null)
            {
                return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.NotFound, "Product ID not present in database."));
            }

            Shop shop = await db.Shops.FindAsync(product.ShopId);

            if (identity.userId == shop.UserId || identity.role == "Admin")
            {
                db.Products.Remove(product);
                await db.SaveChangesAsync();

                return Ok(product);
            }
            return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.Forbidden, "You are not authorized to modify this data."));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool ProductExists(long id)
        {
            return db.Products.Count(e => e.Id == id) > 0;
        }

        private bool ProductTitleExistsInShop(string title, long shopId)
        {
            return db.Products.Count(e => e.Title == title && e.ShopId == shopId) > 0;
        }

        private IdentityHelper getIdentity()
        {
            var identity = (ClaimsIdentity)User.Identity;
            IEnumerable<Claim> claims = identity.Claims;
            string userId = claims.FirstOrDefault(p => p.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            string role = claims.FirstOrDefault(p => p.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Value;
            IdentityHelper identityOut = new IdentityHelper();
            identityOut.userId = userId;
            identityOut.role = role;
            return identityOut;
        }
    }
}