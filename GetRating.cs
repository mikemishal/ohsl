using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;  
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.WebJobs.Host;
using System.Linq;

namespace Company.Function
{
        public class ratingg
        {
            public string id {get; set; }
            public string userId {get; set;}
            public string productId {get; set;}
            public DateTime timestamp {get; set;}
            public string locationName {get; set; }
            public string rating {get; set; }
            public string userNotes {get; set; }

    }
    public static class GetRating
    {
        [FunctionName("GetRating")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get",
                Route = null)]HttpRequest req,
            [CosmosDB(
                databaseName: "openhack",
                collectionName: "rating",
                ConnectionStringSetting = "CosmosDbConnectionString")] DocumentClient client,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string ratingId = req.Query["ratingId"];
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            ratingId = ratingId ?? data?.ratingId;

            if (string.IsNullOrWhiteSpace(ratingId))
            {
                return (ActionResult)new NotFoundResult();
            }

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("openhack", "rating");

            log.LogInformation($"Searching for: {ratingId}");
            
            var option = new FeedOptions { EnableCrossPartitionQuery = true };

            IDocumentQuery<ratingg> query = client.CreateDocumentQuery<ratingg>(collectionUri,option)
                .Where(p => p.id.Contains(ratingId))
                .AsDocumentQuery();
                
            if (query.Equals(null))
            {
                return (ActionResult)new NotFoundResult();
            }
/*             while (query.HasMoreResults)
            {
                foreach (ratingg result in await query.ExecuteNextAsync())
                {
                    log.LogInformation(result.id);
                    return new OkObjectResult(result);
                }
            }  */
            //return new OkObjectResult(ratingId);
            return new OkObjectResult(query);
        }
    }
}