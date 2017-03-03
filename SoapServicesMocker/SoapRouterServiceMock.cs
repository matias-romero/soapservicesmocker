using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace SoapServicesMocker
{
    public class SoapRouterServiceMock : IDisposable
    {
        private readonly Uri _testRunnerServerUri;
        private UnitaryWebServer _loopbackHttpServer;
        private IDictionary<string, Tuple<Func<string, bool>, Action<HttpListenerResponse>>> _soapActionRules = new Dictionary<string, Tuple<Func<string, bool>, Action<HttpListenerResponse>>>();

        public SoapRouterServiceMock(string webServiceAppEndpoint)
        {
            if (!webServiceAppEndpoint.StartsWith("/"))
                webServiceAppEndpoint = "/" + webServiceAppEndpoint;
            if (!webServiceAppEndpoint.EndsWith("/"))
                webServiceAppEndpoint = webServiceAppEndpoint + "/";

            _testRunnerServerUri = UnitaryWebServer.ResolveLocalRandomPrefix(webServiceAppEndpoint);
            _loopbackHttpServer = new UnitaryWebServer(_testRunnerServerUri, this.RouteResponse);
            _loopbackHttpServer.Run();
        }

        public Uri WebServiceEndpoint
        {
            get { return _testRunnerServerUri; }
        }

        private void SendSoapXmlResponse(HttpListenerResponse response, string Xml)
        {
            response.ContentEncoding = Encoding.UTF8;
            response.ContentType = "text/xml";
            //response.SetCookie(new Cookie("AuthenticationCookie", "abcabc"));
            var bytes = Encoding.UTF8.GetBytes(Xml);
            response.Close(bytes, true);
        }

        public void Dispose()
        {
            if (_loopbackHttpServer != null)
            {
                _loopbackHttpServer.Stop();
                _loopbackHttpServer.Dispose();
                _loopbackHttpServer = null;
            }
        }

        private void RouteResponse(HttpListenerRequest request, HttpListenerResponse response)
        {
            var soapAction = request.Headers["SOAPAction"];
            var receivedEntity = new StreamReader(request.InputStream, request.ContentEncoding).ReadToEnd();

            if (_soapActionRules.ContainsKey(soapAction))
            {
                var tuple = _soapActionRules[soapAction];
                if (tuple.Item1.Invoke(receivedEntity))
                    tuple.Item2.Invoke(response);
            }
        }

        public void ConfigureResponseForSOAPAction(string fullyQualifiedSoapAction, string xmlResponse)
        {
            _soapActionRules[fullyQualifiedSoapAction] = Tuple.Create<Func<string, bool>, Action<HttpListenerResponse>>(AlwaysPredicate, (res) => SendSoapXmlResponse(res, xmlResponse));
        }

        private bool AlwaysPredicate(string entityBody) { return true; }
    }
}
