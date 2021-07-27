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

namespace Company.Function

    {public class ratings
        {
            public string id {get; set; }
            public string userId {get; set;}
            public string productId {get; set;}
            public DateTime timestamp {get; set;}
            public string locationName {get; set; }
            public string rating {get; set; }
            public string userNotes {get; set; }

    }
    public static class CreateRating
    {
       [FunctionName("CreateRating")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: "openhack",
                collectionName: "rating",
                ConnectionStringSetting = "CosmosDbConnectionString")]IAsyncCollector<dynamic> documentsOut,
            ILogger log)
        {
            string responseMessage;
            log.LogInformation("Parsing current Data");

            string name = req.Query["name"];
            string userId = req.Query["userId"];
            string productId = req.Query["productId"];
            string locationName = req.Query["locationName"];
            string rating = req.Query["rating"];
            string userNotes = req.Query["userNotes"];
            string curuId = req.Query["userId"];
            string curpId = req.Query["productId"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            ratings ratings = new ratings();

            name = name ?? data?.name;
            userId = userId ??data?.userId;
            productId = productId ??data?.productId;
            locationName = locationName ??data?.locationName;
            rating = rating ??data?.rating;
            userNotes = userNotes ??data?.userNotes;

            log.LogInformation("Making calls to validate Product Id");

            string APIUrlp = "https://serverlessohproduct.trafficmanager.net/api/GetProducts?productId="+productId;
            HttpClient clientp = new HttpClient();
            clientp.BaseAddress = new Uri(APIUrlp);
            clientp.DefaultRequestHeaders.Accept.Clear();
            clientp.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage responsep = await clientp.GetAsync(APIUrlp);
            var readTaskp = responsep.Content.ReadAsStringAsync().ConfigureAwait(false);
            var rawResponsep = readTaskp.GetAwaiter().GetResult();
            bool testp = rawResponsep.Contains(productId);
            var statusCodep = responsep.StatusCode;

            log.LogInformation("Making calls to validate User Id");

            string APIUrlu = "https://serverlessohuser.trafficmanager.net/api/GetUser?userid="+userId;
            HttpClient clientu = new HttpClient();
            clientu.BaseAddress = new Uri(APIUrlu);
            clientu.DefaultRequestHeaders.Accept.Clear();
            clientu.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage responseu = await clientu.GetAsync(APIUrlu);
            var readTasku = responseu.Content.ReadAsStringAsync().ConfigureAwait(false);
            var rawResponseu = readTasku.GetAwaiter().GetResult();
            bool testu = rawResponseu.Contains(userId);
            var statusCodeu = responseu.StatusCode;

            log.LogInformation("Check if rating is within range");
            int ratingn = Convert.ToInt32(rating);
            bool testn = false;
            if (ratingn <= 5 && ratingn >=1)
            {
                testn = true;
            };
            if (testu & testp & testn)
            {
                var id = System.Guid.NewGuid().ToString();
                var timestamp = DateTime.Now;
                ratings.id = id;
                ratings.userId = userId;
                ratings.productId = productId;
                ratings.timestamp = timestamp;
                ratings.locationName = locationName;
                ratings.rating = rating;
                ratings.userNotes = userNotes;                
                // Add a JSON document to the output container.
                await documentsOut.AddAsync(ratings);
                string json = JsonConvert.SerializeObject(ratings);
                return new OkObjectResult(json);

            }else if(!testp || !testu){
                responseMessage = "UserId or ProductId not found";
                //return(ActionResult)new BadRequestResult();
                return new OkObjectResult(responseMessage);
            }else if(!testn){
                responseMessage = "Rating should be between 1 and 5";
                return new OkObjectResult(responseMessage);
            };

            return (ActionResult)new OkResult();

        }
    }
}
