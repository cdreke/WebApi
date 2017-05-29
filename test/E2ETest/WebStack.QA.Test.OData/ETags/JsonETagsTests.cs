﻿using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Builder;
using System.Web.OData.Extensions;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;
using Microsoft.OData.Core;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Annotations;
using Microsoft.OData.Edm.Expressions;
using Microsoft.OData.Edm.Vocabularies.V1;
using Newtonsoft.Json.Linq;
using Nuwa;
using WebStack.QA.Test.OData.ModelBuilder;
using Xunit;

namespace WebStack.QA.Test.OData.ETags
{
    [NuwaFramework]
    public class JsonETagsTests
    {
        [NuwaBaseAddress]
        public string BaseAddress { get; set; }

        [NuwaHttpClient]
        public HttpClient Client { get; set; }

        [NuwaConfiguration]
        public static void UpdateConfiguration(HttpConfiguration configuration)
        {
            configuration.Routes.Clear();
            configuration.MapODataServiceRoute("odata", "odata", GetEdmModel(), new DefaultODataPathHandler(), ODataRoutingConventions.CreateDefault());
            configuration.MessageHandlers.Add(new ETagMessageHandler());
        }

        private static IEdmModel GetEdmModel()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            EntitySetConfiguration<ETagsCustomer> eTagsCustomersSet = builder.EntitySet<ETagsCustomer>("ETagsCustomers");
            SingletonConfiguration<ETagsCustomer> eTagsCustomerSingleton = builder.Singleton<ETagsCustomer>("ETagsCustomer");
            EntityTypeConfiguration<ETagsCustomer> eTagsCustomers = eTagsCustomersSet.EntityType;
            eTagsCustomers.Property(c => c.Id).IsConcurrencyToken();
            eTagsCustomers.Property(c => c.Name).IsConcurrencyToken();
            eTagsCustomers.Property(c => c.BoolProperty).IsConcurrencyToken();
            eTagsCustomers.Property(c => c.ByteProperty).IsConcurrencyToken();
            eTagsCustomers.Property(c => c.CharProperty).IsConcurrencyToken();
            eTagsCustomers.Property(c => c.DecimalProperty).IsConcurrencyToken();
            eTagsCustomers.Property(c => c.DoubleProperty).IsConcurrencyToken();
            eTagsCustomers.Property(c => c.ShortProperty).IsConcurrencyToken();
            eTagsCustomers.Property(c => c.LongProperty).IsConcurrencyToken();
            eTagsCustomers.Property(c => c.SbyteProperty).IsConcurrencyToken();
            eTagsCustomers.Property(c => c.FloatProperty).IsConcurrencyToken();
            eTagsCustomers.Property(c => c.UshortProperty).IsConcurrencyToken();
            eTagsCustomers.Property(c => c.UintProperty).IsConcurrencyToken();
            eTagsCustomers.Property(c => c.UlongProperty).IsConcurrencyToken();
            eTagsCustomers.Property(c => c.GuidProperty).IsConcurrencyToken();
            eTagsCustomers.Property(c => c.DateTimeOffsetProperty).IsConcurrencyToken();

            return builder.GetEdmModel();
        }

        [Fact]
        public void ModelBuilderTest()
        {
            const string expectedEntitySetMetadata =
                "        <EntitySet Name=\"ETagsCustomers\" EntityType=\"WebStack.QA.Test.OData.ETags.ETagsCustomer\">\r\n" +
                "          <NavigationPropertyBinding Path=\"RelatedCustomer\" Target=\"ETagsCustomers\" />\r\n" +
                "          <Annotation Term=\"Org.OData.Core.V1.OptimisticConcurrency\">\r\n" +
                "            <Collection>\r\n" +
                "              <PropertyPath>Id</PropertyPath>\r\n" +
                "              <PropertyPath>Name</PropertyPath>\r\n" +
                "              <PropertyPath>BoolProperty</PropertyPath>\r\n" +
                "              <PropertyPath>ByteProperty</PropertyPath>\r\n" +
                "              <PropertyPath>CharProperty</PropertyPath>\r\n" +
                "              <PropertyPath>DecimalProperty</PropertyPath>\r\n" +
                "              <PropertyPath>DoubleProperty</PropertyPath>\r\n" +
                "              <PropertyPath>ShortProperty</PropertyPath>\r\n" +
                "              <PropertyPath>LongProperty</PropertyPath>\r\n" +
                "              <PropertyPath>SbyteProperty</PropertyPath>\r\n" +
                "              <PropertyPath>FloatProperty</PropertyPath>\r\n" +
                "              <PropertyPath>UshortProperty</PropertyPath>\r\n" +
                "              <PropertyPath>UintProperty</PropertyPath>\r\n" +
                "              <PropertyPath>UlongProperty</PropertyPath>\r\n" +
                "              <PropertyPath>GuidProperty</PropertyPath>\r\n" +
                "              <PropertyPath>DateTimeOffsetProperty</PropertyPath>\r\n" +
                "              <PropertyPath>StringWithConcurrencyCheckAttributeProperty</PropertyPath>\r\n" +
                "            </Collection>\r\n" +
                "          </Annotation>\r\n" +
                "        </EntitySet>";

            const string expectedSingletonMetadata =
                "        <Singleton Name=\"ETagsCustomer\" Type=\"WebStack.QA.Test.OData.ETags.ETagsCustomer\">\r\n" +
                "          <NavigationPropertyBinding Path=\"RelatedCustomer\" Target=\"ETagsCustomers\" />\r\n" +
                "          <Annotation Term=\"Org.OData.Core.V1.OptimisticConcurrency\">\r\n" +
                "            <Collection>\r\n" +
                "              <PropertyPath>Id</PropertyPath>\r\n" +
                "              <PropertyPath>Name</PropertyPath>\r\n" +
                "              <PropertyPath>BoolProperty</PropertyPath>\r\n" +
                "              <PropertyPath>ByteProperty</PropertyPath>\r\n" +
                "              <PropertyPath>CharProperty</PropertyPath>\r\n" +
                "              <PropertyPath>DecimalProperty</PropertyPath>\r\n" +
                "              <PropertyPath>DoubleProperty</PropertyPath>\r\n" +
                "              <PropertyPath>ShortProperty</PropertyPath>\r\n" +
                "              <PropertyPath>LongProperty</PropertyPath>\r\n" +
                "              <PropertyPath>SbyteProperty</PropertyPath>\r\n" +
                "              <PropertyPath>FloatProperty</PropertyPath>\r\n" +
                "              <PropertyPath>UshortProperty</PropertyPath>\r\n" +
                "              <PropertyPath>UintProperty</PropertyPath>\r\n" +
                "              <PropertyPath>UlongProperty</PropertyPath>\r\n" +
                "              <PropertyPath>GuidProperty</PropertyPath>\r\n" +
                "              <PropertyPath>DateTimeOffsetProperty</PropertyPath>\r\n" +
                "              <PropertyPath>StringWithConcurrencyCheckAttributeProperty</PropertyPath>\r\n" +
                "            </Collection>\r\n" +
                "          </Annotation>\r\n" +
                "        </Singleton>";

            string requestUri = string.Format("{0}/odata/$metadata", this.BaseAddress);

            HttpResponseMessage response = this.Client.GetAsync(requestUri).Result;

            var content = response.Content.ReadAsStringAsync().Result;
            Assert.Contains(expectedEntitySetMetadata, content);
            Assert.Contains(expectedSingletonMetadata, content);

            var stream = response.Content.ReadAsStreamAsync().Result;
            IODataResponseMessage message = new ODataMessageWrapper(stream, response.Content.Headers);
            var reader = new ODataMessageReader(message);
            var edmModel = reader.ReadMetadataDocument();
            Assert.NotNull(edmModel);

            var etagCustomers = edmModel.FindDeclaredEntitySet("ETagsCustomers");
            Assert.NotNull(etagCustomers);

            var annotations = edmModel.FindDeclaredVocabularyAnnotations(etagCustomers);
            IEdmVocabularyAnnotation annotation = Assert.Single(annotations);
            Assert.NotNull(annotation);

            Assert.Same(CoreVocabularyModel.ConcurrencyTerm, annotation.Term);
            Assert.Same(etagCustomers, annotation.Target);

            IEdmValueAnnotation valueAnnotation = annotation as IEdmValueAnnotation;
            Assert.NotNull(valueAnnotation);
            Assert.NotNull(valueAnnotation.Value);

            IEdmCollectionExpression collection = valueAnnotation.Value as IEdmCollectionExpression;
            Assert.NotNull(collection);
            Assert.Equal(new[]
            {
                "Id", "Name", "BoolProperty", "ByteProperty", "CharProperty", "DecimalProperty",
                "DoubleProperty", "ShortProperty", "LongProperty", "SbyteProperty",
                "FloatProperty", "UshortProperty", "UintProperty", "UlongProperty",
                "GuidProperty", "DateTimeOffsetProperty",
                "StringWithConcurrencyCheckAttributeProperty"
            },
                collection.Elements.Select(e => ((IEdmPathExpression) e).Path.Single()));
        }

        [Fact]
        public void JsonWithDifferentMetadataLevelsHaveSameETagsTest()
        {
            string requestUri = this.BaseAddress + "/odata/ETagsCustomers";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.ParseAdd("application/json");
            HttpResponseMessage response = this.Client.SendAsync(request).Result;
            Assert.True(response.IsSuccessStatusCode);
            var jsonResult = response.Content.ReadAsAsync<JObject>().Result;
            var jsonETags = jsonResult.GetValue("value").Select(e => e["@odata.etag"].ToString());
            Assert.Equal(jsonETags.Count(), jsonETags.Distinct().Count());

            requestUri = this.BaseAddress + "/odata/ETagsCustomers";
            request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.ParseAdd("application/json;odata=nometadata");
            response = this.Client.SendAsync(request).Result;
            Assert.True(response.IsSuccessStatusCode);
            var jsonWithNometadataResult = response.Content.ReadAsAsync<JObject>().Result;
            var jsonWithNometadataETags = jsonWithNometadataResult.GetValue("value").Select(e => e["@odata.etag"].ToString());
            Assert.Equal(jsonWithNometadataETags.Count(), jsonWithNometadataETags.Distinct().Count());
            Assert.Equal(jsonETags, jsonWithNometadataETags);

            requestUri = this.BaseAddress + "/odata/ETagsCustomers";
            request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.ParseAdd("application/json;odata=fullmetadata");
            response = this.Client.SendAsync(request).Result;
            Assert.True(response.IsSuccessStatusCode);
            var jsonWithFullmetadataResult = response.Content.ReadAsAsync<JObject>().Result;
            var jsonWithFullmetadataETags = jsonWithFullmetadataResult.GetValue("value").Select(e => e["@odata.etag"].ToString());
            Assert.Equal(jsonWithFullmetadataETags.Count(), jsonWithFullmetadataETags.Distinct().Count());
            Assert.Equal(jsonETags, jsonWithFullmetadataETags);

            requestUri = this.BaseAddress + "/odata/ETagsCustomers";
            request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.ParseAdd("application/json;odata=minimalmetadata");
            response = this.Client.SendAsync(request).Result;
            Assert.True(response.IsSuccessStatusCode);
            var jsonWithMinimalmetadataResult = response.Content.ReadAsAsync<JObject>().Result;
            var jsonWithMinimalmetadataETags = jsonWithMinimalmetadataResult.GetValue("value").Select(e => e["@odata.etag"].ToString());
            Assert.Equal(jsonWithMinimalmetadataETags.Count(), jsonWithMinimalmetadataETags.Distinct().Count());
            Assert.Equal(jsonETags, jsonWithMinimalmetadataETags);
        }

        [Fact]
        public void SingletonsHaveETags()
        {
            // ETagsCustomers Entity Set
            string requestUri = this.BaseAddress + "/odata/ETagsCustomers(0)";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.ParseAdd("application/json");
            HttpResponseMessage response = this.Client.SendAsync(request).Result;
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Headers.Contains("ETag"), "Single ETagsCustomer is missing ETag Header");
            var jsonResult = response.Content.ReadAsAsync<JObject>().Result;
            var jsonETag = jsonResult.GetValue("@odata.etag");
            Assert.True(jsonETag != null, "Single ETagsCustomer from Entity Set is missing ETag");

            requestUri = this.BaseAddress + "/odata/ETagsCustomers(0)/ContainedCustomer";
            request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.ParseAdd("application/json");
            response = this.Client.SendAsync(request).Result;
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Headers.Contains("ETag"), "Selected single contained ETagsCustomer is missing ETag Header");
            jsonResult = response.Content.ReadAsAsync<JObject>().Result;
            jsonETag = jsonResult.GetValue("@odata.etag");
            Assert.True(jsonETag != null, "Selected contained ETagsCustomer missing ETag");

            requestUri = this.BaseAddress + "/odata/ETagsCustomers(0)/RelatedCustomer";
            request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.ParseAdd("application/json");
            response = this.Client.SendAsync(request).Result;
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Headers.Contains("ETag"), "Selected single related ETagsCustomer is missing ETag Header");
            jsonResult = response.Content.ReadAsAsync<JObject>().Result;
            jsonETag = jsonResult.GetValue("@odata.etag");
            Assert.True(jsonETag != null, "Selected related ETagsCustomer missing ETag");

            requestUri = this.BaseAddress + "/odata/ETagsCustomers?$expand=RelatedCustomer";
            request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.ParseAdd("application/json");
            response = this.Client.SendAsync(request).Result;
            Assert.True(response.IsSuccessStatusCode);
            jsonResult = response.Content.ReadAsAsync<JObject>().Result;
            jsonETag = jsonResult.GetValue("value")[0]["RelatedCustomer"]["@odata.etag"];
            Assert.True(jsonETag != null, "Expanded related ETagsCustomer missing ETag");

            requestUri = this.BaseAddress + "/odata/ETagsCustomers?$expand=ContainedCustomer";
            request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.ParseAdd("application/json");
            response = this.Client.SendAsync(request).Result;
            Assert.True(response.IsSuccessStatusCode);
            jsonResult = response.Content.ReadAsAsync<JObject>().Result;
            jsonETag = jsonResult.GetValue("value")[0]["ContainedCustomer"]["@odata.etag"];
            Assert.True(jsonETag != null, "Expanded contained ETagsCustomer missing ETag");

            requestUri = this.BaseAddress + "/odata/ETagsCustomers(0)?$expand=RelatedCustomer";
            request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.ParseAdd("application/json");
            response = this.Client.SendAsync(request).Result;
            Assert.True(response.IsSuccessStatusCode);
            jsonResult = response.Content.ReadAsAsync<JObject>().Result;
            jsonETag = jsonResult.GetValue("RelatedCustomer")["@odata.etag"];
            Assert.True(jsonETag != null, "Expanded related ETagsCustomer missing ETag");

            requestUri = this.BaseAddress + "/odata/ETagsCustomers(0)?$expand=ContainedCustomer";
            request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.ParseAdd("application/json");
            response = this.Client.SendAsync(request).Result;
            Assert.True(response.IsSuccessStatusCode);
            jsonResult = response.Content.ReadAsAsync<JObject>().Result;
            jsonETag = jsonResult.GetValue("ContainedCustomer")["@odata.etag"];
            Assert.True(jsonETag != null, "Expanded contained ETagsCustomer missing ETag");

            //ETagsCustomer singleton
            requestUri = this.BaseAddress + "/odata/ETagsCustomer";
            request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.ParseAdd("application/json");
            response = this.Client.SendAsync(request).Result;
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Headers.Contains("ETag"), "Singleton ETagsCustomer is missing ETag Header");
            jsonResult = response.Content.ReadAsAsync<JObject>().Result;
            jsonETag = jsonResult.GetValue("@odata.etag");
            Assert.True(jsonETag != null, "Singleton ETagsCustomer is missing ETag");

            requestUri = this.BaseAddress + "/odata/ETagsCustomer/ContainedCustomer";
            request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.ParseAdd("application/json");
            response = this.Client.SendAsync(request).Result;
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Headers.Contains("ETag"), "Selected single contained ETagsCustomer is missing ETag Header");
            jsonResult = response.Content.ReadAsAsync<JObject>().Result;
            jsonETag = jsonResult.GetValue("@odata.etag");
            Assert.True(jsonETag != null, "Selected contained ETagsCustomer missing ETag");

            requestUri = this.BaseAddress + "/odata/ETagsCustomer/RelatedCustomer";
            request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.ParseAdd("application/json");
            response = this.Client.SendAsync(request).Result;
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Headers.Contains("ETag"), "Selected single related ETagsCustomer is missing ETag Header");
            jsonResult = response.Content.ReadAsAsync<JObject>().Result;
            jsonETag = jsonResult.GetValue("@odata.etag");
            Assert.True(jsonETag != null, "Selected related ETagsCustomer missing ETag");

            requestUri = this.BaseAddress + "/odata/ETagsCustomer?$expand=RelatedCustomer";
            request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.ParseAdd("application/json");
            response = this.Client.SendAsync(request).Result;
            Assert.True(response.IsSuccessStatusCode);
            jsonResult = response.Content.ReadAsAsync<JObject>().Result;
            jsonETag = jsonResult.GetValue("RelatedCustomer")["@odata.etag"];
            Assert.True(jsonETag != null, "Expanded related ETagsCustomer missing ETag");

            requestUri = this.BaseAddress + "/odata/ETagsCustomer?$expand=ContainedCustomer";
            request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.ParseAdd("application/json");
            response = this.Client.SendAsync(request).Result;
            Assert.True(response.IsSuccessStatusCode);
            jsonResult = response.Content.ReadAsAsync<JObject>().Result;
            jsonETag = jsonResult.GetValue("ContainedCustomer")["@odata.etag"];
            Assert.True(jsonETag != null, "Expanded contained ETagsCustomer missing ETag");
        }
    }
}