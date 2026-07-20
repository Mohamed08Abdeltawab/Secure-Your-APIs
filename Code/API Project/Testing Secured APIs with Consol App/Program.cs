using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Security;
using System.Threading.Tasks;

namespace StudentApiConsoleClient
{
    class Program
    {
        // ==========================
        // Configuration
        // ==========================

        // Base URL of the secured Student API
        private const string BaseUrl = "https://localhost:7217";

        // Test credentials that already exist in the API
        private const string Email = "ali.ahmed@student.com";
        private const string Password = "password1";

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Student API Console Client (JWT) ===");
            Console.WriteLine();

            var http = CreateHttpClientForLocalDev(BaseUrl);

            //retrieve token 
            //step 1
            var token = await LoginAndGetTokenAsync(http, Email, Password);

            //check if token is not null and have value
            if(string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("Field Login.");
                return;
            }

            //if login success
            Console.WriteLine("Login succeeded.");
            Console.WriteLine($"Token (first 30 chars): {token[..30]}...");
            Console.WriteLine();

            //step 2
            //called secure endpoint without token
            Console.WriteLine("Calling GET /api/Students/All WITHOUT token...");
            await CallGetAllStudentsAsync(http, null);
            Console.WriteLine();


            //step 3
            //called secure endpoint without token
            Console.WriteLine("Calling GET /api/Students/All With token...");
            await CallGetAllStudentsAsync(http, token);
            Console.WriteLine();
        }

        // ==========================
        // Helper Methods
        // ==========================

        static HttpClient CreateHttpClientForLocalDev(string baseUrl)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    (message, certificate, chain, sslErrors) =>
                        sslErrors == SslPolicyErrors.None ||
                        sslErrors == SslPolicyErrors.RemoteCertificateChainErrors
            };

            return new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl)
            };
        }

        static async Task<string> LoginAndGetTokenAsync(HttpClient http, string email, string password)
        {
            //creat request of login request
            var request = new LoginRequest
            {
                Email = email,
                Password = password
            };

            //creat value for response 
            var response = await http.PostAsJsonAsync("/api/Auth/login", request);

            //check status code
            if(response.StatusCode == HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("Invalid credentials.");
                return "";
            }

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Login failed: {response.StatusCode}");
                return "";
            }

            //get token value
            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
            return tokenResponse?.Token ?? "";
        }

        static async Task CallGetAllStudentsAsync(HttpClient http, string token)
        {
            //creat request 
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/Students/All");

            //if token is not null get authorize of this token and allow 
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            //creat response 
            var response = await http.SendAsync(request);

            //check from status code
            if(response.StatusCode == HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("401 Unauthorized");
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Request failed: {response.StatusCode}");
                return;
            }

            var students = await response.Content.ReadFromJsonAsync<List<StudentDto>>();
            Console.WriteLine($"{students.Count} students returned:");
            foreach (var s in students)
            {
                Console.WriteLine($"- {s.Name} (Age: {s.Age}, Grade: {s.Grade})");
            }
        }
    }

    // ==========================
    // DTOs
    // ==========================

    class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    class TokenResponse
    {
        public string Token { get; set; }
    }

    class StudentDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public int Grade { get; set; }
    }
}