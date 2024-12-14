using Newtonsoft.Json.Linq;
using RestSharp;
using System.Net;

namespace ApiTests
{
    [TestFixture]
    public class CategoryTests : IDisposable
    {
        private RestClient client;
        private string token;
        private Random random;
        private string title;

        [SetUp]
        public void Setup()
        {
            client = new RestClient(GlobalConstants.BaseUrl);
            random = new Random();
            token = GlobalConstants.AuthenticateUser("john.doe@example.com", "password123");
            Assert.That(token, Is.Not.Null.Or.Empty, "Authentication token should not be null or empty");
            
        }

        [Test]
        public void Test_CategoryLifecycle_RecipeBook()
        {
            // Step 1: Create a new category
            var name = $"categoryName_{random.Next(999, 9999)}";
            var createRequest = new RestRequest("/category", Method.Post);
            createRequest.AddHeader("Authorization", $"Bearer {token}");
            createRequest.AddJsonBody(new
            {
                name
            });

            // Act
            var createResponse = client.Execute(createRequest);

            // Assert
            Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "The response does not contain the correct status code OK (200)");

            var createdCategory = JObject.Parse(createResponse.Content);
            Assert.That(createdCategory["_id"]?.ToString(), Is.Not.Null.Or.Empty, 
                "Category ID is not as expected");

            // Step 2: Get all categories
            var getAllCategoriesRequest = new RestRequest("/category", Method.Get);
            var getAllCategoriesResponse = client.Execute(getAllCategoriesRequest);

            Assert.Multiple(() =>
            {
                Assert.That(getAllCategoriesResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "The response does not contain the correct status code OK (200)");
                Assert.That(getAllCategoriesResponse.Content, Is.Not.Null.Or.Empty,
                    "Response content is empty");

                var categories = JArray.Parse(getAllCategoriesResponse.Content);
                Assert.That(categories?.Type, Is.EqualTo(JTokenType.Array),
                    "Expected response content is not a JSON array");
                Assert.That(categories.Count, Is.GreaterThan(0),
                    "Expected at least one category in the response");
            });

            // Step 3: Get category by ID
            var categoryId = createdCategory["_id"]?.ToString();

            var getByIdRequest = new RestRequest($"/category/{categoryId}", Method.Get);
            var getByIdResponse = client.Execute(getByIdRequest);

            Assert.Multiple(() =>
            {
                Assert.That(getByIdResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "The response does not contain the correct status code OK (200)");
                Assert.That(getByIdResponse.Content, Is.Not.Null.Or.Empty,
                    "Response content is empty");

                var categoryById = JObject.Parse(getByIdResponse.Content);
                Assert.That(categoryById["_id"]?.ToString(), Is.EqualTo(categoryId),
                    "Expected the category ID to match");
                Assert.That(categoryById["name"]?.ToString(), Is.EqualTo(name),
                    "Expected the category name to match");
            });

            // Step 4: Edit the category
            var editRequest = new RestRequest($"/category/{categoryId}", Method.Put);
            var udpatedCategoryName = name + "_updated";
            editRequest.AddHeader("Authorization", $"Bearer {token}");
            editRequest.AddJsonBody(new
            {
                name = udpatedCategoryName
            });

            var editResponse = client.Execute(editRequest);
            Assert.That(editResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "The response does not contain the correct status code OK (200)");

            // Step 5: Verify the category is updated
            var getUpdatedCategoryRequest = new RestRequest($"/category/{categoryId}", Method.Get);
            var getUpdatedCategoryResponse = client.Execute(getUpdatedCategoryRequest);

            Assert.Multiple(() =>
            {
                Assert.That(getUpdatedCategoryResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "The response does not contain the correct status code OK (200)");
                Assert.That(getUpdatedCategoryResponse.Content, Is.Not.Null.Or.Empty,
                    "Response content is empty");

                var updatedCategory = JObject.Parse(getUpdatedCategoryResponse.Content);
                Assert.That(updatedCategory["name"]?.ToString(), Is.EqualTo(udpatedCategoryName),
                    "The updated category name does not match");
            });

            // Step 6: Delete the category
            var deleteRequest = new RestRequest($"category/{categoryId}", Method.Delete);
            deleteRequest.AddHeader("Authorization", $"Bearer {token}");

            var deleteResponse = client.Execute(deleteRequest);
            Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "The response does not contain the correct status code OK (200)");

            // Step 7: Verify that the deleted category cannot be found
            var getDeletedCategoryRequest = new RestRequest($"/category/{categoryId}", Method.Get);
            var getDeletedCategoryResponse = client.Execute(getDeletedCategoryRequest);


            Assert.That(getDeletedCategoryResponse.Content, Is.Empty.Or.EqualTo("null"),
                "Deleted category should not be found");
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
