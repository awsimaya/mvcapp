using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using mvcapp.Models;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime.Documents;
using Microsoft.AspNetCore.Http;


namespace mvcapp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _config;
        private readonly HttpClient httpClient = new HttpClient();

        private AmazonDynamoDBClient _client;

        private static readonly Counter TickTock =
            Metrics.CreateCounter("sampleapp_ticks_total", "Just keeps on ticking");

        private static readonly Gauge apiResponse =
            Metrics.CreateGauge("api_response", "Keeps track of the api response time");

        public HomeController(ILogger<HomeController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;

            AWSCredentials _awsCredentials;

            if (new CredentialProfileStoreChain().TryGetAWSCredentials("ijaganna+playground1-Admin-OneClick",
                out _awsCredentials))
            {
                _client = new AmazonDynamoDBClient(_awsCredentials, RegionEndpoint.USEast2);
            }
            else
            {
                _client = new AmazonDynamoDBClient(RegionEndpoint.USEast2);
            }
        }


        public IActionResult Index()
        {
            TickTock.Inc();
            return View();
        }

        public IActionResult InvokeAPI()
        {
            _logger.LogInformation($"{GetTraceId()}Hello from InvokeAPI");


            string apigw_url = Environment.GetEnvironmentVariable("APIGW_URL");

            if (apigw_url == null)
                apigw_url = _config.GetValue<string>("APIGW_URL");

            _logger.LogInformation($"APIGW URL : {apigw_url}");

            var timeBeforeCall = DateTime.Now;

            // if (new Random().Next(0, 6) > 3)
            // {
            //     Task.Delay(3000);
            //     _logger.LogWarning($"{GetTraceId()} Something is slowing down your API response.");
            // }

            var result = httpClient.GetAsync(apigw_url).Result;

            var latency = DateTime.Now - timeBeforeCall;

            apiResponse.Set(latency.TotalMilliseconds);

            ViewData["apiresponse"] = result.Content.ReadAsStringAsync().Result;
            ViewData["apiResponseTime"] = latency.TotalMilliseconds;
            
            return View();
        }

        public IActionResult Groceries(string q = "Apple")
        {
            var request = new QueryRequest
            {
                TableName = "grocery",
                KeyConditionExpression = "item_type = :v_type",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":v_type", new AttributeValue {S = q}}
                }
            };

            var result = _client.QueryAsync(request).Result;

            try
            {
                _logger.LogInformation($"{GetTraceId()} Number of keys present - {result.Items.First().Keys.Count}"); 
            }
            catch (Exception e)
            {
                _logger.LogError($"{GetTraceId()} - {e.Message} - {e.StackTrace}"); 

                throw;
            }
            return View(result);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }

        private string GetTraceId()
        {
            var traceId = Activity.Current.TraceId.ToHexString();
            var version = "1";
            var epoch = traceId.Substring(0, 8);
            var random = traceId.Substring(8);
            return "{" + "\"traceId\"" + ": " + "\"" + version + "-" + epoch + "-" + random + "\"" + "}";
        }
    }
}