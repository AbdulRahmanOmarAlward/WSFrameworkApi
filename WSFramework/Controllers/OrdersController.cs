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
    public class OrdersController : ApiController, WSFController
    {
        private WSFrameworkDBEntitiesFull db = new WSFrameworkDBEntitiesFull();

        class OrderProducts
        {
            public IList<ProductOutOrder> Products { get; set; }
        }

        public class ProductOutOrder : Product
        {
            public long Quantity { get; set; }
            public IList<Image> Images { get; set; }
            public IList<Category> Categories { get; set; }
        }

        public class ProductsInOrder
        {
            public long ProductId { get; set; }
            public int Quantity { get; set; }
        }
        public class OrderUpdate
        {
            public string ShippingAddress { get; set; }
            public string BillingAddress { get; set; }
            public int Status { get; set; }
            public int Amount { get; set; }
        }
        public class OrderIn
        {
            public long CustomerId { get; set; }
            public long ShopId { get; set; }
            public string ShippingAddress { get; set; }
            public string BillingAddress { get; set; }
            public IList<ProductsInOrder> Products { get; set; }
        }

        // GET: /Orders
        [Authorize(Roles = "Admin, User")]
        public IQueryable<Order> GetOrders()
        {
            CurrentIdentity identity = getIdentity();

            if (identity.role == "Admin")
            {
                return db.Orders;
            }
            else
            {
                long shopId = ((Shop)db.Shops.FirstOrDefault(p => p.UserId == identity.userId)).Id;
                return db.Orders.Where(p => p.ShopId == shopId);
            }
        }

        // GET: /Orders/5
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(Order))]
        public async Task<IHttpActionResult> GetOrder(long id)
        {
            CurrentIdentity identity = getIdentity();

            Order order = await db.Orders.FindAsync(id);
            if (order == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));

            if (identity.role == "Admin")
            {
                return Ok(order);
            }
            else
            {
                long shopId = ((Shop)await db.Shops.FirstOrDefaultAsync(p => p.UserId == identity.userId)).Id;
                if (order.ShopId == shopId)
                {
                    return Ok(order);
                }
                else
                {
                    return ResponseMessage(getHttpResponse(HttpStatusCode.Unauthorized));

                }
            }
        }

        // GET: /Orders/5/products
        [Route("Orders/{id}/products")]
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(OrderProducts))]
        public async Task<IHttpActionResult> GetOrderProducts(long id)
        {
            CurrentIdentity identity = getIdentity();

            Order order = await db.Orders.FindAsync(id);
            if (order == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));

            if (identity.role != "Admin")
                if (order.ShopId != (await db.Shops.FirstOrDefaultAsync(p => p.UserId == identity.userId)).Id)
                    return ResponseMessage(getHttpResponse(HttpStatusCode.Unauthorized));

            OrderProducts orderOut = new OrderProducts();
            orderOut.Products = new List<ProductOutOrder>();
            IList<OrdersToProduct> productsInOrder = new List<OrdersToProduct>();
            productsInOrder = await db.OrdersToProducts.Where(p => p.OrderId == id).ToListAsync();

            foreach (var product in productsInOrder)
            {
                orderOut.Products.Add(await GetProductsForOrder(product.ProductId,product.Quantity));
            }

            return Ok(orderOut);
        }

        // PUT: /Orders/5
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutOrder(long id, OrderUpdate orderIn)
        {
            CurrentIdentity identity = getIdentity();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            Order orderCurrent = await db.Orders.FindAsync(id);
            if (orderCurrent == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));

            if (identity.role != "Admin")
                if (orderCurrent.ShopId != (await db.Shops.FirstOrDefaultAsync(p => p.UserId == identity.userId)).Id)
                    return ResponseMessage(getHttpResponse(HttpStatusCode.Unauthorized));

            orderCurrent.ShippingAddress = (orderIn.ShippingAddress != null) ? orderIn.ShippingAddress : orderCurrent.ShippingAddress;
            orderCurrent.BillingAddress = (orderIn.BillingAddress != null) ? orderIn.BillingAddress : orderCurrent.BillingAddress;
            orderCurrent.Status = orderIn.Status;
            orderCurrent.Amount = orderIn.Amount;
            orderCurrent.ShippingAddress = (orderIn.ShippingAddress != null) ? orderIn.ShippingAddress : orderCurrent.ShippingAddress;
            orderCurrent.UpdatedAt = DateTime.Now;
            db.Entry(orderCurrent).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {

            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: /Orders
        [ResponseType(typeof(Order))]
        public async Task<IHttpActionResult> PostOrder(OrderIn orderIn)
        {
            long shopId = orderIn.ShopId;

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            foreach (var products in orderIn.Products)
            {
                Product product = await db.Products.FindAsync(products.ProductId);
                if(product == null)
                    return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));

                if (product.Stock <= 0)
                    return ResponseMessage(getHttpResponse(HttpStatusCode.Gone));

                if(product.Stock - products.Quantity < 0)
                    return ResponseMessage(getHttpResponse(HttpStatusCode.Forbidden));

                if (product.ShopId != shopId)
                {
                    return ResponseMessage(getHttpResponse(HttpStatusCode.Unauthorized));
                }
            }

            foreach (var products in orderIn.Products)
            {
                await DecreaseStock(products.ProductId, products.Quantity);
            }

            Order newOrder = new Order();
            newOrder.CustomerId = orderIn.CustomerId;
            newOrder.ShippingAddress = orderIn.ShippingAddress;
            newOrder.BillingAddress = orderIn.BillingAddress;
            DateTime now = DateTime.Now;
            newOrder.CreatedAt = now;
            newOrder.UpdatedAt = now;
            newOrder.Status = 0;
            newOrder.ShopId = shopId;
            float price = 0;
            foreach (var product in orderIn.Products)
            {
                price += (float)((await db.Products.FindAsync(product.ProductId)).Price * product.Quantity);
            }
            newOrder.Amount = price;

            db.Orders.Add(newOrder);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
            }

            if (orderIn.Products != null)
            {
                foreach (var productsIn in orderIn.Products)
                {
                    OrdersToProduct newOrdersToProducts = new OrdersToProduct();
                    newOrdersToProducts.OrderId = newOrder.Id;
                    newOrdersToProducts.ProductId = productsIn.ProductId;
                    newOrdersToProducts.Quantity = productsIn.Quantity;
                    db.OrdersToProducts.Add(newOrdersToProducts);
                }
            }
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {

            }

            return CreatedAtRoute("WSApi", new { id = newOrder.Id }, newOrder);
        }

        private async Task DecreaseStock(long id, int amount)
        {
            Product productCurrent = await db.Products.FindAsync(id);
            productCurrent.Stock -= amount;
            db.Entry(productCurrent).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
            }
        }

        private async Task<ProductOutOrder> GetProductsForOrder(long id, long quantity)
        {
            Product product = await db.Products.FindAsync(id);

            IList<Image> images = await db.Images.Where(p => p.ProductId == id).ToListAsync();
            IList<ProductsToCategory> productToCategories = await db.ProductsToCategories.Where(p => p.ProductId == id).ToListAsync();
            IList<Category> categories = new List<Category>();

            foreach (var categoryEach in productToCategories)
            {
                categories.Add(await db.Categories.FirstOrDefaultAsync(p => p.Id == categoryEach.CategoryId));
            }

            ProductOutOrder productOut = new ProductOutOrder();
            productOut.Quantity = quantity;
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

        private bool OrderExists(long id)
        {
            return db.Orders.Count(e => e.Id == id) > 0;
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
                    reasonPhrase = "ID not found.";
                    break;
                case HttpStatusCode.Gone:
                    reasonPhrase = "Product sold out.";
                    break;
                case HttpStatusCode.Forbidden:
                    reasonPhrase = "Product doesn't have the required stock to complete this transaction.";
                    break;
                case HttpStatusCode.Unauthorized:
                    reasonPhrase = "You are not authorized to sell this product.";
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