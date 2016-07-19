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
    public class CustomersController : ApiController, WSFController
    {
        private WSFrameworkDBEntitiesFull db = new WSFrameworkDBEntitiesFull();
        public class CustomerIn
        {
            public string Name { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public string Address { get; set; }
            public string Zip { get; set; }
            public string City { get; set; }
            public string Country { get; set; }
            public long ShopId { get; set; }
        }

        // GET: /Customers
        [Authorize(Roles = "Admin, User")]
        public IQueryable<Customer> GetCustomers()
        {
            CurrentIdentity identity = getIdentity();

            if(identity.role == "Admin")
            {
                return db.Customers;
            }
            else
            {
                long shopId = ((Shop)db.Shops.FirstOrDefault(p => p.UserId == identity.userId)).Id;
                return db.Customers.Where(p => p.ShopId == shopId);
            }
        }

        // GET: /Customers/5
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(Customer))]
        public async Task<IHttpActionResult> GetCustomer(long id)
        {
            CurrentIdentity identity = getIdentity();

            Customer customer = await db.Customers.FindAsync(id);
            if (customer == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));

            if (identity.role == "Admin")
            {
                return Ok(customer);
            }
            else
            {
                long shopId = ((Shop) await db.Shops.FirstOrDefaultAsync(p => p.UserId == identity.userId)).Id;
                if(customer.ShopId == shopId)
                {
                    return Ok(customer);
                }
                else
                {
                    return ResponseMessage(getHttpResponse(HttpStatusCode.Unauthorized));

                }
            }
        }

        // GET: /Customers/5/Orders
        [Route("Customers/{id}/Orders")]
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(IList<Order>))]
        public async Task<IHttpActionResult> GetCustomerOrders(long id)
        {
            CurrentIdentity identity = getIdentity();
             
            if (identity.role == "Admin")
            {
                return Ok(await db.Orders.Where(p => p.CustomerId == id).ToListAsync());
            }
            else
            {
                Customer customer = await db.Customers.FindAsync(id);
                if (customer == null)
                    return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));
                long shopId = ((Shop)await db.Shops.FirstOrDefaultAsync(p => p.UserId == identity.userId)).Id;
                if (customer.ShopId == shopId)
                {
                    return Ok(await db.Orders.Where(p => p.CustomerId == id).ToListAsync());
                }
                else
                {
                    return ResponseMessage(getHttpResponse(HttpStatusCode.Unauthorized));

                }
            }
        }

        // PUT: /Customers/5
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutCustomer(long id, CustomerIn customerIn)
        {
            CurrentIdentity identity = getIdentity();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            Customer customerCurrent = await db.Customers.FindAsync(id);
            if (customerCurrent == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));

            if (identity.role != "Admin")
                if (customerCurrent.ShopId != (await db.Shops.FirstOrDefaultAsync(p => p.UserId == identity.userId)).Id)
                    return ResponseMessage(getHttpResponse(HttpStatusCode.Unauthorized));

            customerCurrent.Name = (customerIn.Name != null) ? customerIn.Name : customerCurrent.Name;
            customerCurrent.Phone = (customerIn.Phone != null) ? customerIn.Phone : customerCurrent.Phone;        
            customerCurrent.Email = (customerIn.Email != null) ? customerIn.Email : customerCurrent.Email;
            customerCurrent.Address = (customerIn.Address != null) ? customerIn.Address : customerCurrent.Address;
            customerCurrent.Zip = (customerIn.Zip != null) ? customerIn.Zip : customerCurrent.Zip;
            customerCurrent.City = (customerIn.City != null) ? customerIn.City : customerCurrent.City;
            customerCurrent.Country = (customerIn.Country != null) ? customerIn.Country : customerCurrent.Country;
            customerCurrent.ShopId = customerCurrent.ShopId;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: /Customers
        [ResponseType(typeof(Customer))]
        public async Task<IHttpActionResult> PostCustomer(CustomerIn customer)
        {

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            Customer newCustomer = new Customer();
            newCustomer.Name = customer.Name;
            newCustomer.Phone = customer.Phone;
            newCustomer.Email = customer.Email;
            newCustomer.Address = customer.Address;
            newCustomer.Zip = customer.Zip;
            newCustomer.City = customer.City;
            newCustomer.Country = customer.Country;
            newCustomer.ShopId = customer.ShopId;

            db.Customers.Add(newCustomer);

            try 
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw;
            }

            return CreatedAtRoute("WSApi", new { id = newCustomer.Id }, newCustomer);
        }

        // DELETE: /Customers/5
        [Authorize(Roles = "Admin, User")]
        [ResponseType(typeof(Customer))]
        public async Task<IHttpActionResult> DeleteCustomer(long id)
        {
            CurrentIdentity identity = getIdentity();
            Customer customer = await db.Customers.FindAsync(id);
            if (customer == null)
                return ResponseMessage(getHttpResponse(HttpStatusCode.NotFound));

            if (identity.role == "Admin")
            {
                db.Customers.Remove(customer);
                await db.SaveChangesAsync();
                return Ok(customer);
            }
            else
            {
                long shopId = ((Shop)await db.Shops.FirstOrDefaultAsync(p => p.UserId == identity.userId)).Id;
                if (customer.ShopId == shopId)
                {
                    db.Customers.Remove(customer);
                    await db.SaveChangesAsync();
                    return Ok(customer);
                }
                else
                {
                    return ResponseMessage(getHttpResponse(HttpStatusCode.Unauthorized));
                }
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
                    reasonPhrase = "Customer ID not present in database.";
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