using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using WSFramework.Helpers;
using WSFramework.Models;

namespace WSFramework.Controllers
{
    public class CategoriesController : ApiController
    {
        private WSFrameworkDBEntitiesFull db = new WSFrameworkDBEntitiesFull();

        public class CategoryUpdate
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public string imageUrl { get; set; }
            public int IsActive { get; set; }
        }

        public class CategoryIn
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public string imageUrl { get; set; }
        }

        // GET: /Categories
        public IQueryable<Category> GetCategories()
        {
            return db.Categories;
        }

        // GET: /Categories/5
        [ResponseType(typeof(Category))]
        public async Task<IHttpActionResult> GetCategory(long id)
        {
            Category category = await db.Categories.FindAsync(id);
            if (category == null)
                return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.NotFound, "Category ID not present in database."));

            return Ok(category);
        }

        // PUT: /Categories/5
        [Authorize(Roles = "Admin")]
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutCategory(long id, CategoryUpdate categoryIn)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (categoryIn.IsActive != 0)
                if (categoryIn.IsActive != 1)
                    return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.BadRequest, "IsActive can only be 0 or 1. 0 is inactive. 1 is active."));

            Category categoryCurrent = await db.Categories.FindAsync(id);

            if (categoryCurrent == null)
                return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.NotFound, "Category ID not present in database."));

            if (categoryCurrent.Title != categoryIn.Title)
                if (CategoryTitleExists(categoryIn.Title))
                    return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.Conflict, "Category title already present in database."));

            categoryCurrent.Title = (categoryIn.Title != null) ? categoryIn.Title : categoryCurrent.Title;
            categoryCurrent.Description = (categoryIn.Description != null) ? categoryIn.Description : categoryCurrent.Description;
            categoryCurrent.UpdatedAt = DateTime.Now;
            categoryCurrent.IsActive = categoryIn.IsActive;
            categoryCurrent.Image = (categoryIn.imageUrl != null) ? categoryIn.imageUrl : categoryCurrent.Image;
            db.Entry(categoryCurrent).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
                {
                    return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.NotFound, "Category ID not present in database."));
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: /Categories
        [Authorize(Roles = "Admin")]
        [ResponseType(typeof(Category))]
        public async Task<IHttpActionResult> PostCategory(CategoryIn categoryIn)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (CategoryTitleExists(categoryIn.Title))
                return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.Conflict, "Category title already present in database."));

            Category newCategory = new Category();
            newCategory.Title = categoryIn.Title;
            newCategory.Description = categoryIn.Description;
            newCategory.Image = categoryIn.imageUrl;
            newCategory.IsActive = 0;
            DateTime now = DateTime.Now;
            newCategory.CreatedAt = now;
            newCategory.UpdatedAt = now;

            db.Categories.Add(newCategory);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (CategoryExists(newCategory.Id))
                {
                    return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.Conflict, "Category ID already present in database."));
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtRoute("WSApi", new { id = newCategory.Id }, newCategory);
        }

        // DELETE: /Categories/5
        [Authorize(Roles = "Admin")]
        [ResponseType(typeof(Category))]
        public async Task<IHttpActionResult> DeleteCategory(long id)
        {
            Category category = await db.Categories.FindAsync(id);
            if (category == null)
                return ResponseMessage(HttpResponseHelper.getHttpResponse(HttpStatusCode.NotFound, "Category ID not present in database."));

            db.Categories.Remove(category);
            await db.SaveChangesAsync();

            return Ok(category);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool CategoryExists(long id)
        {
            return db.Categories.Count(e => e.Id == id) > 0;
        }

        private bool CategoryTitleExists(string title)
        {
            return db.Categories.Count(e => e.Title == title) > 0;
        }

    }
}