namespace ElectronicObserver.Services.ApiFileService
{
	public class ApiFileData
	{
		public string ApiName { get; }
		public string RequestBody { get; }
		public string ResponseBody { get; }

		public ApiFileData(string apiName, string requestBody, string responseBody)
		{
			ApiName = apiName;
			RequestBody = requestBody;
			ResponseBody = responseBody;
		}
	}
}
