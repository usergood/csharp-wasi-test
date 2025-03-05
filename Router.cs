using System.Text;  
using ProxyWorld.wit.imports.wasi.http.v0_2_0;
using ProxyWorld.wit.imports.wasi.io.v0_2_0;
namespace ProxyWorld.wit.exports.wasi.http.v0_2_0;

public class IncomingHandlerImpl: IIncomingHandler
{
    public static void Handle(ITypes.IncomingRequest request, ITypes.ResponseOutparam responseOut)
    {
        ITypes.Method method = request.Method();
        #pragma warning disable CS8602 // Possible null reference argument.
        var schema = request.Scheme().Tag switch {
            ITypes.Scheme.Tags.Http => "http://",
            ITypes.Scheme.Tags.Https => "https://",
            _ => "",
        };
        #pragma warning restore CS8602 // Possible null reference argument.
        var uri = new Uri(schema + request.Authority()+request.PathWithQuery());
        
        var path_without_query = uri.GetLeftPart(UriPartial.Path).Replace(schema + request.Authority(), "");
        var queryParams = uri.Query.TrimStart('?').Split('&').Select(q => q.Split('=')).ToDictionary(kv => kv[0], kv => kv.Length > 1 ? kv[1] : null);

        var method_string = method.Tag switch
            {
                ITypes.Method.Tags.Connect => "Connect",
                ITypes.Method.Tags.Delete => "Delete",
                ITypes.Method.Tags.Get => "Get",
                ITypes.Method.Tags.Head => "Head",
                ITypes.Method.Tags.Options => "Options",
                ITypes.Method.Tags.Patch => "Patch",
                ITypes.Method.Tags.Post => "Post",
                ITypes.Method.Tags.Put => "Put",
                ITypes.Method.Tags.Trace => "Trace",
                _ => ""
            };

        var content = Encoding.ASCII.GetBytes($"Not found, path => {path_without_query} method => {method_string}");
        var content_type = "text/plain";

        // router paths there

        var headers = new List<(string, byte[])> {
            ("content-type", Encoding.ASCII.GetBytes(content_type)),
            ("content-length", Encoding.ASCII.GetBytes(content.Count().ToString()))
        };
        var response = new ITypes.OutgoingResponse(ITypes.Fields.FromList(headers));
        var body = response.Body();
        ITypes.ResponseOutparam.Set(responseOut, Result<ITypes.OutgoingResponse, ITypes.ErrorCode>.Ok(response));
        using (var stream = body.Write()) {
            stream.BlockingWriteAndFlush(content);
        }
        ITypes.OutgoingBody.Finish(body, null);
    }

    public static string ParseBodyAsString(ITypes.IncomingRequest request) 
    {
        ITypes.IncomingBody body = request.Consume();
        IStreams.InputStream stream = body.Stream(); 
        
        ulong len = 4096;
        var result = new List<byte>();
        byte[] buffer;

        do
        {
            buffer = stream.BlockingRead(len);
            result.AddRange(buffer);
        } while (buffer.Length == (int)len);

        if (result.Count == 0) {
            return "";
        }
        return Encoding.UTF8.GetString(result.ToArray());
    }
}
