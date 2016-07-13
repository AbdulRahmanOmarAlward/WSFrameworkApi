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
using System.Net.Http;

namespace WSFramework.Controllers
{
    public class ProductIn
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string DescriptionFull { get; set; }
        public long ShopId { get; set; }
        public IList<string> Images { get; set; }
        public IList<long> CategoryId { get; set; }
        public int Stock { get; set; }
        public float Price { get; set; }
    }

    public class ProductOut : Product
    {
        public IList<Image> Images { get; set; }
        public IList<Category> Categories { get; set; }
    }

    public class ProductUpdate
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string DescriptionFull { get; set; }
        public int IsActive { get; set; }
        public IList<string> Images { get; set; }
        public IList<long> CategoryId { get; set; }
        public int Stock { get; set; }
        public float? Price { get; set; }
    }

    public class ProductsController : ApiController, WSFController
    {
        private WSFrameworkDBEntitiesFull db = new WSFrameworkDBEntitiesFull();

        // GET: /Products
        public IQueryable<Product> GetProducts()
        {
            return db.Products;
        }

        // GET: /Products/5
        [ResponseType(typeof(Product))]
        public async Task<IHttpActionResult> GetProduct(long id)
        {
            Product product = await db.Products.FindAsync(id);
            if (product == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));

            await IncrementView(id);

            return Ok(product);
        }

        // GET: /Products/5/Details
        [Route("Products/{id}/Details")]
        [ResponseType(typeof(ProductOut))]
        public async Task<IHttpActionResult> GetProductDetails(long id)
        {
            Product product = await db.Products.FindAsync(id);
            if (product == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));

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
            productOut.Images = images;
            productOut.Categories = categories;
            productOut.Stock = product.Stock;
            productOut.Price = product.Price;
            await IncrementView(id);

            return Ok(productOut);
        }

        // GET: /Products/Own
        [Route("Products/Own/")]
        [ResponseType(typeof(ProductOut))]
        public async Task<IHttpActionResult> GetProductOwn()
        {
            CurrentIdentity identity = getIdentity();
            long shopId = (await db.Shops.Where(p => p.UserId == identity.userId).FirstOrDefaultAsync()).Id;
            List<Product> products = await db.Products.Where(p => p.ShopId == shopId).ToListAsync();
            if (products == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));

            List<Product> productsOut = new List<Product>();
            foreach (var product in products)
            {
                productsOut.Add(product);
            }

            return Ok(productsOut);
        }

        // GET: /Products/Own/Details
        [Route("Products/Own/Details")]
        [ResponseType(typeof(ProductOut))]
        public async Task<IHttpActionResult> GetProductOwnDetails()
        {
            CurrentIdentity identity = getIdentity();
            long shopId = (await db.Shops.Where(p => p.UserId == identity.userId).FirstOrDefaultAsync()).Id;
            List<Product> products = await db.Products.Where(p => p.ShopId == shopId).ToListAsync();
            if (products == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));

            List<ProductOut> productsOut = new List<ProductOut>();
            foreach (var product in products)
            {
                IList<Image> images = await db.Images.Where(p => p.ProductId == product.Id).ToListAsync();
                IList<ProductsToCategory> productToCategories = await db.ProductsToCategories.Where(p => p.ProductId == product.Id).ToListAsync();
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
                productOut.Images = images;
                productOut.Categories = categories;
                productOut.Stock = product.Stock;
                productOut.Price = product.Price;
                productsOut.Add(productOut);
            }

            return Ok(productsOut);
        }

        // PUT: /Products/5
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutProduct(long id, ProductUpdate productIn)
        {
            CurrentIdentity identity = getIdentity();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            if (productIn.IsActive != 0 && productIn.IsActive != 1)
                    return ResponseMessage(getHttpResponse(HttpStatusCode.BadRequest));

            Product productCurrent = await db.Products.FindAsync(id);

            if (productCurrent == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));
            
            string userId = (await db.Shops.FindAsync(productCurrent.ShopId)).UserId;

            if (identity.userId == userId || identity.role == "Admin")
            {
                if (productCurrent.Title != productIn.Title)
                    if (ProductTitleExistsInShop(productIn.Title, (long)productCurrent.ShopId))
                        return ResponseMessage(getHttpResponse(HttpStatusCode.Conflict));

                productCurrent.Title = (productIn.Title != null) ? productIn.Title : productCurrent.Title;
                productCurrent.Description = (productIn.Description != null) ? productIn.Description : productCurrent.Description;
                productCurrent.DescriptionFull = (productIn.DescriptionFull != null) ? productIn.DescriptionFull : productCurrent.DescriptionFull;
                productCurrent.UpdatedAt = DateTime.Now;
                productCurrent.IsActive = productIn.IsActive;
                productCurrent.Stock = productIn.Stock;
                productCurrent.Price = (productIn.Price != null) ? productIn.Price : productCurrent.Price;
                db.Entry(productCurrent).State = EntityState.Modified;

                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(id))
                    {
                        return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));
                    }
                    else
                    {
                        throw;
                    }
                }

                IList<Image> currentImages = await db.Images.Where(p => p.ProductId == id).ToListAsync();

                foreach (var image in currentImages) //TODO: Kinda hackish. Should check if images already excists instead of deleting them right off the bat.
                {
                    db.Images.Remove(image);
                }

                if (productIn.Images != null)
                {
                    foreach (var image in productIn.Images)
                    {
                        Image newImage = new Image();
                        newImage.ProductId = id;
                        newImage.ImageUrl = image;
                        db.Images.Add(newImage);
                    }

                    try
                    {
                        await db.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        throw;
                    }
                }

                IList<ProductsToCategory> currentCategories = await db.ProductsToCategories.Where(p => p.ProductId == id).ToListAsync();

                foreach (var category in currentCategories) //TODO: Kinda hackish. Should check if categories are already applied instead of deleting them right off the bat.
                {
                    db.ProductsToCategories.Remove(category);
                }
                if(productIn.CategoryId != null)
                {
                    foreach (var categoryIn in productIn.CategoryId)
                    {
                        ProductsToCategory newCategory = new ProductsToCategory();
                        newCategory.ProductId = id;
                        newCategory.CategoryId = categoryIn;
                        db.ProductsToCategories.Add(newCategory);
                    }
                }

                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    throw;
                }

                return StatusCode(HttpStatusCode.NoContent);
            }
            return ResponseMessage(getHttpResponse(HttpStatusCode.Unauthorized));
        }

        // PUT: /Products/5/Activate
        [Route("Products/{productId}/Activate")]
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(void))]
        [HttpPut]
        public async Task<IHttpActionResult> ActivateProduct(long productId, ProductIn product)
        {
            CurrentIdentity identity = getIdentity();

            Product productCurrent = await db.Products.FindAsync(productId);

            if (productCurrent == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));

            string userId = (await db.Shops.FindAsync(productCurrent.ShopId)).UserId;

            if (identity.userId == userId || identity.role == "Admin")
            {
                if(productCurrent.IsActive == 0)
                    productCurrent.IsActive = 1;
                else
                    productCurrent.IsActive = 0;

                db.Entry(productCurrent).State = EntityState.Modified;

                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(productId))
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

        // POST: /Products
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(ProductIn))]
        public async Task<IHttpActionResult> PostProduct(ProductIn productIn)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            CurrentIdentity identity = getIdentity();
            Shop shopToCheck = await db.Shops.FindAsync(productIn.ShopId);
            Product productsToCheck = await db.Products.FindAsync(productIn.ShopId);

            if (shopToCheck.UserId != identity.userId)
                if(identity.role != "Admin")
                    return ResponseMessage(getHttpResponse(HttpStatusCode.Forbidden));

            if(ProductTitleExistsInShop(productIn.Title, productIn.ShopId))
                return ResponseMessage(getHttpResponse(HttpStatusCode.Conflict));

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
            newProduct.Stock = productIn.Stock;
            newProduct.Price = productIn.Price;

            db.Products.Add(newProduct);
        
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (ProductExists(newProduct.Id))
                {
                    return ResponseMessage(getHttpResponse(HttpStatusCode.Conflict));
                }
                else
                {
                    throw;
                }
            }

            if (productIn.Images != null)
            {
                foreach (var image in productIn.Images)
                {
                    Image newImage = new Image();
                    newImage.ProductId = newProduct.Id;
                    newImage.ImageUrl = image;
                    db.Images.Add(newImage);
                }

                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    throw;
                }
            }

            if(productIn.CategoryId != null)
            {
                foreach (var category in productIn.CategoryId)
                {
                    ProductsToCategory newCategory = new ProductsToCategory();
                    newCategory.ProductId = newProduct.Id;
                    newCategory.CategoryId = category;
                    db.ProductsToCategories.Add(newCategory);
                }

                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    throw;
                }
            }

            return CreatedAtRoute("WSApi", new { id = newProduct.Id }, newProduct);
        }

        // DELETE: /Products/5
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(Product))]
        public async Task<IHttpActionResult> DeleteProduct(long id)
        {
            CurrentIdentity identity = getIdentity();

            Product product = await db.Products.FindAsync(id);
            if (product == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));
            
            Shop shop = await db.Shops.FindAsync(product.ShopId);
            IList <Image> images = await db.Images.Where(p => p.ProductId == id).ToListAsync();
            IList<ProductsToCategory> categories = await db.ProductsToCategories.Where(p => p.ProductId == id).ToListAsync();
            if (identity.userId == shop.UserId || identity.role == "Admin")
            {
                db.Products.Remove(product);
                await db.SaveChangesAsync();
                foreach (var image in images)
                {
                    db.Images.Remove(image);
                }
                foreach (var category in categories)
                {
                    db.ProductsToCategories.Remove(category);
                }
                await db.SaveChangesAsync();
                return Ok(product);
            }
            return ResponseMessage(getHttpResponse(HttpStatusCode.Unauthorized));
        }

        private async Task IncrementView(long id)
        {
            Product productCurrent = await db.Products.FindAsync(id);
            productCurrent.Views = productCurrent.Views + 1;
            db.Entry(productCurrent).State = EntityState.Modified;

            try
            {   
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }
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
                    reasonPhrase = "Product ID not present in database.";
                    break;
                case HttpStatusCode.Conflict:
                    reasonPhrase = "Shop already has a product with that title.";
                    break;
                case HttpStatusCode.Forbidden:
                    reasonPhrase = "Not owner of provided shop ID.";
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