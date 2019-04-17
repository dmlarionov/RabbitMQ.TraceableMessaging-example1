using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using lib;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Linq;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using lib.DTO;

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
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            var config = configBuilder.Build();

            // connect to RabbitMQ
            var conn = (new ConnectionFactory()
                {
                    HostName = config["RabbitMQ:Connection:Hostname"],
                    UserName = config["RabbitMQ:Connection:Username"],
                    Password = config["RabbitMQ:Connection:Password"],
                    VirtualHost = config["RabbitMQ:Connection:Vhost"]
                })
                .CreateConnection();

            // wait peers to be ready
            var waitPeers = ReadyChecker.WaitPeersAsync(config["RabbitMQ:Exchanges:ReadyCheck"], conn, new List<string> { "apigw", "bang", "bar", "fib", "foo" });

            // get App Insights instrumentation key from CLI
            Console.WriteLine("Please enter your Application Insights instrumentation key: ");
            var instrumKey = Console.ReadLine();

            // generate key for signing tokens
            var tokenKey = GenKey();

            // distribute keys
            await KeyDistributor.ConfigurePeersAsync(config["RabbitMQ:Exchanges:KeyDistribution"], conn, new Keys { AppInsightsInstrumKey = instrumKey, JwtIssuerKey = tokenKey });

            // if peers are ready
            if (await Task.WhenAny(waitPeers, Task.Delay(2000)) == waitPeers)
            {
                // Task completed within timeout.
                // Consider that the task may have faulted or been canceled.
                // We re-await the task so that any exceptions/cancellation is rethrown.
                await waitPeers;

                bool shutdown = false;

                // perform demo scenarios based on user choise
                while (!shutdown)
                {
                    switch (GetCase())
                    {
                        case '1':
                            RunCase1();
                            break;
                        case '2':
                            RunCase2();
                            break;
                        case '3':
                            RunCase3();
                            break;
                        case '4':
                            RunCase4();
                            break;
                        case 'q':
                            await Shutdowner.ShutdownPeersAsync(config["RabbitMQ:Exchanges:Shutdown"], conn);
                            shutdown = true;
                            break;
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

        static char GetCase()
        {
            Console.WriteLine();
            Console.WriteLine("Which scenario should we test with API gateway?: ");
            Console.WriteLine("1) HTTP GET by unauthorized user (401 Unauthorized propagated from one of RabbitMQ services)");
            Console.WriteLine("2) HTTP GET by authorized unprivileged user (403 Forbidden propagated from one of RabbitMQ services)");
            Console.WriteLine("3) HTTP GET by authorized privileged user (200 OK)");
            Console.WriteLine("4) HTTP GET by authorized privileged unlucky user (500 Internal Server Error due to failure in one of services)");
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

        static void RunCase1()
        {

        }

        static void RunCase2()
        {

        }

        static void RunCase3()
        {

        }

        static void RunCase4()
        {

        }

        static byte[] GenKey()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return Encoding.ASCII.GetBytes(new string(Enumerable.Repeat(chars, 256).Select(s => s[random.Next(s.Length)]).ToArray()));
        }

        static string GenToken(byte[] key, string name)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, name)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
