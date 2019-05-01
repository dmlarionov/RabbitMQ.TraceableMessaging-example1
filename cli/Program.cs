using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using lib;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Linq;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using lib.DTO;
using System.Net;
using System.Net.Http.Headers;

namespace cli
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string basePath = (Debugger.IsAttached) ? Directory.GetCurrentDirectory() : AppDomain.CurrentDomain.BaseDirectory;
            
            // build config
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
            var config = configBuilder.Build();

            // connect to RabbitMQ
            var conn = Utils.ConnectRabbitMQ(config);
            
            // wait peers to be ready
            var waitPeers = ReadyChecker.WaitPeersAsync(config["RabbitMQ:Exchanges:ReadyCheck"], conn, new List<string> { "apigw", "bang", "bar", "fib", "foo" });

            // get App Insights instrumentation key from CLI
            Console.WriteLine("Please enter your Application Insights instrumentation key: ");
            var instrumKey = Console.ReadLine();

            // generate key and token
            var tokenKey = GenKey();

            // distribute keys
            await KeyDistributor.ConfigurePeersAsync(config["RabbitMQ:Exchanges:KeyDistribution"], conn, new Keys { AppInsightsInstrumKey = instrumKey, JwtIssuerKey = tokenKey });

            // if peers are ready
            if (await Task.WhenAny(waitPeers, Task.Delay(10000)) == waitPeers)
            {
                // Task completed within timeout.
                // Consider that the task may have faulted or been canceled.
                // We re-await the task so that any exceptions/cancellation is rethrown.
                await waitPeers;

                bool shutdown = false;

                // perform demo scenarios based on user choise
                using (var http = new HttpClient())
                {
                    http.BaseAddress = new Uri(config["ApiGW:Url"]);
                    while (!shutdown)
                    {
                        try
                        {
                            string url = null;
                            switch (MainDialog())
                            {
                                case '1':
                                    url = "api/demo/fooping1";
                                    break;
                                case '2':
                                    url = "api/demo/barping2";
                                    break;
                                case '3':
                                    url = "api/demo/fibping3";
                                    break;
                                case '4':
                                    url = "api/demo/fibping4";
                                    break;
                                case 'q':
                                    shutdown = true;
                                    break;
                            }
                            if (shutdown)
                                await Shutdowner.ShutdownPeersAsync(config["RabbitMQ:Exchanges:Shutdown"], conn);
                            else
                            {
                                HttpResponseMessage resp = await http.GetAsync(url);
                                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                                {
                                    var username = LoginDialog();
                                    var token = GenToken(tokenKey, username);
                                    http.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);
                                    Console.WriteLine($"Bearer JWT for user '{username}' will be sent in the header of every request since now.");
                                }
                                else
                                {
                                    resp.EnsureSuccessStatusCode();
                                    Console.WriteLine();
                                    Console.WriteLine("----------------------------------------");
                                    Console.WriteLine($"HTTP GET '/{url}' completed at {DateTime.Now.ToString()}.");
                                    Console.WriteLine("----------------------------------------");
                                }
                            }
                        }
                        catch (HttpRequestException ex)
                        {
                            Console.WriteLine();
                            Console.WriteLine("----------------------------------------");
                            Console.WriteLine("Message: " + ex.Message);
                            Console.WriteLine("----------------------------------------");
                        }
                    }
                }
            }
            else
            {
                // timeout/cancellation logic
                Console.WriteLine();
                Console.WriteLine("Sorry, but some services are not ready for this demo. Check if docker is running all containters of docker-compose.yml.");
                await Shutdowner.ShutdownPeersAsync(config["RabbitMQ:Exchanges:Shutdown"], conn);
                return;
            }
            Console.WriteLine("Bye!");
        }

        static char MainDialog()
        {
            Console.WriteLine();
            Console.WriteLine("Which scenario should we test with API gateway?: ");
            Console.WriteLine("1) HTTP GET /api/demo/fooping1");
            Console.WriteLine("2) HTTP GET /api/demo/barping2");
            Console.WriteLine("3) HTTP GET /api/demo/fibping3 (Internal Server Error)");
            Console.WriteLine("4) HTTP GET /api/demo/fibping4 (Forbidden)");
            Console.WriteLine();
            Console.WriteLine("Each HTTP call to API gateway turnes into series of RabbitMQ calls. You can see a distributed trace in your Application Insights instance in Azure portal. You can test scenarios in any order.");
            Console.WriteLine();
            Console.WriteLine("To quit press 'q'.");
            Console.WriteLine();

            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey();
                switch (key.KeyChar)
                {
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case 'q':
                        return key.KeyChar;
                    default:
                        Console.WriteLine("Sorry, inappropriate choice. Only 'q' and scenario number allowed.");
                        break;
                }
            }
            while (true);
        }

        static string LoginDialog()
        {
            Console.WriteLine();
            Console.WriteLine("Sir, API asks for your name. What should I tell it?: ");
            return Console.ReadLine();
        }

        static byte[] GenKey()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return Encoding.ASCII.GetBytes(new string(Enumerable.Repeat(chars, 256).Select(s => s[random.Next(s.Length)]).ToArray()));
        }

        static string GenToken(byte[] key, string username)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim("scope", "1"),
                    new Claim("scope", "2"),
                    new Claim("scope", "3"),
                    new Claim("scope", "4")
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
