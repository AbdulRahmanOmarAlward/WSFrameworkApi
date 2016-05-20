using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;

namespace WSFramework.Providers
{
    public static class HttpResponseGenerator
    {
        public static HttpResponseMessage getHttpResponse(HttpStatusCode statusCode, string reasonPhrase)
        {
            HttpResponseMessage resp = new HttpResponseMessage();
            resp.StatusCode = statusCode;
            resp.ReasonPhrase = reasonPhrase;
            return resp;
        }
    }
}