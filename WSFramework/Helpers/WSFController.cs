using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WSFramework.Helpers
{
    interface WSFController
    {
        HttpResponseMessage getHttpResponse(HttpStatusCode statusCode);
    }
}
