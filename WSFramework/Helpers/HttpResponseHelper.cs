using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;

namespace WSFramework.Helpers
{
    public static class HttpResponseHelper
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