# SoapServicesMocker
Integration-Tests involving SOAP WebServices made easy.

The purpose of SoapServicesMocker is to create a simple local web-server capable of return tailored responses based on diferent SOAP Actions configured for each unit-test.
There are many cases when other types of mocking are more suitable, for example if you have some kind of interfaces between your application code and the service caller code. 
But if you have "Proxy generated" classes using wsdl.exe (Classes inheriting from System.Web.Services.Protocols.SoapHttpClientProtocol especially);
Things become a bit owerwhelming because the generated code is not so "flexible" to implement some interface or another abstraction layer.


# Example Use Case #
```csharp
[TestMethod]
public void TestRealWebServiceClientCallingMockingWebServiceResponses()
{
	using (var routerMock = new SoapRouterServiceMock("/CustomServiceEndpoint"))
	{
		routerMock.ConfigureResponseForSOAPAction(
			"\"CUSTOMACTION"", //This is the Operation name matching definition in the WSDL
			"<SOAP-ENV:Envelope ....... </SOAP-ENV:Envelope>"); //The entire SOAP Response returned when calling this SOAP Action
		
		//WsdlProxy Class was built using wsdl.exe (Import Web Reference) with certain .wsdl
		var ws = new WsdlProxy(){ Url = routerMock.WebServiceEndpoint.ToString() };
		var wsTypedResponse = ws.CustomAction();
		
		//Assert....
	}
}
```