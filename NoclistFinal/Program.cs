using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Noclist
{


    public class Program
    {
        //Object handling sending and receiving requests to server 
        static HttpClient client = new HttpClient();

        //Used to verify the application only tries a connection 3 times before giving up. Mainly used for testing purposes
        static public int retries { get; set; }

        //Used to hold the Response after sending Request to /auth
        static public HttpResponseMessage authmessage { get; set; }

        //Used to hold the Response after sending Request to /users
        static public HttpResponseMessage usermessage { get; set; }


        //static public string jsonstring { get; set; }

        //Used to hold the URI received from the user
        static public string baseadd { get; set; }


        //Tries to send GET request to server
        //If successful, checks to see if Response StatusCode=200
        //If unsuccessful, retries 2 more time(for a total of 3 times)
        //If still unsuccessful, returns a bad HTTPResponseMessage
        //If Response StatusCode=200, retunrs ResponseMessage
        //int retry=Holds num of times application tries to send GET request
        //bool success=Holds whether or not Response StatusCode=200
        //token= Holds a succssful Response Message if there is one. Sets StatusCode to "bad" value is thrown
        static public async Task<HttpResponseMessage> GetAuthToken(string add)
        {
            int retry = 0;
            bool success = false;

            HttpResponseMessage token = new HttpResponseMessage();

            while (retry < 3 && success == false)
            {
                try
                {
                    token = await client.GetAsync(add + "/auth");
                    if (token.StatusCode != HttpStatusCode.OK)
                    {
                        retry = retry;
                    }
                    else
                    {
                        success = true;
                    }
                }
                catch (Exception ex)
                {
                    token.StatusCode = HttpStatusCode.BadGateway;
                    return token;
                }
                retry++;
            }
            retries = retry;
            return token;

        }


        //Takes the authentication token from the /auth response
        //Concatenates the token and = "/users"
        //Converts it to Byte array(ComputeHash takes Byte array as argument)
        //Computes hash
        //Converts hash into string
        //Input:
        //authtoken-Authentication token from /auth ResponseHeader
        //Output:
        //checksum.ToString()-sha256(authtoken+endpoint) converted into a string
        static public string GetChecksum(string authtoken)
        {
            string plaintext = authtoken + "/users";
            UTF8Encoding encoder = new UTF8Encoding();
            Byte[] plaintextbyte = encoder.GetBytes(plaintext);

            StringBuilder checksum = new StringBuilder();
            using (SHA256 hash = SHA256.Create())
            {


                Byte[] crypt = hash.ComputeHash(plaintextbyte);

                foreach (Byte b in crypt)
                {
                    checksum.Append(b.ToString("x2"));
                }
            }
            return checksum.ToString();

        }


        //Inserts "X-Request-Checksum" and sha256(<auth_token>+<requet_path>) as key-value pair into Headers
        //Inputs:
        //string checksum-sha256(<auth_token>+<requet_path>)
        //HttpClient c- Used to get the default headers sent with Requests
        static public void InsertHeader(string checksum, HttpClient c)
        {
            HttpRequestHeaders headers = c.DefaultRequestHeaders;
            headers.Add("X-Request-Checksum", checksum);
        }


        //Tries to send GET request to server
        //If successful, checks to see if Response StatusCode=200
        //If unsuccessful, retries 2 more time(for a total of 3 times)
        //If still unsuccessful, returns a bad HTTPResponseMessage
        //If Response StatusCode=200, returns ResponseMessage
        //int retry=Holds num of times application tries to send GET request
        //bool success=Holds whether or not Response StatusCode=200
        //users= Holds a succssful Response Message if there is one. Sets StatusCode to "bad" value is thrown
        static public async Task<HttpResponseMessage> GetUsers(string add)
        {
            int retry = 0;
            bool success = false;

            HttpResponseMessage users = new HttpResponseMessage();
            while (retry < 3 && success == false)
            {
                try
                {
                    users = await client.GetAsync(add + "/users");
                    if (users.StatusCode != HttpStatusCode.OK)
                    {
                        retry = retry;
                    }
                    else
                    {
                        success = true;
                    }
                }
                catch (Exception ex)
                {
                    users.StatusCode = HttpStatusCode.BadGateway;
                    return users;
                }
                retry++;
            }
            retries = retry;

            return users;

        }


        //Takes ResponseMessage from GET Requeste to /users endpoint
        //Formats the body into JSON by:
        //  Starting with ["
        //  Iterating over the body, appending each char until \n is found
        //  Once found, function appends ", " to JSON
        //  Repeats until end of body is reached
        //  Appends ] to end of JSON
        //  Writes JSON out to Console
        //Input: 
        //HttpResponseMessage msg- The response message from the GET request to /users
        static public async void CreateJSON(HttpResponseMessage msg)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[\"");
            int start = 0;
            char compare = '\n';
            string unsctruct = await msg.Content.ReadAsStringAsync();
            while (start != unsctruct.Length - 1)
            {
                if (compare != unsctruct[start])
                {
                    sb.Append(unsctruct[start]);
                    start++;
                }
                else
                {
                    sb.Append("\", \"");
                    start++;
                }
            }

            sb.Append("\"]");

            Console.WriteLine(sb);

        }

        //Checks to see if user supplied valid input
        //If so, application continues
        //Also returns StatusCode=-1 for failures and StatusCode=
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("A URI was not provided. Please run the application again with a URI.");
                return -1;
            }
            else if (args.Length > 1)
            {
                Console.WriteLine("Too many arguments were provided. Please restart and supply only one argument.");
                return -1;

            }
            else if (args.Length == 1)
            {
                Uri test;
                bool isValid = Uri.TryCreate(args[0], UriKind.Absolute, out test) && test.Scheme == Uri.UriSchemeHttp;
                if (!isValid)
                {
                    Console.WriteLine("Uri not valid. Please restart the application with a valid URI.");
                    return -1;
                }
                else
                {
                    baseadd = args[0];
                }
            }

            int status = RunAsync().GetAwaiter().GetResult();
            return status;

        }


        //Sets base URI for Requests and Responses
        //Calls all aformentioned functions to:
        //  Get Authentication token from /auth Response Header
        //  Hashes header+ /users endpoint and converts hash into stirng
        //  Inserts new header key and value into Request Headers
        //  Gets ResponseMessage from /users
        //  Converts Response Message body into JSON and output it to Console
        static async Task<int> RunAsync()
        {
            client.BaseAddress = new Uri("http://localhost:8888/");
            authmessage = await GetAuthToken("http://localhost:8888");
            //client.BaseAddress = new Uri(baseadd);
            //authmessage = await GetAuthToken(baseadd);
            retries = 0;
            if (authmessage.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("There was a problem getting authenticated. Please check your credentials and try again.");
                return -1;
            }
            //Console.WriteLine(token.Headers.GetValues("Badsec-Authentication-Token").FirstOrDefault());
            string authtoken = authmessage.Headers.GetValues("Badsec-Authentication-Token").FirstOrDefault();
            string checksum = GetChecksum(authtoken);
            InsertHeader(checksum, client);
            //usermessage=await GetUsers("http://localhost:8888");
            usermessage = await GetUsers(baseadd);
            if (usermessage.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("There was a problem getting the UserIds.");
                return -1;
            }
            retries = 0;
            CreateJSON(usermessage);
            return 0;





        }
    }
}

