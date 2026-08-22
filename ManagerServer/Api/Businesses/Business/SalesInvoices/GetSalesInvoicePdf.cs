using ManagerServer.Endpoints;
using Microsoft.AspNetCore.Http;

namespace ManagerServer.Api.Businesses.Business.SalesInvoices
{
    [ProtoContract]
    [ProducesContent("application/pdf", typeof(byte[]))]
    internal sealed class GetSalesInvoicePdf : ViewEndpoint<IResult>
    {
        public override IResult AuthorizedHandle()
        {
            if (!Key.HasValue) throw new BadRequestException("Sales invoice key is required.");

            var source = new GetSalesInvoiceView
            {
                Business = Business,
                Key = Key,
                Language = Language,
                Referrer = Referrer,
                Context = Context
            };
            var view = ViewMapper.From(source.AuthorizedHandle());
            if (view == null) return Results.NotFound();

            var database = GetApplicationData().Businesses.Get(Business);
            var businessDetails = database.Single<Model.BusinessDetails>();
            var logo = GetApplicationData().Businesses.GetImage(Business, businessDetails.Key)?.Item1;
            var pdf = new ManagerServer.Pdf.SalesInvoicePdfRenderer(view, logo).Generate();

            return Results.File(pdf, contentType: "application/pdf", enableRangeProcessing: true);
        }
    }
}
