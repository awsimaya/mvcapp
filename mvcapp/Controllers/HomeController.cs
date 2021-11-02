﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using mvcapp.Models;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;


using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using System.Net.Http;
using System.Configuration;


namespace mvcapp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _config;
        private readonly HttpClient httpClient = new HttpClient();

        AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        

        private static readonly Counter TickTock =
       Metrics.CreateCounter("sampleapp_ticks_total", "Just keeps on ticking");

  
        public HomeController(ILogger<HomeController> logger, IConfiguration config )
        {
            _logger = logger;
            _config = config;
        }

        public IActionResult Index()
        {
            TickTock.Inc();
            return View();
        }

        public IActionResult InvokeAPI()
        {
            _logger.LogInformation($"{GetTraceId()}Hello from InvokeAPI");
            _logger.LogError($"{GetTraceId()}Some error :(");

            string apigw_url;

            apigw_url = Environment.GetEnvironmentVariable("APIGW_URL");

            if (apigw_url == null)
                apigw_url = _config.GetValue<string>("APIGW_URL");

            var result = httpClient.GetAsync(apigw_url).Result;

            ViewData["apiresponse"] = result.Content.ReadAsStringAsync().Result;
            return View();
        }

        public IActionResult Privacy()
        {
            Console.Write(Activity.Current.TraceId.ToHexString());

           // AmazonDynamoDBClient client = new AmazonDynamoDBClient();

            var request = new QueryRequest
            {
                TableName = "grocery",
                KeyConditionExpression = "item_type = :v_type",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                    {":v_type", new AttributeValue { S =  "Apple" }}}
            };

            var response = client.QueryAsync(request).Result;

            foreach (Dictionary<string, AttributeValue> item in response.Items)
            {
                // Process the result.
                Console.WriteLine(item);
            }


            return View();

        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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
