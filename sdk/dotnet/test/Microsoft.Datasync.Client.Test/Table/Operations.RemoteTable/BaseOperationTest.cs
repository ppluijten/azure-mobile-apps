﻿// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using Datasync.Common.Test;
using Datasync.Common.Test.Mocks;
using Datasync.Common.Test.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text;
using Xunit;

namespace Microsoft.Datasync.Client.Test.Table.Operations.RemoteTable
{
    [ExcludeFromCodeCoverage]
    public class BaseOperationTest : BaseTest
    {
        protected readonly IdEntity payload = new() { Id = "db0ec08d-46a9-465d-9f5e-0066a3ee5b5f", StringValue = "test" };
        protected const string sPayload = "{\"id\":\"db0ec08d-46a9-465d-9f5e-0066a3ee5b5f\",\"stringValue\":\"test\"}";
        protected const string sBadJson = "{this-is-bad-json";

        protected readonly IRemoteTable table, authTable;
        protected readonly IdOnly idOnly;
        protected readonly IdEntity idEntity;
        protected readonly JObject jIdEntity, jIdOnly;
        protected readonly string sId, sIdEntity, sIdOnly, expectedEndpoint, tableEndpoint;

        public BaseOperationTest() : base()
        {
            sId = Guid.NewGuid().ToString("N");
            expectedEndpoint = new Uri(Endpoint, $"/tables/movies/{sId}").ToString();
            tableEndpoint = new Uri(Endpoint, "/tables/movies").ToString();

            // The entity, JObject, and JSON string for an IdOnly object.
            idOnly = new IdOnly { Id = sId };
            jIdOnly = CreateJsonDocument(idOnly);
            sIdOnly = jIdOnly.ToString(Formatting.None);

            // The entity, JObject, and JSON string for an IdEntity object.
            idEntity = new IdEntity { Id = sId, Version = "etag" };
            jIdEntity = CreateJsonDocument(idEntity);
            sIdEntity = jIdEntity.ToString(Formatting.None);

            table = GetMockClient().GetRemoteTable("movies");
            authTable = GetMockClient(new MockAuthenticationProvider(ValidAuthenticationToken)).GetRemoteTable("movies");
        }

        /// <summary>
        /// Checks that the provided result matches the payload.
        /// </summary>
        /// <param name="response">The result received.</param>
        protected void AssertJsonMatches(JToken response)
        {
            Assert.NotNull(response);
            Assert.IsAssignableFrom<JObject>(response);
            var result = (JObject)response;

            Assert.Equal(payload.Id, result.Value<string>("id"));
            Assert.Equal(payload.StringValue, result.Value<string>("stringValue"));
        }

        /// <summary>
        /// Check that the request is the correct method and path.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="endpoint"></param>
        protected HttpRequestMessage AssertSingleRequest(HttpMethod method, string endpoint)
        {
            Assert.Single(MockHandler.Requests);
           return AssertRequest(MockHandler.Requests[0], method, endpoint);
        }

        /// <summary>
        /// Checks that the specified request is the right method and path
        /// </summary>
        /// <param name="request"></param>
        /// <param name="method"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        protected static HttpRequestMessage AssertRequest(HttpRequestMessage request, HttpMethod method, string endpoint)
        {
            Assert.Equal(method, request.Method);
            Assert.Equal(endpoint, request.RequestUri.ToString());
            AssertEx.HasHeader(request.Headers, "ZUMO-API-VERSION", "3.0.0");
            return request;
        }

        /// <summary>
        /// Returns bad JSON response.
        /// </summary>
        /// <param name="statusCode"></param>
        protected void ReturnBadJson(HttpStatusCode statusCode)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(sBadJson, Encoding.UTF8, "application/json")
            };
            MockHandler.Responses.Add(response);
        }

        /// <summary>
        /// A set of invalid IDs for testing
        /// </summary>
        public static IEnumerable<object[]> GetInvalidIds() => new List<object[]>
        {
            new object[] { "" },
            new object[] { " " },
            new object[] { "\t" },
            new object[] { "abcdef gh" },
            new object[] { "!!!" },
            new object[] { "?" },
            new object[] { ";" },
            new object[] { "{EA235ADF-9F38-44EA-8DA4-EF3D24755767}" },
            new object[] { "###" }
        };
    }
}
