using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Noclist;
namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        static HttpClient client = new HttpClient();
        static string badbase = "http://1.2.3.4:8888";
        static string goodbase = "http://localhost:8888";
        static string googlebase = "http://google.com/auth";

        [TestMethod]
        //Verifies reception of ResponseMessage with valid connection(StatusCode=200)
        public async Task GetAuthTokenWithGoodURI()
        {
            client.BaseAddress = new Uri(goodbase);

            HttpResponseMessage actual = await Noclist.Program.GetAuthToken(goodbase);
            Assert.AreEqual(HttpStatusCode.OK, actual.StatusCode);

        }

        [TestMethod]
        //Verifies X-Request-Checksum header is added to RequestMessage
        public void VerifyChecksumHeader()
        {
            client.BaseAddress = new Uri(goodbase);

            Noclist.Program.InsertHeader("12345", client);

            bool headeradded = true;
            try
            {
                client.DefaultRequestHeaders.GetValues("X-Request-Checksum");
            }
            catch (Exception ex)
            {
                headeradded = false;
            }

            Assert.AreEqual(true, headeradded);

        }

        [TestMethod]
        //Verifies Application stops trying to connect after 3 non-200 responses (/auth)
        public async Task ThreeFailsonAuth()
        {
            client.BaseAddress = new Uri(googlebase);
            HttpResponseMessage response = await Noclist.Program.GetAuthToken(googlebase);
            int retries = Noclist.Program.retries;

            Assert.AreEqual(3, retries);

        }

        [TestMethod]
        //Verifies Application stops trying to connect after 3 non-200 responses 9
        public async Task ThreeFailsonUser()
        {
            client.BaseAddress = new Uri(googlebase);
            HttpResponseMessage respone = await Noclist.Program.GetUsers(googlebase);
            int retries = Noclist.Program.retries;

            Assert.AreEqual(3, retries);
        }
    }
}

