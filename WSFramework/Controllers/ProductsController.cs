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
        public float Price { get; set; }
    }

    public class ProductsController : ApiController
    {
        private WSFrameworkDBEntitiesFull db = new WSFrameworkDBEntitiesFull();

        // GET: /Products
        public IQueryable<Product> GetProducts()
        {
            return db.Products;
        }

        // GET: /Products/5
        [ResponseType(typeof(ProductOut))]
        public async Task<IHttpActionResult> GetProduct(long id)
        {
            Product product = await db.Products.FindAsync(id);
            if (product == null)
                return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.NotFound, "Product ID not present in database."));

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

        // PUT: /Products/5
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutProduct(long id, ProductUpdate productIn)
        {
            IdentityHelper identity = getIdentity();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            if (productIn.IsActive != 0 && productIn.IsActive != 1)
                    return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.BadRequest, "IsActive can only be 0 or 1. 0 is inactive. 1 is active."));

            Product productCurrent = await db.Products.FindAsync(id);

            if (productCurrent == null)
                return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.NotFound, "Product ID not present in database."));
            
            string userId = (await db.Shops.FindAsync(productCurrent.ShopId)).UserId;

            if (identity.userId == userId || identity.role == "Admin")
            {
                if (productCurrent.Title != productIn.Title)
                    if (ProductTitleExistsInShop(productIn.Title, (long)productCurrent.ShopId))
                        return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.Conflict, "Product title already present in shop."));

                productCurrent.Title = (productIn.Title != null) ? productIn.Title : productCurrent.Title;
                productCurrent.Description = (productIn.Description != null) ? productIn.Description : productCurrent.Description;
                productCurrent.DescriptionFull = (productIn.DescriptionFull != null) ? productIn.DescriptionFull : productCurrent.DescriptionFull;
                productCurrent.UpdatedAt = DateTime.Now;
                productCurrent.IsActive = productIn.IsActive;
                productCurrent.Stock = productIn.Stock;
                productCurrent.Price = productIn.Price;
                db.Entry(productCurrent).State = EntityState.Modified;

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

                }

                return StatusCode(HttpStatusCode.NoContent);
            }
            return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.Forbidden, "You are not authorized to modify this data."));
        }

        // POST: /Products
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(Product))]
        public async Task<IHttpActionResult> PostProduct(ProductIn productIn)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
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
                    return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.Conflict, "Product ID already present in database."));
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

                }
            }

            return CreatedAtRoute("WSApi", new { id = newProduct.Id }, newProduct);
        }

        // DELETE: /Products/5
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(Product))]
        public async Task<IHttpActionResult> DeleteProduct(long id)
        {
            IdentityHelper identity = getIdentity();

            Product product = await db.Products.FindAsync(id);
            if (product == null)
                return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.NotFound, "Product ID not present in database."));
            
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
            return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.Forbidden, "You are not authorized to modify this data."));
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