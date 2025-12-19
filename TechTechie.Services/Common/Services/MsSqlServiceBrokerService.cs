using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Net;
using System.Text;
using System.Xml.Linq;


namespace TechTechie.Services.Common.Services
{
    public class MsSqlServiceBrokerService : BackgroundService
    {
        private readonly ILogger<MsSqlServiceBrokerService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private string _connectionString;
        private int _maxWorkers;
        private readonly string _logFilePath;
        private readonly bool _useFileLogging;

        public MsSqlServiceBrokerService(
            ILogger<MsSqlServiceBrokerService> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;

            _connectionString = _configuration.GetConnectionString("sqlconn")
                ?? throw new InvalidOperationException("Connection string TechTechieDB not found");


            // Read configuration from BrokerService section
            _maxWorkers = _configuration.GetValue<int>("ServiceBroker:MaxWorkers", 10);
            _logFilePath = _configuration.GetValue<string>("ServiceBroker:LogFilePath");

            // Only enable file logging if path is provided and valid
            _useFileLogging = !string.IsNullOrEmpty(_logFilePath);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            if (string.IsNullOrEmpty(_connectionString))
            {
                LogMessage("TechTechie - MS SQL Database Connection not found");
                return;
            }
            LogMessage("TechTechie - Service Broker HTTP Processor started");

            //LogMessage($"Connection: {_connectionString}");
            LogMessage($"Workers: {_maxWorkers}");

            LogMessage($"Service Broker HTTP Processor started with {_maxWorkers} workers");

            var workers = new Task[_maxWorkers];
            for (int i = 0; i < _maxWorkers; i++)
            {
                int workerId = i;
                workers[i] = Task.Run(() => ProcessMessagesAsync(workerId, stoppingToken), stoppingToken);
            }

            try
            {
                await Task.WhenAll(workers);
            }
            catch (OperationCanceledException)
            {
                LogMessage("Service shutdown requested");
            }
            catch (Exception ex)
            {
                LogMessage($"Fatal error in service execution {ex.Message}");
                throw;
            }

            LogMessage("Service Broker HTTP Processor stopped");
        }

        private async Task ProcessMessagesAsync(int workerId, CancellationToken stoppingToken)
        {

            LogMessage($"Worker {workerId} started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessSingleMessageAsync(workerId, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LogMessage($"Worker {workerId} error: {ex.Message}");
                    await Task.Delay(1000, stoppingToken);
                }
            }

            LogMessage($"Worker {workerId} stopped");
        }

        private async Task ProcessSingleMessageAsync(int workerId, CancellationToken stoppingToken)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync(stoppingToken);

                using (SqlCommand cmd = new SqlCommand(@"
                     WAITFOR
                    (
                        RECEIVE TOP (1)
                            conversation_handle,
                            message_type_name,
                            CAST(message_body AS XML) AS message_body
                        FROM dbo.HttpResponseQueue 
                    ), TIMEOUT 5000", conn))
                {
                    cmd.CommandTimeout = 10;

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync(stoppingToken))
                    {
                        if (await reader.ReadAsync(stoppingToken))
                        {
                            Guid handle = reader.GetGuid(0);
                            string messageType = reader.GetString(1);
                            string xmlBody = reader.GetString(2);
                            reader.Close();

                            if (messageType == "HttpRequestMessage")
                            {
                                await ProcessHttpRequestAsync(conn, handle, xmlBody, workerId, stoppingToken);
                            }
                        }
                    }
                }
            }
        }

        private async Task ProcessHttpRequestAsync(
    SqlConnection conn,
    Guid handle,
    string xmlRequest,
    int workerId,
    CancellationToken stoppingToken)
        {
            string responseBody = "";
            int statusCode = 200;
            DateTime startTime = DateTime.Now;
            string url = "";

            try
            {
                XDocument doc = XDocument.Parse(xmlRequest);
                url = doc.Root.Element("Url")?.Value ?? "";
                string method = doc.Root.Element("Method")?.Value ?? "GET";
                string contentType = doc.Root.Element("ContentType")?.Value;
                string headersString = doc.Root.Element("Headers")?.Value;
                string body = doc.Root.Element("Body")?.Value;
                string requestId = doc.Root.Element("RequestId")?.Value;

                LogMessage($"Worker {workerId}: {method} {url} (ID: {requestId})");

                string methodUpper = method.ToUpper();

                // Handle special methods that don't make HTTP calls
                switch (methodUpper)
                {
                    case "ENCRYPT":
                        (responseBody, statusCode) = await HandleEncryptAsync(body, workerId);
                        break;

                    case "DECRYPT":
                        (responseBody, statusCode) = await HandleDecryptAsync(body, workerId);
                        break;

                    case "BASE64":
                        (responseBody, statusCode) = await HandleStringToBase64Async(body, workerId);
                        break;

                    case "GET":
                    case "POST":
                    case "PUT":
                    case "PATCH":
                    case "DELETE":
                    case "HEAD":
                    case "OPTIONS":
                        (responseBody, statusCode) = await ExecuteHttpRequestAsync(
                            methodUpper, url, contentType, headersString, body,
                            workerId, startTime, stoppingToken);
                        break;

                    default:
                        responseBody = $"Unsupported method: {method}";
                        statusCode = 400;
                        LogMessage($"Worker {workerId}: Unsupported method {method}");
                        break;
                }
            }
            catch (HttpRequestException httpEx)
            {
                // Match CLR error format
                if (httpEx.InnerException is WebException webEx)
                {
                    using (WebResponse response = webEx.Response)
                    {
                        if (response != null)
                        {
                            using (Stream DataError = response.GetResponseStream())
                            using (StreamReader reader = new StreamReader(DataError))
                            {
                                string errorText = await reader.ReadToEndAsync();
                                responseBody = $"Error in url : {url} , Response : {errorText}";

                                if (response is HttpWebResponse httpResponse)
                                    statusCode = (int)httpResponse.StatusCode;
                                else
                                    statusCode = 500;
                            }
                        }
                        else
                        {
                            responseBody = $"Error in url : {url} , No response received.";
                            statusCode = 500;
                        }
                    }
                }
                else
                {
                    responseBody = $"Error in url : {url} , Response : {httpEx.Message}";
                    statusCode = 500;
                }


                LogMessage($"Worker {workerId}: HTTP Error - {responseBody}");
            }
            catch (TaskCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                responseBody = $"Error in url : {url} , No response received.";
                statusCode = 408;

                LogMessage($"Worker {workerId}: Timeout");
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                responseBody = $"Error in url : {url} {ex.Message}";
                statusCode = 500;
                LogMessage($"Worker {workerId}: Exception - {ex.Message}");
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                await SendResponseAsync(conn, handle, responseBody, statusCode, stoppingToken);
            }
        }

        private async Task<(string responseBody, int statusCode)> ExecuteHttpRequestAsync(
            string method,
            string url,
            string contentType,
            string headersString,
            string body,
            int workerId,
            DateTime startTime,
            CancellationToken stoppingToken)
        {
            var httpClient = _httpClientFactory.CreateClient("HttpBrokerClient");
            var request = new HttpRequestMessage(new HttpMethod(method), url);

            // Set HTTP version with fallback
            request.Version = new Version(2, 0);
            request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

            // Set User-Agent
            request.Headers.TryAddWithoutValidation("User-Agent", "Tech-Techie/2.0");

            // Parse and add custom headers
            if (!string.IsNullOrEmpty(headersString))
            {
                foreach (var headerItem in headersString.Split(';'))
                {
                    if (string.IsNullOrWhiteSpace(headerItem)) continue;

                    var parts = headerItem.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        string name = parts[0].Trim();
                        string value = parts[1].Trim();
                        request.Headers.TryAddWithoutValidation(name, value);
                    }
                }
            }

            // Add Accept header for GET
            if (method == "GET" && !request.Headers.Contains("Accept"))
            {
                request.Headers.TryAddWithoutValidation("Accept", "application/json");
            }

            // Add body for methods that support it
            if (!string.IsNullOrEmpty(body) && (method == "POST" || method == "PUT" || method == "PATCH"))
            {
                request.Content = new StringContent(body, System.Text.Encoding.UTF8,
                    contentType ?? "application/json");
            }

            // Execute request
            using (HttpResponseMessage response = await httpClient.SendAsync(request, stoppingToken))
            {
                int statusCode = (int)response.StatusCode;
                string responseBody = await response.Content.ReadAsStringAsync(stoppingToken);

                TimeSpan duration = DateTime.Now - startTime;
                string protocol = response.Version.ToString();

                LogMessage($"Worker {workerId}: Success - Status {statusCode}, Protocol: HTTP/{protocol}, Duration: {duration.TotalMilliseconds}ms");

                return (responseBody, statusCode);
            }
        }

        private async Task<(string responseBody, int statusCode)> HandleEncryptAsync(string Data, int workerId)
        {
            try
            {


                string encrypted = "";

                LogMessage($"Worker {workerId}: Encryption completed");

                return (encrypted, 200);
            }
            catch (Exception ex)
            {
                LogMessage($"Worker {workerId}: Encryption failed -  {ex.Message}");
                return ($"Encryption error: {ex.Message}", 500);
            }
        }

        private async Task<(string responseBody, int statusCode)> HandleDecryptAsync(string Data, int workerId)
        {
            try
            {

                string dencrypted = "";

                LogMessage($"Worker {workerId}: Decryption completed");

                return (dencrypted, 200);
            }
            catch (Exception ex)
            {
                LogMessage($"Worker {workerId}: Decryption failed-  {ex.Message}");
                return ($"Decryption error: {ex.Message}", 500);
            }
        }

        private async Task<(string responseBody, int statusCode)> HandleStringToBase64Async(string Data, int workerId)
        {
            try
            {
                string result = "";

                if (!string.IsNullOrEmpty(Data))
                {
                    var byteArray = new UTF8Encoding().GetBytes(Data);
                    result = Convert.ToBase64String(byteArray);

                }

                LogMessage($"Worker {workerId}: StringToBase64 completed");

                return (result, 200);
            }
            catch (Exception ex)
            {
                LogMessage($"Worker {workerId}: StringToBase64 failed- {ex.Message}");
                return ($"Decryption error: {ex.Message}", 500);
            }
        }


        private async Task SendResponseAsync(
            SqlConnection conn,
            Guid handle,
            string responseBody,
            int statusCode,
            CancellationToken stoppingToken)
        {
            try
            {
                XDocument responseDoc = new XDocument(
                    new XElement("Response",
                        new XElement("StatusCode", statusCode),
                        new XElement("Body", new XCData(responseBody))
                    )
                );

                string responseXml = responseDoc.ToString();

                using (SqlCommand cmd = new SqlCommand(@"
                    SEND ON CONVERSATION @handle MESSAGE TYPE [HttpResponseMessage](@response);
                    END CONVERSATION @handle;", conn))
                {
                    cmd.Parameters.AddWithValue("@handle", handle);
                    cmd.Parameters.Add("@response", SqlDbType.Xml).Value = responseXml;
                    await cmd.ExecuteNonQueryAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {

                LogMessage($"Error sending response: {ex.Message}");

                try
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "END CONVERSATION @handle WITH ERROR = 500 DESCRIPTION = @error", conn))
                    {
                        cmd.Parameters.AddWithValue("@handle", handle);
                        cmd.Parameters.AddWithValue("@error", ex.Message);
                        await cmd.ExecuteNonQueryAsync(stoppingToken);
                    }
                }
                catch { }
            }
        }

        private void LogMessage(string message)
        {
            try
            {
                if (_useFileLogging)
                {


                    string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";
                    string logDir = Path.GetDirectoryName(_logFilePath);

                    if (!Directory.Exists(logDir))
                        Directory.CreateDirectory(logDir);

                    File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
                }
                else
                {
                    // Fallback to console logging if file logging is not enabled
                    _logger.LogInformation($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}");
                }
            }
            catch { }
        }
    }

}
