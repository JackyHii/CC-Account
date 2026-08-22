using System.Threading.Tasks;
using ManagerServer.Model;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace ManagerServer.HttpHandlers.Businesses.Business.SalesInvoices
{
    [ProtoContract]
    internal sealed class CreateCustomer : BusinessHandler
    {
        public override async Task Post()
        {
            if (!GetCurrentUserPermissions(Business).CanCreate(typeof(ManagerServer.HttpHandlers.Businesses.Business.Customers.CustomerForm).Namespace))
            {
                Response.StatusCode = 403;
                await Response.WriteAsync("You don't have permission to create customers.");
                return;
            }

            if (!Request.HasFormContentType)
            {
                Response.StatusCode = 400;
                await Response.WriteAsync("Customer name is required.");
                return;
            }

            var form = await Request.ReadFormAsync();
            var name = form["Name"].ToString().Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                Response.StatusCode = 400;
                await Response.WriteAsync("Customer name is required.");
                return;
            }

            var customer = new Customer
            {
                Key = Guid.CreateVersion7(),
                Name = name
            };
            ApplicationData.Businesses.Process(Business, customer, GetUserName());

            Response.ContentType = "application/json; charset=utf-8";
            await Response.WriteAsync(JsonConvert.SerializeObject(new
            {
                customer.Key,
                customer.UniqueName,
                customer.Currency,
                customer.HasDefaultDueDateDays,
                customer.DefaultDueDateDays,
                customer.HasDefaultBillingAddress,
                customer.DefaultBillingAddress,
                customer.CustomFields2
            }));
        }
    }
}
