using System.Threading.Tasks;

namespace ManagerServer.HttpHandlers.Businesses.Business
{
    [ProtoContract]
    internal sealed class ViewObjectFile : BusinessHandler
    {
        [ProtoMember(1)] public System.Guid Key;

        public override async Task Get()
        {
            if (!ApplicationData.Businesses.Exists(Business))
            {
                Response.StatusCode = 404;
                return;
            }

            var file = ApplicationData.Businesses.GetImage(Business, Key);
            if (file == null)
            {
                Response.StatusCode = 404;
                return;
            }

            Response.ContentType = file.Item2;
            Response.Headers["Content-Disposition"] = "inline";
            await Response.Body.WriteAsync(file.Item1, 0, file.Item1.Length);
        }
    }
}
