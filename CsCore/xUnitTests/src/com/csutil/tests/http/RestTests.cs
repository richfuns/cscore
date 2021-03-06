using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using com.csutil.http;
using Xunit;

namespace com.csutil.tests.http {

    public class RestTests {

        [Fact]
        public async void ExampleUsage1() {
            RestRequest request = new Uri("https://httpbin.org/get").SendGET();
            // Send the request and parse the response into the HttpBinGetResp class:
            HttpBinGetResp response = await request.GetResult<HttpBinGetResp>();
            Log.d("Your external IP is " + response.origin);
        }

        public class HttpBinGetResp { // The https://httpbin.org/get json as a class
            public string origin { get; set; }
            public Dictionary<string, object> headers { get; set; }
        }

        [Fact]
        public async void TestSendGET1() {
            await new Uri("https://httpbin.org/get").SendGET().GetResult<HttpBinGetResp>((x) => {
                Assert.NotNull(x);
                Log.d("Your external IP is " + x.origin);
                Assert.NotNull(x.origin);
            });
        }

        [Fact]
        public async void TestSendGET2() {
            RestRequest request = new Uri("https://httpbin.org/get").SendGET();
            await ValidateResponse(request);
        }

        [Fact]
        public async void TestRestFactory1() {
            RestRequest request = RestFactory.instance.SendRequest(new Uri("https://httpbin.org/get"), HttpMethod.Get);
            await ValidateResponse(request);
        }

        private static async Task ValidateResponse(RestRequest request) {
            var includedRequestHeaders = new Dictionary<string, string>();
            includedRequestHeaders.Add("Aaa", "aaa 1");
            includedRequestHeaders.Add("Bbb", "bbb 2");
            request.WithRequestHeaders(new Headers(includedRequestHeaders));
            var response = await request.GetResult<HttpBinGetResp>();
            Assert.NotNull(response);
            Log.d("Your external IP is " + response.origin);
            Assert.NotNull(response.origin);

            Log.d("response.headers contain the following elements:");
            foreach (var h in response.headers) { Log.d(" > " + h.Key + " (with value " + h.Value + ")"); }

            foreach (var sentHeader in includedRequestHeaders) {
                Log.d("Now looking for " + sentHeader.Key + " (with value " + sentHeader.Value + ")");
                Assert.Equal(sentHeader.Value, response.headers.First(x => x.Key.Equals(sentHeader.Key)).Value);
            }
        }

        [Fact]
        public static async void TestGetCurrentPing() {
            var pingInMs = await RestFactory.instance.GetCurrentPing();
            Assert.NotEqual(-1, pingInMs);
            Assert.True(0 < pingInMs && pingInMs < 500, "pingInMs=" + pingInMs);
        }

    }

}